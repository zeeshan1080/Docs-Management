using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using DocumentManagement.Application.Abstractions;
using DocumentManagement.Application.Documents;
using DocumentManagement.Domain;
using DocumentManagement.Infrastructure.Data;
using DocumentManagement.Infrastructure.Data.Entities;
using DocumentManagement.Infrastructure.Identity;
using DocumentManagement.Infrastructure.Options;

namespace DocumentManagement.Infrastructure.Services;

public class DocumentService : IDocumentService
{
    private const int MaxPageSize = 100;
    private const int MaxChunkSessionsPerFile = 100_000;
    private static readonly JsonSerializerOptions ChunkMetaJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
    };

    private readonly ApplicationDbContext _db;
    private readonly IAccessControlService _access;
    private readonly IFileStorage _files;
    private readonly IFolderPhysicalStorage _folderPhysical;
    private readonly UserManager<ApplicationUser> _users;
    private readonly FileStorageOptions _fileOptions;
    private readonly DocumentViewerOptions _viewerOptions;
    private readonly IMemoryCache _cache;
    private readonly IHostEnvironment _host;

    public DocumentService(
        ApplicationDbContext db,
        IAccessControlService access,
        IFileStorage files,
        IFolderPhysicalStorage folderPhysical,
        UserManager<ApplicationUser> users,
        IHostEnvironment host,
        IMemoryCache cache,
        IOptions<FileStorageOptions> fileOptions,
        IOptions<DocumentViewerOptions> viewerOptions)
    {
        _db = db;
        _access = access;
        _files = files;
        _folderPhysical = folderPhysical;
        _users = users;
        _host = host;
        _cache = cache;
        _fileOptions = fileOptions.Value;
        _viewerOptions = viewerOptions.Value;
    }

    private string StorageAbsoluteRoot
    {
        get
        {
            var root = _fileOptions.RootPath;
            return Path.IsPathRooted(root) ? root : Path.Combine(_host.ContentRootPath, root);
        }
    }

    public async Task<IReadOnlyList<DocumentDto>> ListByFolderAsync(string userId, int folderId, CancellationToken cancellationToken = default)
    {
        if (!await EnsureContentAllowedAsync(userId, cancellationToken)) return Array.Empty<DocumentDto>();

        var navigable = await _access.GetAccessibleFolderIdsAsync(userId, cancellationToken);
        if (!navigable.Contains(folderId)) return Array.Empty<DocumentDto>();

        var fullAccess = await _access.GetFullAccessFolderIdsAsync(userId, cancellationToken);
        var viewOnly = await _access.GetViewOnlyFolderIdsAsync(userId, cancellationToken);
        if (fullAccess.Contains(folderId) || viewOnly.Contains(folderId))
        {
            return await _db.Documents.AsNoTracking()
                .Where(d => d.FolderId == folderId && !d.IsDeleted)
                .OrderByDescending(d => d.CreatedOn)
                .Select(d => new DocumentDto
                {
                    Id = d.Id,
                    FolderId = d.FolderId,
                    OriginalFileName = d.OriginalFileName,
                    ContentType = d.ContentType,
                    SizeBytes = d.SizeBytes,
                    IsLink = d.IsLink,
                    ExternalUrl = d.ExternalUrl,
                    CreatedOn = d.CreatedOn,
                    CreatedByUserId = d.CreatedBy,
                    CreatedByDisplayName = d.CreatedByUser == null
                        ? null
                        : ((!string.IsNullOrEmpty(d.CreatedByUser.FirstName) ||
                            !string.IsNullOrEmpty(d.CreatedByUser.LastName))
                            ? ((d.CreatedByUser.FirstName ?? "") + " " + (d.CreatedByUser.LastName ?? "")).Trim()
                            : d.CreatedByUser.Email)
                })
                .ToListAsync(cancellationToken);
        }

        var sharedIds = await _access.GetDocumentShareIdsInFolderAsync(userId, folderId, cancellationToken);
        var idList = sharedIds.ToList();
        if (idList.Count == 0) return Array.Empty<DocumentDto>();

        return await _db.Documents.AsNoTracking()
            .Where(d => d.FolderId == folderId && !d.IsDeleted && idList.Contains(d.Id))
            .OrderByDescending(d => d.CreatedOn)
            .Select(d => new DocumentDto
            {
                Id = d.Id,
                FolderId = d.FolderId,
                OriginalFileName = d.OriginalFileName,
                ContentType = d.ContentType,
                SizeBytes = d.SizeBytes,
                IsLink = d.IsLink,
                ExternalUrl = d.ExternalUrl,
                CreatedOn = d.CreatedOn,
                CreatedByUserId = d.CreatedBy,
                CreatedByDisplayName = d.CreatedByUser == null
                    ? null
                    : ((!string.IsNullOrEmpty(d.CreatedByUser.FirstName) ||
                        !string.IsNullOrEmpty(d.CreatedByUser.LastName))
                        ? ((d.CreatedByUser.FirstName ?? "") + " " + (d.CreatedByUser.LastName ?? "")).Trim()
                        : d.CreatedByUser.Email)
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<DocumentPageDto> ListByFolderPagedAsync(
        string userId,
        int folderId,
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var safePage = page < 1 ? 1 : page;
        var safePageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        var term = search?.Trim();

        if (!await EnsureContentAllowedAsync(userId, cancellationToken))
            return EmptyPage(safePage, safePageSize);

        var navigable = await _access.GetAccessibleFolderIdsAsync(userId, cancellationToken);
        if (!navigable.Contains(folderId))
            return EmptyPage(safePage, safePageSize);

        IQueryable<Document> query;
        var fullAccess = await _access.GetFullAccessFolderIdsAsync(userId, cancellationToken);
        var viewOnly = await _access.GetViewOnlyFolderIdsAsync(userId, cancellationToken);
        if (fullAccess.Contains(folderId) || viewOnly.Contains(folderId))
        {
            query = _db.Documents.AsNoTracking()
                .Where(d => d.FolderId == folderId && !d.IsDeleted);
        }
        else
        {
            var sharedIds = await _access.GetDocumentShareIdsInFolderAsync(userId, folderId, cancellationToken);
            var idList = sharedIds.ToList();
            if (idList.Count == 0) return EmptyPage(safePage, safePageSize);
            query = _db.Documents.AsNoTracking()
                .Where(d => d.FolderId == folderId && !d.IsDeleted && idList.Contains(d.Id));
        }

        if (!string.IsNullOrWhiteSpace(term))
        {
            query = query.Where(d => d.OriginalFileName.Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)safePageSize);
        if (totalPages > 0 && safePage > totalPages)
        {
            safePage = totalPages;
        }

        var items = await query
            .OrderByDescending(d => d.CreatedOn)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .Select(d => new DocumentDto
            {
                Id = d.Id,
                FolderId = d.FolderId,
                OriginalFileName = d.OriginalFileName,
                ContentType = d.ContentType,
                SizeBytes = d.SizeBytes,
                IsLink = d.IsLink,
                ExternalUrl = d.ExternalUrl,
                CreatedOn = d.CreatedOn,
                CreatedByUserId = d.CreatedBy,
                CreatedByDisplayName = d.CreatedByUser == null
                    ? null
                    : ((!string.IsNullOrEmpty(d.CreatedByUser.FirstName) ||
                        !string.IsNullOrEmpty(d.CreatedByUser.LastName))
                        ? ((d.CreatedByUser.FirstName ?? "") + " " + (d.CreatedByUser.LastName ?? "")).Trim()
                        : d.CreatedByUser.Email)
            })
            .ToListAsync(cancellationToken);

        return new DocumentPageDto
        {
            Items = items,
            Page = safePage,
            PageSize = safePageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    public async Task<DocumentDto?> UploadAsync(string userId, int folderId, Stream content, string originalFileName, string contentType, CancellationToken cancellationToken = default)
    {
        if (!await EnsureContentAllowedAsync(userId, cancellationToken)) return null;
        if (!await _access.CanAccessFolderAsync(userId, folderId, cancellationToken)) return null;

        if (content.CanSeek && content.Length > _fileOptions.MaxUploadBytes) return null;

        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        if (buffer.Length > _fileOptions.MaxUploadBytes) return null;
        buffer.Position = 0;

        return await PersistNewDocumentFromStreamAsync(
            userId,
            folderId,
            buffer,
            buffer.Length,
            originalFileName,
            contentType,
            cancellationToken);
    }

    public async Task<DocumentDto?> CreateLinkAsync(
        string userId,
        int folderId,
        CreateDocumentLinkRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null) return null;
        var name = request.DisplayName.Trim();
        if (string.IsNullOrEmpty(name) || name.Length > 500) return null;

        var trimmedUrl = request.Url.Trim();
        if (trimmedUrl.Length > 2000) return null;
        if (!Uri.TryCreate(trimmedUrl, UriKind.Absolute, out var uri)) return null;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return null;

        if (!await EnsureContentAllowedAsync(userId, cancellationToken)) return null;
        if (!await _access.CanAccessFolderAsync(userId, folderId, cancellationToken)) return null;

        var entity = new Document
        {
            FolderId = folderId,
            OriginalFileName = name,
            StoredFileName = "",
            ContentType = "application/x-document-link",
            SizeBytes = 0,
            IsLink = true,
            ExternalUrl = trimmedUrl,
            CreatedBy = userId,
            CreatedOn = DateTime.UtcNow,
            IsDeleted = false,
        };
        _db.Documents.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        var uploader = await _users.FindByIdAsync(userId);
        return new DocumentDto
        {
            Id = entity.Id,
            FolderId = entity.FolderId,
            OriginalFileName = entity.OriginalFileName,
            ContentType = entity.ContentType,
            SizeBytes = entity.SizeBytes,
            IsLink = true,
            ExternalUrl = trimmedUrl,
            CreatedOn = entity.CreatedOn,
            CreatedByUserId = userId,
            CreatedByDisplayName = UserDisplayName.Format(uploader),
        };
    }

    public async Task<InitChunkUploadResponseDto?> InitChunkUploadAsync(
        string userId,
        int folderId,
        InitChunkUploadRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!await EnsureContentAllowedAsync(userId, cancellationToken)) return null;
        if (!await _access.CanAccessFolderAsync(userId, folderId, cancellationToken)) return null;

        var name = request.FileName.Trim();
        if (string.IsNullOrEmpty(name) || name.Length > 500) return null;
        if (request.TotalSize <= 0 || request.TotalSize > _fileOptions.MaxUploadBytes) return null;
        if (request.TotalChunks < 1 || request.TotalChunks > MaxChunkSessionsPerFile) return null;

        var minChunks = (int)Math.Ceiling(request.TotalSize / (double)_fileOptions.MaxChunkUploadBytes);
        if (request.TotalChunks < minChunks) return null;

        var maxChunkSize = (request.TotalSize + request.TotalChunks - 1) / request.TotalChunks;
        if (maxChunkSize > _fileOptions.MaxChunkUploadBytes) return null;

        var sessionId = Guid.NewGuid();
        var sessionDir = GetChunkSessionDirectory(sessionId);
        Directory.CreateDirectory(sessionDir);

        var meta = new ChunkSessionMeta
        {
            UserId = userId,
            FolderId = folderId,
            OriginalFileName = name,
            ContentType = NormalizeContentType(request.ContentType),
            TotalSize = request.TotalSize,
            TotalChunks = request.TotalChunks,
            CreatedUtc = DateTime.UtcNow,
        };

        await File.WriteAllTextAsync(
            Path.Combine(sessionDir, "meta.json"),
            JsonSerializer.Serialize(meta, ChunkMetaJson),
            cancellationToken);

        return new InitChunkUploadResponseDto { SessionId = sessionId };
    }

    public async Task<bool> UploadChunkAsync(
        string userId,
        int folderId,
        Guid sessionId,
        int chunkIndex,
        Stream chunkStream,
        long chunkLength,
        CancellationToken cancellationToken = default)
    {
        if (chunkLength <= 0 || chunkLength > _fileOptions.MaxChunkUploadBytes) return false;

        var meta = await TryReadChunkSessionMetaAsync(sessionId, cancellationToken);
        if (meta == null) return false;
        if (!string.Equals(meta.UserId, userId, StringComparison.Ordinal) || meta.FolderId != folderId)
            return false;
        if (chunkIndex < 0 || chunkIndex >= meta.TotalChunks) return false;

        var sessionDir = GetChunkSessionDirectory(sessionId);
        var partPath = Path.Combine(sessionDir, $"part.{chunkIndex}");
        try
        {
            await using (var fs = File.Create(partPath))
            {
                await chunkStream.CopyToAsync(fs, cancellationToken);
            }

            if (new FileInfo(partPath).Length != chunkLength)
            {
                File.Delete(partPath);
                return false;
            }
        }
        catch
        {
            if (File.Exists(partPath)) File.Delete(partPath);
            throw;
        }

        return true;
    }

    public async Task<DocumentDto?> CompleteChunkUploadAsync(
        string userId,
        int folderId,
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var meta = await TryReadChunkSessionMetaAsync(sessionId, cancellationToken);
        if (meta == null) return null;
        if (!string.Equals(meta.UserId, userId, StringComparison.Ordinal) || meta.FolderId != folderId)
            return null;

        var sessionDir = GetChunkSessionDirectory(sessionId);
        long total = 0;
        for (var i = 0; i < meta.TotalChunks; i++)
        {
            var partPath = Path.Combine(sessionDir, $"part.{i}");
            if (!File.Exists(partPath)) return null;
            total += new FileInfo(partPath).Length;
        }

        if (total != meta.TotalSize) return null;

        var mergedPath = Path.Combine(sessionDir, "_merged.bin");
        await using (var output = new FileStream(mergedPath, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan))
        {
            for (var i = 0; i < meta.TotalChunks; i++)
            {
                var partPath = Path.Combine(sessionDir, $"part.{i}");
                await using var input = new FileStream(partPath, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
                await input.CopyToAsync(output, cancellationToken);
            }
        }

        if (new FileInfo(mergedPath).Length != meta.TotalSize)
        {
            TryDeleteDirectoryRecursive(sessionDir);
            return null;
        }

        try
        {
            await using var readMerged = new FileStream(mergedPath, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
            var dto = await PersistNewDocumentFromStreamAsync(
                userId,
                folderId,
                readMerged,
                meta.TotalSize,
                meta.OriginalFileName,
                meta.ContentType,
                cancellationToken);
            return dto;
        }
        finally
        {
            TryDeleteDirectoryRecursive(sessionDir);
        }
    }

    private async Task<DocumentDto?> PersistNewDocumentFromStreamAsync(
        string userId,
        int folderId,
        Stream content,
        long sizeBytes,
        string originalFileName,
        string contentType,
        CancellationToken cancellationToken)
    {
        if (!await EnsureContentAllowedAsync(userId, cancellationToken)) return null;
        if (!await _access.CanAccessFolderAsync(userId, folderId, cancellationToken)) return null;
        if (sizeBytes > _fileOptions.MaxUploadBytes) return null;

        await _folderPhysical.EnsureDirectoryExistsAsync(folderId, cancellationToken);
        var relativeDir = await _folderPhysical.GetRelativePathAsync(folderId, cancellationToken);
        var stored = await _files.SaveAsync(
            content,
            originalFileName,
            contentType,
            string.IsNullOrEmpty(relativeDir) ? null : relativeDir,
            cancellationToken);
        var entity = new Document
        {
            FolderId = folderId,
            OriginalFileName = originalFileName,
            StoredFileName = stored,
            ContentType = contentType,
            SizeBytes = sizeBytes,
            IsLink = false,
            ExternalUrl = null,
            CreatedBy = userId,
            CreatedOn = DateTime.UtcNow,
            IsDeleted = false
        };
        _db.Documents.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        var uploader = await _users.FindByIdAsync(userId);
        return new DocumentDto
        {
            Id = entity.Id,
            FolderId = entity.FolderId,
            OriginalFileName = entity.OriginalFileName,
            ContentType = entity.ContentType,
            SizeBytes = entity.SizeBytes,
            IsLink = false,
            ExternalUrl = null,
            CreatedOn = entity.CreatedOn,
            CreatedByUserId = userId,
            CreatedByDisplayName = UserDisplayName.Format(uploader)
        };
    }

    private string GetChunkSessionDirectory(Guid sessionId) =>
        Path.Combine(StorageAbsoluteRoot, ".chunk-sessions", sessionId.ToString("N"));

    private async Task<ChunkSessionMeta?> TryReadChunkSessionMetaAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var path = Path.Combine(GetChunkSessionDirectory(sessionId), "meta.json");
        if (!File.Exists(path)) return null;
        try
        {
            var json = await File.ReadAllTextAsync(path, cancellationToken);
            return JsonSerializer.Deserialize<ChunkSessionMeta>(json, ChunkMetaJson);
        }
        catch
        {
            return null;
        }
    }

    private static void TryDeleteDirectoryRecursive(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
            // best effort
        }
    }

    private sealed class ChunkSessionMeta
    {
        public string UserId { get; set; } = "";
        public int FolderId { get; set; }
        public string OriginalFileName { get; set; } = "";
        public string ContentType { get; set; } = "";
        public long TotalSize { get; set; }
        public int TotalChunks { get; set; }
        public DateTime CreatedUtc { get; set; }
    }

    public async Task<(Stream Stream, string FileName, string ContentType)?> DownloadAsync(string userId, int documentId, CancellationToken cancellationToken = default)
    {
        if (!await EnsureContentAllowedAsync(userId, cancellationToken)) return null;
        if (!await _access.CanAccessDocumentAsync(userId, documentId, cancellationToken)) return null;
        return await OpenDocumentStreamAsync(documentId, cancellationToken);
    }

    public async Task<string?> MintViewerTokenAsync(string userId, int documentId, CancellationToken cancellationToken = default)
    {
        if (!await EnsureContentAllowedAsync(userId, cancellationToken)) return null;
        if (!await _access.CanAccessDocumentAsync(userId, documentId, cancellationToken)) return null;

        var doc = await _db.Documents.AsNoTracking().FirstOrDefaultAsync(d => d.Id == documentId && !d.IsDeleted, cancellationToken);
        if (doc == null || doc.IsLink) return null;
        if (!DocumentViewerSupport.IsSupported(doc.OriginalFileName, doc.ContentType)) return null;

        var token = Guid.NewGuid().ToString("N");
        var minutes = Math.Clamp(_viewerOptions.TokenMinutes, 1, 120);
        _cache.Set(
            ViewerCacheKey(token),
            documentId,
            new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(minutes) });

        return token;
    }

    public Task<(Stream Stream, string FileName, string ContentType)?> DownloadByViewerTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(token) || !Guid.TryParseExact(token, "N", out _))
            return Task.FromResult<(Stream Stream, string FileName, string ContentType)?>(null);

        if (!_cache.TryGetValue(ViewerCacheKey(token), out int documentId))
            return Task.FromResult<(Stream Stream, string FileName, string ContentType)?>(null);

        return OpenDocumentStreamAsync(documentId, cancellationToken);
    }

    private static string ViewerCacheKey(string token) => $"docviewer:{token}";

    private async Task<(Stream Stream, string FileName, string ContentType)?> OpenDocumentStreamAsync(
        int documentId,
        CancellationToken cancellationToken)
    {
        var doc = await _db.Documents.AsNoTracking().FirstOrDefaultAsync(d => d.Id == documentId && !d.IsDeleted, cancellationToken);
        if (doc == null || doc.IsLink || string.IsNullOrEmpty(doc.StoredFileName)) return null;

        Stream stream;
        try
        {
            var relativeDir = await _folderPhysical.GetRelativePathAsync(doc.FolderId, cancellationToken);
            stream = await _files.OpenReadAsync(
                doc.StoredFileName,
                string.IsNullOrEmpty(relativeDir) ? null : relativeDir,
                cancellationToken);
        }
        catch (FileNotFoundException)
        {
            return null;
        }

        return (stream, doc.OriginalFileName, doc.ContentType);
    }

    public async Task<bool> SoftDeleteAsync(string userId, int documentId, CancellationToken cancellationToken = default)
    {
        var user = await _users.FindByIdAsync(userId);
        if (user == null || !await _users.IsInRoleAsync(user, AppRoles.Management)) return false;
        if (!await _access.CanAccessDocumentAsync(userId, documentId, cancellationToken)) return false;

        var doc = await _db.Documents.FirstOrDefaultAsync(d => d.Id == documentId && !d.IsDeleted, cancellationToken);
        if (doc == null) return false;

        if (!doc.IsLink && !string.IsNullOrEmpty(doc.StoredFileName))
        {
            var relativeDir = await _folderPhysical.GetRelativePathAsync(doc.FolderId, cancellationToken);
            await _files.MoveFileToDeletedAsync(
                doc.StoredFileName,
                string.IsNullOrEmpty(relativeDir) ? null : relativeDir,
                cancellationToken);
        }

        doc.IsDeleted = true;
        doc.LastModifiedOn = DateTime.UtcNow;
        doc.LastModifiedBy = userId;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<bool> EnsureContentAllowedAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await _users.FindByIdAsync(userId);
        if (user == null) return false;
        if (await _users.IsInRoleAsync(user, AppRoles.Management)) return true;
        return (ApprovalStatus)user.ApprovalStatus == ApprovalStatus.Approved;
    }

    private static string NormalizeContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType)) return "application/octet-stream";
        var t = contentType.Trim();
        return t.Length > 200 ? t[..200] : t;
    }

    private static DocumentPageDto EmptyPage(int page, int pageSize) => new()
    {
        Items = Array.Empty<DocumentDto>(),
        Page = page,
        PageSize = pageSize,
        TotalCount = 0,
        TotalPages = 0
    };
}
