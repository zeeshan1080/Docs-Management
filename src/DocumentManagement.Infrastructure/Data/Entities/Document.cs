using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DocumentManagement.Infrastructure.Identity;

namespace DocumentManagement.Infrastructure.Data.Entities;

[Table("Documents")]
public class Document : AuditableEntity
{
    public int Id { get; set; }
    public int FolderId { get; set; }

    [MaxLength(500)]
    public string OriginalFileName { get; set; } = "";

    [MaxLength(500)]
    public string StoredFileName { get; set; } = "";

    [MaxLength(200)]
    public string ContentType { get; set; } = "";

    public long SizeBytes { get; set; }

    /// <summary>When true, <see cref="ExternalUrl"/> is opened in the browser instead of a stored file.</summary>
    public bool IsLink { get; set; }

    [MaxLength(2000)]
    public string? ExternalUrl { get; set; }

    public bool IsDeleted { get; set; }

    public Folder? Folder { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }
    public ICollection<DocumentShare> DocumentShares { get; set; } = new List<DocumentShare>();
}
