using DocumentManagement.Application.Sharing;

namespace DocumentManagement.Application.Abstractions;

public interface IFolderShareService
{
    Task<IReadOnlyList<FolderShareDto>> ListAsync(string userId, int folderId, CancellationToken cancellationToken = default);
    Task<(bool Ok, string? Error, int? ShareId)> AddAsync(string userId, int folderId, CreateFolderShareRequest request, CancellationToken cancellationToken = default);
    Task<bool> RemoveAsync(string userId, int shareId, CancellationToken cancellationToken = default);
}
