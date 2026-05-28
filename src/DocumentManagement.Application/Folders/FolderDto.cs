namespace DocumentManagement.Application.Folders;

public class FolderDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int? ParentFolderId { get; set; }

    /// <summary>When true, the folder cannot be deleted.</summary>
    public bool IsDefault { get; set; }

    public DateTime CreatedOn { get; set; }

    /// <summary>User id who created the folder, if recorded.</summary>
    public string? CreatedByUserId { get; set; }

    /// <summary>Display name or email of creator.</summary>
    public string? CreatedByDisplayName { get; set; }

    public IReadOnlyList<FolderDto> Children { get; set; } = Array.Empty<FolderDto>();
}
