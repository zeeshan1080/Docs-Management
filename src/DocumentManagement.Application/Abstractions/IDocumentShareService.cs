using DocumentManagement.Application.Sharing;

namespace DocumentManagement.Application.Abstractions;

public interface IDocumentShareService
{
    Task<IReadOnlyList<DocumentShareDto>> ListAsync(string userId, int documentId, CancellationToken cancellationToken = default);
    Task<(bool Ok, string? Error, int? ShareId)> AddAsync(string userId, int documentId, CreateDocumentShareRequest request, CancellationToken cancellationToken = default);
    Task<bool> RemoveAsync(string userId, int documentId, int shareId, CancellationToken cancellationToken = default);
}
