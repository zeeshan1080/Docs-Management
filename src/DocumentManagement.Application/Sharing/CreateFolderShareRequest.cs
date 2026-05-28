using DocumentManagement.Domain;

namespace DocumentManagement.Application.Sharing;

public class CreateFolderShareRequest
{
    public FolderAccessLevel AccessLevel { get; set; } = FolderAccessLevel.FullAccess;
    public ShareType ShareType { get; set; }
    public string? RoleId { get; set; }
    public int? LocationId { get; set; }
    public string? UserId { get; set; }
}
