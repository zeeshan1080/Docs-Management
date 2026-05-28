using DocumentManagement.Application.Abstractions;
using DocumentManagement.Infrastructure.Data;
using DocumentManagement.Infrastructure.Data.Entities;

namespace DocumentManagement.Infrastructure.Services;

public class EfAuditService : IAuditService
{
    private readonly ApplicationDbContext _db;

    public EfAuditService(ApplicationDbContext db) => _db = db;

    public async Task LogAsync(
        string? userId,
        string action,
        string? entityType,
        string? entityId,
        string? details,
        CancellationToken cancellationToken = default)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            CreatedBy = userId,
            CreatedOn = DateTime.UtcNow,
        });

        await _db.SaveChangesAsync(cancellationToken);
    }
}
