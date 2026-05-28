namespace DocumentManagement.Application.Documents;

public class CreateDocumentLinkRequest
{
    /// <summary>Label shown in the folder (e.g. "Time Off Request").</summary>
    public string DisplayName { get; set; } = "";

    /// <summary>Absolute http(s) URL opened in a new tab.</summary>
    public string Url { get; set; } = "";
}
