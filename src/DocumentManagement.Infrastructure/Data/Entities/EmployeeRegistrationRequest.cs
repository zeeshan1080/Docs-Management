using System.ComponentModel.DataAnnotations.Schema;
using DocumentManagement.Infrastructure.Identity;

namespace DocumentManagement.Infrastructure.Data.Entities;

[Table("EmployeeRegistrationRequests")]
public class EmployeeRegistrationRequest : AuditableEntity
{
    public int Id { get; set; }
    public string UserId { get; set; } = null!;
    public string RequestedRoleId { get; set; } = null!;
    public int RequestedLocationId { get; set; }
    public byte Status { get; set; }
    public string? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public string? Notes { get; set; }

    public ApplicationUser? User { get; set; }
    public ApplicationRole? RequestedRole { get; set; }
    public Location? RequestedLocation { get; set; }
    public ApplicationUser? ReviewedBy { get; set; }
}
