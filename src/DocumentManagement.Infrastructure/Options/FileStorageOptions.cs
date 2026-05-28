namespace DocumentManagement.Infrastructure.Options;

public class FileStorageOptions
{
    public const string SectionName = "FileStorage";
    public string RootPath { get; set; } = "uploads";

    /// <summary>
    /// Path under the storage root where soft-deleted files are moved, preserving the same folder segments as the live tree.
    /// </summary>
    public string DeletedFilesRelativePath { get; set; } = "deleted-files";

    public long MaxUploadBytes { get; set; } = 200 * 1024 * 1024;

    /// <summary>Max size of one chunk in a chunked upload (single request). Must be within server multipart limits.</summary>
    public long MaxChunkUploadBytes { get; set; } = 16 * 1024 * 1024;
}
