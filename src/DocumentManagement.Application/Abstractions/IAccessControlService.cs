namespace DocumentManagement.Application.Abstractions;

public interface IAccessControlService
{
    /// <summary>Folder IDs the user may see in the tree (full folder access plus ancestors of individually shared documents).</summary>
    Task<IReadOnlySet<int>> GetAccessibleFolderIdsAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Folder IDs where the user has full folder-share access (upload + list all files in subtree).</summary>
    Task<IReadOnlySet<int>> GetFullAccessFolderIdsAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Folder IDs where the user has view-only folder-share access (list/download all files, no upload).</summary>
    Task<IReadOnlySet<int>> GetViewOnlyFolderIdsAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Document IDs shared directly to the user within a folder when the user does not have full folder access.</summary>
    Task<IReadOnlyList<int>> GetDocumentShareIdsInFolderAsync(string userId, int folderId, CancellationToken cancellationToken = default);

    /// <summary>True if the user has full folder-share access (upload and list all files), not merely navigable for a document share.</summary>
    Task<bool> CanAccessFolderAsync(string userId, int folderId, CancellationToken cancellationToken = default);

    Task<bool> CanAccessDocumentAsync(string userId, int documentId, CancellationToken cancellationToken = default);
}
