namespace DocumentManagement.Infrastructure.Options;

public class SpaPublicOptions
{
    public const string SectionName = "SpaPublic";

    /// <summary>Public SPA origin for password-reset links (e.g. https://app.example.com or http://localhost:4200).</summary>
    public string BaseUrl { get; set; } = "http://localhost:4200";
}
