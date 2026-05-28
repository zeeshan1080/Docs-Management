namespace DocumentManagement.Application.Abstractions;

/// <summary>
/// Maps logical folders to directories under the configured file storage root and keeps disk layout in sync.
/// </summary>
public interface IFolderPhysicalStorage
{
    /// <summary>Relative path from storage root using sanitized folder names (no leading slash).</summary>
    Task<string> GetRelativePathAsync(int folderId, CancellationToken cancellationToken = default);

    Task EnsureDirectoryExistsAsync(int folderId, CancellationToken cancellationToken = default);

    Task TryMoveDirectoryAsync(string oldRelativePath, string newRelativePath, CancellationToken cancellationToken = default);

    /// <summary>Deletes the directory only if it exists and is empty (no files or subfolders).</summary>
    Task TryDeleteEmptyDirectoryAsync(string relativePathUnderRoot, CancellationToken cancellationToken = default);
}
