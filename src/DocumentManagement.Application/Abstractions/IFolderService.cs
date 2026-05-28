using DocumentManagement.Application.Folders;

namespace DocumentManagement.Application.Abstractions;

public interface IFolderService
{
    Task<IReadOnlyList<FolderDto>> GetTreeAsync(string userId, CancellationToken cancellationToken = default);

    Task<FolderStatsDto?> GetStatsAsync(string userId, int folderId, CancellationToken cancellationToken = default);

    Task<FolderDto?> CreateAsync(string userId, CreateFolderRequest request, CancellationToken cancellationToken = default);
    Task<FolderDto?> RenameAsync(string userId, int folderId, RenameFolderRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string userId, int folderId, CancellationToken cancellationToken = default);
}
