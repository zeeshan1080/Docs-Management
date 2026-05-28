using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DocumentManagement.Infrastructure.Data.Entities;

[Table("AuditLogs")]
public class AuditLog : AuditableEntity
{
    public long Id { get; set; }
    public string? UserId { get; set; }

    [MaxLength(100)]
    public string Action { get; set; } = "";

    [MaxLength(100)]
    public string? EntityType { get; set; }

    [MaxLength(100)]
    public string? EntityId { get; set; }

    public string? Details { get; set; }
}
