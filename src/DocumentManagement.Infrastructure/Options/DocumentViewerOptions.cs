namespace DocumentManagement.Infrastructure.Options;

public class DocumentViewerOptions
{
    public const string SectionName = "DocumentViewer";

    /// <summary>
    /// Public base URL of this API (e.g. https://files.example.com) so Google Docs Viewer can fetch files.
    /// If empty, the host from the incoming HTTP request is used when minting links (localhost will not work for Google).
    /// </summary>
    public string PublicOrigin { get; set; } = "";

    /// <summary>Lifetime of a one-time viewer token (Google may fetch the file more than once during preview).</summary>
    public int TokenMinutes { get; set; } = 15;
}
