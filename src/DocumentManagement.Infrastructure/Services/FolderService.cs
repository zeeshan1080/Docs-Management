using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DocumentManagement.Application.Abstractions;
using DocumentManagement.Application.Folders;
using DocumentManagement.Domain;
using DocumentManagement.Infrastructure.Data;
using DocumentManagement.Infrastructure.Data.Entities;
using DocumentManagement.Infrastructure.Identity;

namespace DocumentManagement.Infrastructure.Services;

public class FolderService : IFolderService
{
    private readonly ApplicationDbContext _db;
    private readonly IAccessControlService _access;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IFolderPhysicalStorage _folderPhysical;

    public FolderService(
        ApplicationDbContext db,
        IAccessControlService access,
        UserManager<ApplicationUser> users,
        IFolderPhysicalStorage folderPhysical)
    {
        _db = db;
        _access = access;
        _users = users;
        _folderPhysical = folderPhysical;
    }

    public async Task<IReadOnlyList<FolderDto>> GetTreeAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _users.FindByIdAsync(userId);
        if (user == null) return Array.Empty<FolderDto>();
        if ((ApprovalStatus)user.ApprovalStatus != ApprovalStatus.Approved && !await _users.IsInRoleAsync(user, AppRoles.Management))
            return Array.Empty<FolderDto>();

        var accessible = await _access.GetAccessibleFolderIdsAsync(userId, cancellationToken);
        if (accessible.Count == 0) return Array.Empty<FolderDto>();

        // EF Core cannot translate IReadOnlySet<>.Contains; use a list for SQL IN (...).
        var accessibleIds = accessible.ToList();

        var flat = await _db.Folders.AsNoTracking()
            .Include(f => f.CreatedByUser)
            .Where(f => accessibleIds.Contains(f.Id))
            .OrderBy(f => f.Name)
            .ToListAsync(cancellationToken);

