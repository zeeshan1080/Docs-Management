namespace DocumentManagement.Infrastructure.Services;

internal static class DocumentViewerSupport
{
    public static bool IsSupported(string originalFileName, string? contentType)
    {
        var ext = Path.GetExtension(originalFileName).ToLowerInvariant();
        var ct = (contentType ?? "").ToLowerInvariant();
        if (ext == ".pdf" || ct.Contains("pdf")) return true;
        if (ext == ".docx" || ct.Contains("wordprocessingml")) return true;
        if (ext == ".doc" || ct == "application/msword" || ct.Contains("msword")) return true;
        if (ext == ".png" || ct.Contains("png")) return true;
        return false;
    }
}
