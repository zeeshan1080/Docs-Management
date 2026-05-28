namespace DocumentManagement.Application.Abstractions;

public interface IFileStorage
{
    /// <param name="relativeDirectoryUnderRoot">Optional subdirectory under the storage root (mirrors folder hierarchy). Null or empty = file at root (legacy).</param>
    Task<string> SaveAsync(
        Stream content,
        string originalFileName,
        string contentType,
        string? relativeDirectoryUnderRoot = null,
        CancellationToken cancellationToken = default);

    /// <param name="relativeDirectoryUnderRoot">Subdirectory where the file was stored; null or empty = root (legacy).</param>
    Task<Stream> OpenReadAsync(
        string storedFileName,
        string? relativeDirectoryUnderRoot = null,
        CancellationToken cancellationToken = default);

    Task DeleteIfExistsAsync(
        string storedFileName,
        string? relativeDirectoryUnderRoot = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves a file from its live location to the configured deleted-files area, using the same relative directory layout.
    /// No-op if the file is missing or already under the deleted area.
    /// </summary>
    Task MoveFileToDeletedAsync(
        string storedFileName,
        string? relativeDirectoryUnderRoot = null,
        CancellationToken cancellationToken = default);
}