        return BuildForest(flat, accessible);
    }

    public async Task<FolderStatsDto?> GetStatsAsync(string userId, int folderId, CancellationToken cancellationToken = default)
    {
        var user = await _users.FindByIdAsync(userId);
        if (user == null) return null;
        if ((ApprovalStatus)user.ApprovalStatus != ApprovalStatus.Approved && !await _users.IsInRoleAsync(user, AppRoles.Management))
            return null;

        var navigable = await _access.GetAccessibleFolderIdsAsync(userId, cancellationToken);
        if (!navigable.Contains(folderId)) return null;

        var directChildFolderCount = await _db.Folders.AsNoTracking()
            .CountAsync(f => f.ParentFolderId == folderId, cancellationToken);

        var docQuery = _db.Documents.AsNoTracking().Where(d => d.FolderId == folderId && !d.IsDeleted);
        var documentCount = await docQuery.CountAsync(cancellationToken);
        var totalDocumentSizeBytes = await docQuery.SumAsync(d => (long?)d.SizeBytes, cancellationToken) ?? 0L;

        return new FolderStatsDto
        {
            DirectChildFolderCount = directChildFolderCount,
            DocumentCount = documentCount,
            TotalDocumentSizeBytes = totalDocumentSizeBytes
        };
    }

    public async Task<FolderDto?> CreateAsync(string userId, CreateFolderRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _users.FindByIdAsync(userId);
        if (user == null || !await _users.IsInRoleAsync(user, AppRoles.Management)) return null;

        if (request.ParentFolderId is { } pid)
        {
            var parent = await _db.Folders.FindAsync(new object[] { pid }, cancellationToken);
            if (parent == null) return null;
        }

        var folder = new Folder
        {
            Name = request.Name.Trim(),
            ParentFolderId = request.ParentFolderId,
            CreatedOn = DateTime.UtcNow,
            CreatedBy = userId
        };
        _db.Folders.Add(folder);
        await _db.SaveChangesAsync(cancellationToken);
        await _folderPhysical.EnsureDirectoryExistsAsync(folder.Id, cancellationToken);
        return new FolderDto
        {
            Id = folder.Id,
            Name = folder.Name,
            ParentFolderId = folder.ParentFolderId,
            IsDefault = folder.IsDefault,
            CreatedOn = folder.CreatedOn,
            CreatedByUserId = user.Id,
            CreatedByDisplayName = UserDisplayName.Format(user),
            Children = Array.Empty<FolderDto>()
        };
    }

    public async Task<FolderDto?> RenameAsync(
        string userId,
        int folderId,
        RenameFolderRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.FindByIdAsync(userId);
        if (user == null || !await _users.IsInRoleAsync(user, AppRoles.Management)) return null;

        var folder = await _db.Folders.FirstOrDefaultAsync(f => f.Id == folderId, cancellationToken);
        if (folder == null) return null;

        var name = request.Name.Trim();
        if (string.IsNullOrEmpty(name)) return null;

        var duplicate = await _db.Folders.AnyAsync(
            f =>
                f.ParentFolderId == folder.ParentFolderId
                && f.Name == name
                && f.Id != folderId,
            cancellationToken);
        if (duplicate) return null;

        var oldRelativePath = await _folderPhysical.GetRelativePathAsync(folderId, cancellationToken);
        folder.Name = name;
        await _db.SaveChangesAsync(cancellationToken);
        var newRelativePath = await _folderPhysical.GetRelativePathAsync(folderId, cancellationToken);
        if (!string.Equals(oldRelativePath, newRelativePath, StringComparison.Ordinal))
            await _folderPhysical.TryMoveDirectoryAsync(oldRelativePath, newRelativePath, cancellationToken);

        var reload = await _db.Folders.AsNoTracking()
            .Include(f => f.CreatedByUser)
            .FirstAsync(f => f.Id == folderId, cancellationToken);

        return new FolderDto
        {
            Id = reload.Id,
            Name = reload.Name,
            ParentFolderId = reload.ParentFolderId,
            IsDefault = reload.IsDefault,
            CreatedOn = reload.CreatedOn,
            CreatedByUserId = reload.CreatedBy,
            CreatedByDisplayName = UserDisplayName.Format(reload.CreatedByUser),
            Children = Array.Empty<FolderDto>()
        };
    }

    public async Task<bool> DeleteAsync(string userId, int folderId, CancellationToken cancellationToken = default)
    {
        var user = await _users.FindByIdAsync(userId);
        if (user == null || !await _users.IsInRoleAsync(user, AppRoles.Management)) return false;

        var folder = await _db.Folders.Include(f => f.Children).Include(f => f.Documents)
            .FirstOrDefaultAsync(f => f.Id == folderId, cancellationToken);
        if (folder == null) return false;
        if (folder.IsDefault) return false;
        if (folder.Children.Count > 0) return false;
        if (folder.Documents.Any(d => !d.IsDeleted)) return false;

        var relativePath = await _folderPhysical.GetRelativePathAsync(folderId, cancellationToken);
        _db.Folders.Remove(folder);
        await _db.SaveChangesAsync(cancellationToken);
        await _folderPhysical.TryDeleteEmptyDirectoryAsync(relativePath, cancellationToken);
        return true;
    }

    private static List<FolderDto> BuildForest(List<Folder> flat, IReadOnlySet<int> accessible)
    {
        var byParent = flat.ToLookup(f => f.ParentFolderId);
        FolderDto Map(int id)
        {
            var f = flat.First(x => x.Id == id);
            var children = flat.Where(c => c.ParentFolderId == id).OrderBy(c => c.Name).Select(c => Map(c.Id)).ToList();
            return new FolderDto
            {
                Id = f.Id,
                Name = f.Name,
                ParentFolderId = f.ParentFolderId,
                IsDefault = f.IsDefault,
                CreatedOn = f.CreatedOn,
                CreatedByUserId = f.CreatedBy,
                CreatedByDisplayName = UserDisplayName.Format(f.CreatedByUser),
                Children = children
            };
        }

        var roots = flat
            .Where(f => f.ParentFolderId == null || !accessible.Contains(f.ParentFolderId.Value))
            .OrderBy(f => f.Name)
            .Select(f => Map(f.Id))
            .ToList();
        return roots;
    }
}
