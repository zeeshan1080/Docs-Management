namespace DocumentManagement.Application.Documents;

public class DocumentPageDto
{
    public IReadOnlyList<DocumentDto> Items { get; set; } = Array.Empty<DocumentDto>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}
