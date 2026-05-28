namespace DocumentManagement.Infrastructure.Services;

internal static class FileStoragePathGuard
{
    /// <summary>Ensures <paramref name="resolvedFullPath"/> is the storage root or a path under it.</summary>
    public static void AssertWithinStorageRoot(string absoluteRoot, string resolvedFullPath)
    {
        var root = Path.GetFullPath(absoluteRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var full = Path.GetFullPath(resolvedFullPath);
        var rel = Path.GetRelativePath(root, full);
        if (rel.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            || string.Equals(rel, "..", StringComparison.Ordinal))
            throw new InvalidOperationException("Resolved path leaves the storage root.");
    }
}
