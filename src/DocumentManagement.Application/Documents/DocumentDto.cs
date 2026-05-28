namespace DocumentManagement.Application.Documents;

public class DocumentDto
{
    public int Id { get; set; }
    public int FolderId { get; set; }
    public string OriginalFileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long SizeBytes { get; set; }

    public bool IsLink { get; set; }
    public string? ExternalUrl { get; set; }
    public DateTime CreatedOn { get; set; }

    public string? CreatedByUserId { get; set; }

    public string? CreatedByDisplayName { get; set; }
}
