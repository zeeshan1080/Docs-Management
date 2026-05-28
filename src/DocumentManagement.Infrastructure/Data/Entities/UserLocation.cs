using System.ComponentModel.DataAnnotations.Schema;
using DocumentManagement.Infrastructure.Identity;

namespace DocumentManagement.Infrastructure.Data.Entities;

[Table("UserLocations")]
public class UserLocation : AuditableEntity
{
    public string UserId { get; set; } = null!;
    public int LocationId { get; set; }
    public bool IsPrimary { get; set; } = true;

    public ApplicationUser? User { get; set; }
    public Location? Location { get; set; }
}
