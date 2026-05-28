using System.ComponentModel.DataAnnotations.Schema;
using DocumentManagement.Infrastructure.Identity;

namespace DocumentManagement.Infrastructure.Data.Entities;

[Table("DocumentShares")]
public class DocumentShare : AuditableEntity
{
    public int Id { get; set; }
    public int DocumentId { get; set; }
    public byte ShareType { get; set; }
    public string? RoleId { get; set; }
    public int? LocationId { get; set; }
    public string? UserId { get; set; }

    public Document? Document { get; set; }
    public ApplicationRole? Role { get; set; }
    public Location? Location { get; set; }
    public ApplicationUser? User { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }
}
