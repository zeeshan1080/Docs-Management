namespace DocumentManagement.Application.Folders;

public class FolderStatsDto
{
    public int DirectChildFolderCount { get; set; }

    public int DocumentCount { get; set; }

    public long TotalDocumentSizeBytes { get; set; }
}
