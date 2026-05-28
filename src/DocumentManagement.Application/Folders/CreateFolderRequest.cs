namespace DocumentManagement.Application.Folders;

public class CreateFolderRequest
{
    public string Name { get; set; } = "";
    public int? ParentFolderId { get; set; }
}
