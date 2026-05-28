using DocumentManagement.Domain;

namespace DocumentManagement.Application.Sharing;

public class DocumentShareDto
{
    public int Id { get; set; }
    public int DocumentId { get; set; }
    public ShareType ShareType { get; set; }
    public string? RoleId { get; set; }
    public string? RoleName { get; set; }
    public int? LocationId { get; set; }
    public string? LocationName { get; set; }
    public string? UserId { get; set; }
    public string? UserEmail { get; set; }
    public DateTime CreatedOn { get; set; }

    public string? CreatedByUserId { get; set; }

    public string? CreatedByDisplayName { get; set; }
}
