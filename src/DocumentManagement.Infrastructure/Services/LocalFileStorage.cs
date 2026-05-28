using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using DocumentManagement.Application.Abstractions;
using DocumentManagement.Infrastructure.Options;

namespace DocumentManagement.Infrastructure.Services;

public class LocalFileStorage : IFileStorage
{
    private readonly string _absoluteRoot;
    private readonly string _deletedHierarchyRootRelative;

    public LocalFileStorage(IHostEnvironment env, IOptions<FileStorageOptions> options)
    {
        var opt = options.Value;
        var root = opt.RootPath;
        _absoluteRoot = Path.IsPathRooted(root) ? root : Path.Combine(env.ContentRootPath, root);
        Directory.CreateDirectory(_absoluteRoot);
        _deletedHierarchyRootRelative = NormalizeDeletedBase(opt.DeletedFilesRelativePath);
    }

    public async Task<string> SaveAsync(
        Stream content,
        string originalFileName,
        string contentType,
        string? relativeDirectoryUnderRoot = null,
        CancellationToken cancellationToken = default)
    {
        var ext = Path.GetExtension(originalFileName);
        var stored = $"{Guid.NewGuid():N}{ext}";
        var full = CombineFileUnderRoot(relativeDirectoryUnderRoot, stored);
        var dir = Path.GetDirectoryName(full);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        await using var fs = File.Create(full);
        await content.CopyToAsync(fs, cancellationToken);
        return stored;
    }

    public Task<Stream> OpenReadAsync(
        string storedFileName,
        string? relativeDirectoryUnderRoot = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var hierarchical = CombineFileUnderRoot(relativeDirectoryUnderRoot, storedFileName);
        if (File.Exists(hierarchical))
            return Task.FromResult<Stream>(File.OpenRead(hierarchical));

        var legacy = CombineFileUnderRoot(null, storedFileName);
        if (File.Exists(legacy))
            return Task.FromResult<Stream>(File.OpenRead(legacy));

        throw new FileNotFoundException("Stored file not found.", storedFileName);
    }

    public Task DeleteIfExistsAsync(
        string storedFileName,
        string? relativeDirectoryUnderRoot = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var hierarchical = CombineFileUnderRoot(relativeDirectoryUnderRoot, storedFileName);
        if (File.Exists(hierarchical))
            File.Delete(hierarchical);
        else
        {
            var legacy = CombineFileUnderRoot(null, storedFileName);
            if (File.Exists(legacy))
                File.Delete(legacy);
        }

        return Task.CompletedTask;
    }

    public Task MoveFileToDeletedAsync(
        string storedFileName,
        string? relativeDirectoryUnderRoot = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var fileName = Path.GetFileName(storedFileName);
        var sourceFull = GetExistingFileFullPath(relativeDirectoryUnderRoot, fileName);
        if (sourceFull == null) return Task.CompletedTask;

        var deletedRootFull = Path.GetFullPath(Path.Combine(_absoluteRoot, _deletedHierarchyRootRelative));
        var sourceNorm = Path.GetFullPath(sourceFull);
        if (IsUnderDirectory(sourceNorm, deletedRootFull))
            return Task.CompletedTask;

        var folderRel = NormalizeRelativeDirectory(relativeDirectoryUnderRoot);
        var destDir = string.IsNullOrEmpty(folderRel)
            ? Path.Combine(_absoluteRoot, _deletedHierarchyRootRelative)
            : Path.Combine(_absoluteRoot, _deletedHierarchyRootRelative, folderRel);
        Directory.CreateDirectory(destDir);
        var destFull = Path.Combine(destDir, fileName);
        if (File.Exists(destFull))
        {
            destFull = Path.Combine(
                destDir,
                $"{Path.GetFileNameWithoutExtension(fileName)}_{Guid.NewGuid():N}{Path.GetExtension(fileName)}");
        }

        destFull = Path.GetFullPath(destFull);
        FileStoragePathGuard.AssertWithinStorageRoot(_absoluteRoot, destFull);
        File.Move(sourceFull, destFull);
        return Task.CompletedTask;
    }

    private string CombineFileUnderRoot(string? relativeDirectoryUnderRoot, string storedFileName)
    {
        var fileName = Path.GetFileName(storedFileName);
        var rel = NormalizeRelativeDirectory(relativeDirectoryUnderRoot);
        var combined = string.IsNullOrEmpty(rel)
            ? Path.Combine(_absoluteRoot, fileName)
            : Path.Combine(_absoluteRoot, rel, fileName);
        var full = Path.GetFullPath(combined);
        FileStoragePathGuard.AssertWithinStorageRoot(_absoluteRoot, full);
        return full;
    }

    private string? GetExistingFileFullPath(string? relativeDirectoryUnderRoot, string fileName)
    {
        try
        {
            var hierarchical = CombineFileUnderRoot(relativeDirectoryUnderRoot, fileName);
            if (File.Exists(hierarchical)) return hierarchical;
            var legacy = CombineFileUnderRoot(null, fileName);
            if (File.Exists(legacy)) return legacy;
        }
        catch (InvalidOperationException)
        {
            return null;
        }

        return null;
    }

    private static bool IsUnderDirectory(string candidateFullPath, string directoryFullPath)
    {
        var dir = directoryFullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var cand = candidateFullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (OperatingSystem.IsWindows())
        {
            if (!cand.StartsWith(dir, StringComparison.OrdinalIgnoreCase)) return false;
            return cand.Length == dir.Length || cand[dir.Length] == Path.DirectorySeparatorChar;
        }

        if (!cand.StartsWith(dir, StringComparison.Ordinal)) return false;
        return cand.Length == dir.Length || cand[dir.Length] == Path.DirectorySeparatorChar;
    }

    private static string? NormalizeRelativeDirectory(string? relativeDirectoryUnderRoot)
    {
        if (string.IsNullOrWhiteSpace(relativeDirectoryUnderRoot)) return null;
        var parts = relativeDirectoryUnderRoot
            .Replace('/', Path.DirectorySeparatorChar)
            .Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        foreach (var p in parts)
        {
            if (p is "." or "..")
                throw new InvalidOperationException("Invalid relative directory.");
        }

        return Path.Combine(parts);
    }

    private static string NormalizeDeletedBase(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "deleted-files";
        return NormalizeRelativeDirectory(value.Trim())!;
    }
}
