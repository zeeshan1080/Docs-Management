using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using DocumentManagement.Application.Abstractions;
using DocumentManagement.Infrastructure.Data;
using DocumentManagement.Infrastructure.Options;

namespace DocumentManagement.Infrastructure.Services;

public class FolderPhysicalStorage : IFolderPhysicalStorage
{
    private readonly ApplicationDbContext _db;
    private readonly string _absoluteRoot;

    public FolderPhysicalStorage(ApplicationDbContext db, IHostEnvironment env, IOptions<FileStorageOptions> options)
    {
        _db = db;
        var root = options.Value.RootPath;
        _absoluteRoot = Path.IsPathRooted(root) ? root : Path.Combine(env.ContentRootPath, root);
    }

    public async Task<string> GetRelativePathAsync(int folderId, CancellationToken cancellationToken = default)
    {
        var segments = new List<string>();
        int? cur = folderId;
        while (cur is { } cid)
        {
            var row = await _db.Folders.AsNoTracking()
                .Where(f => f.Id == cid)
                .Select(f => new { f.Name, f.ParentFolderId })
                .FirstOrDefaultAsync(cancellationToken);
            if (row == null) break;
            segments.Insert(0, SanitizeSegment(row.Name));
            cur = row.ParentFolderId;
        }

        return segments.Count == 0 ? "" : Path.Combine(segments.ToArray());
    }

    public async Task EnsureDirectoryExistsAsync(int folderId, CancellationToken cancellationToken = default)
    {
        var rel = await GetRelativePathAsync(folderId, cancellationToken);
        if (string.IsNullOrEmpty(rel)) return;
        var full = SafeCombineUnderRoot(rel);
        Directory.CreateDirectory(full);
    }

    public Task TryMoveDirectoryAsync(string oldRelativePath, string newRelativePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(oldRelativePath) || string.IsNullOrWhiteSpace(newRelativePath))
            return Task.CompletedTask;
        if (string.Equals(oldRelativePath, newRelativePath, StringComparison.OrdinalIgnoreCase))
            return Task.CompletedTask;

        var oldFull = SafeCombineUnderRoot(oldRelativePath);
        var newFull = SafeCombineUnderRoot(newRelativePath);
        if (!Directory.Exists(oldFull)) return Task.CompletedTask;
        if (Directory.Exists(newFull)) return Task.CompletedTask;

        var parent = Path.GetDirectoryName(newFull);
        if (!string.IsNullOrEmpty(parent))
            Directory.CreateDirectory(parent);

        Directory.Move(oldFull, newFull);
        return Task.CompletedTask;
    }

    public Task TryDeleteEmptyDirectoryAsync(string relativePathUnderRoot, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(relativePathUnderRoot)) return Task.CompletedTask;

        var full = SafeCombineUnderRoot(relativePathUnderRoot);
        if (!Directory.Exists(full)) return Task.CompletedTask;
        if (Directory.EnumerateFileSystemEntries(full).Any()) return Task.CompletedTask;

        Directory.Delete(full);
        return Task.CompletedTask;
    }

    private string SafeCombineUnderRoot(string relativePath)
    {
        var normalized = NormalizeRelativePath(relativePath);
        var combined = Path.Combine(_absoluteRoot, normalized);
        var full = Path.GetFullPath(combined);
        FileStoragePathGuard.AssertWithinStorageRoot(_absoluteRoot, full);
        return full;
    }

    private static string NormalizeRelativePath(string relativePath)
    {
        var parts = relativePath
            .Replace('/', Path.DirectorySeparatorChar)
            .Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        foreach (var p in parts)
        {
            if (p is "." or "..")
                throw new InvalidOperationException("Invalid relative path.");
        }

        return Path.Combine(parts);
    }

    private static string SanitizeSegment(string name)
    {
        var trimmed = name.Trim();
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(trimmed.Length);
        foreach (var c in trimmed)
            sb.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);

        var s = sb.ToString().Trim('.', ' ');
        return string.IsNullOrEmpty(s) ? "folder" : s;
    }
}
