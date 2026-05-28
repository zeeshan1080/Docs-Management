using System.ComponentModel.DataAnnotations.Schema;
using DocumentManagement.Infrastructure.Identity;

namespace DocumentManagement.Infrastructure.Data.Entities;

[Table("FolderShares")]
public class FolderShare : AuditableEntity
{
    public int Id { get; set; }
    public int FolderId { get; set; }
    public byte ShareType { get; set; }
    /// <summary>1 = view/download only, 2 = full (upload + same listing).</summary>
    public byte AccessLevel { get; set; } = 2;
    public string? RoleId { get; set; }
    public int? LocationId { get; set; }
    public string? UserId { get; set; }

    public Folder? Folder { get; set; }
    public ApplicationRole? Role { get; set; }
    public Location? Location { get; set; }
    public ApplicationUser? User { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }
}
