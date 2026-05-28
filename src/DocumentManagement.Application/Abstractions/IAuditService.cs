namespace DocumentManagement.Application.Abstractions;

public interface IAuditService
{
    Task LogAsync(string? userId, string action, string? entityType, string? entityId, string? details, CancellationToken cancellationToken = default);
}
