using DocumentManagement.Domain;

namespace DocumentManagement.Application.Sharing;

public class FolderShareDto
{
    public int Id { get; set; }
    public int FolderId { get; set; }
    public ShareType ShareType { get; set; }
    public FolderAccessLevel AccessLevel { get; set; }
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
