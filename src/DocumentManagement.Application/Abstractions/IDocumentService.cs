using DocumentManagement.Application.Documents;

namespace DocumentManagement.Application.Abstractions;

public interface IDocumentService
{
    Task<IReadOnlyList<DocumentDto>> ListByFolderAsync(string userId, int folderId, CancellationToken cancellationToken = default);
    Task<DocumentPageDto> ListByFolderPagedAsync(
        string userId,
        int folderId,
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default);
    Task<DocumentDto?> UploadAsync(string userId, int folderId, Stream content, string originalFileName, string contentType, CancellationToken cancellationToken = default);

    /// <summary>Adds a named shortcut to an external URL (no file stored).</summary>
    Task<DocumentDto?> CreateLinkAsync(string userId, int folderId, CreateDocumentLinkRequest request, CancellationToken cancellationToken = default);

    Task<InitChunkUploadResponseDto?> InitChunkUploadAsync(
        string userId,
        int folderId,
        InitChunkUploadRequestDto request,
        CancellationToken cancellationToken = default);

    Task<bool> UploadChunkAsync(
        string userId,
        int folderId,
        Guid sessionId,
        int chunkIndex,
        Stream chunkStream,
        long chunkLength,
        CancellationToken cancellationToken = default);

    Task<DocumentDto?> CompleteChunkUploadAsync(
        string userId,
        int folderId,
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task<(Stream Stream, string FileName, string ContentType)?> DownloadAsync(string userId, int documentId, CancellationToken cancellationToken = default);

    /// <summary>Mints a short-lived token for Google Docs Viewer (public <c>/api/documents/view/{token}</c> fetch).</summary>
    Task<string?> MintViewerTokenAsync(string userId, int documentId, CancellationToken cancellationToken = default);

    /// <summary>Opens the document stream for a valid viewer token (no user id; token was issued after access check).</summary>
    Task<(Stream Stream, string FileName, string ContentType)?> DownloadByViewerTokenAsync(string token, CancellationToken cancellationToken = default);

    Task<bool> SoftDeleteAsync(string userId, int documentId, CancellationToken cancellationToken = default);
}
