using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DocumentManagement.Infrastructure.Identity;

namespace DocumentManagement.Infrastructure.Data.Entities;

[Table("Folders")]
public class Folder : AuditableEntity
{
    public int Id { get; set; }

    [MaxLength(500)]
    public string Name { get; set; } = "";

    public int? ParentFolderId { get; set; }

    /// <summary>System default folder; must not be deleted.</summary>
    public bool IsDefault { get; set; }

    public Folder? Parent { get; set; }
    public ICollection<Folder> Children { get; set; } = new List<Folder>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ApplicationUser? CreatedByUser { get; set; }
}
