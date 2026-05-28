using Microsoft.EntityFrameworkCore;
using DocumentManagement.Application.Abstractions;
using DocumentManagement.Domain;
using DocumentManagement.Infrastructure.Data;
using DocumentManagement.Infrastructure.Data.Entities;

namespace DocumentManagement.Infrastructure.Services;

/// <summary>
/// Resolves folder/document visibility using EF Core (replaces SQL TVFs and stored procedures).
/// </summary>
public class EfAccessControlService : IAccessControlService
{
    private const string ManagementRoleNormalized = "MANAGEMENT";

    private readonly ApplicationDbContext _db;

    private string? _cachedUserId;
    private AccessSets? _cachedSets;

    public EfAccessControlService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlySet<int>> GetAccessibleFolderIdsAsync(
        string userId,
        CancellationToken cancellationToken = default) =>
        (await GetSetsAsync(userId, cancellationToken)).Navigable;

    public async Task<IReadOnlySet<int>> GetFullAccessFolderIdsAsync(
        string userId,
        CancellationToken cancellationToken = default) =>
        (await GetSetsAsync(userId, cancellationToken)).Full;

    public async Task<IReadOnlySet<int>> GetViewOnlyFolderIdsAsync(
        string userId,
        CancellationToken cancellationToken = default) =>
        (await GetSetsAsync(userId, cancellationToken)).ViewOnly;

    public async Task<IReadOnlyList<int>> GetDocumentShareIdsInFolderAsync(
        string userId,
        int folderId,
        CancellationToken cancellationToken = default)
    {
        var roleIds = await GetUserRoleIdsAsync(userId, cancellationToken);
        var locationIds = await GetUserLocationIdsAsync(userId, cancellationToken);

        return await (
            from ds in _db.DocumentShares.AsNoTracking()
            join d in _db.Documents.AsNoTracking() on ds.DocumentId equals d.Id
            where d.FolderId == folderId
                  && !d.IsDeleted
                  && ((ds.ShareType == 3 && ds.UserId == userId)
                      || (ds.ShareType == 1 && ds.RoleId != null && roleIds.Contains(ds.RoleId))
                      || (ds.ShareType == 2 && ds.LocationId != null && locationIds.Contains(ds.LocationId.Value)))
            select d.Id).Distinct().ToListAsync(cancellationToken);
    }

    public async Task<bool> CanAccessFolderAsync(string userId, int folderId, CancellationToken cancellationToken = default)
    {
        var sets = await GetSetsAsync(userId, cancellationToken);
        return sets.Full.Contains(folderId);
    }

    public async Task<bool> CanAccessDocumentAsync(string userId, int documentId, CancellationToken cancellationToken = default)
    {
        var doc = await _db.Documents.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == documentId && !d.IsDeleted, cancellationToken);
        if (doc == null) return false;

        var sets = await GetSetsAsync(userId, cancellationToken);
        if (sets.Full.Contains(doc.FolderId) || sets.ViewOnly.Contains(doc.FolderId)) return true;

        var roleIds = await GetUserRoleIdsAsync(userId, cancellationToken);
        var locationIds = await GetUserLocationIdsAsync(userId, cancellationToken);

        return await _db.DocumentShares.AsNoTracking()
            .AnyAsync(
                ds => ds.DocumentId == documentId
                      && ((ds.ShareType == 3 && ds.UserId == userId)
                          || (ds.ShareType == 1 && ds.RoleId != null && roleIds.Contains(ds.RoleId))
                          || (ds.ShareType == 2 && ds.LocationId != null && locationIds.Contains(ds.LocationId.Value))),
                cancellationToken);
    }

    private async Task<AccessSets> GetSetsAsync(string userId, CancellationToken cancellationToken)
    {
        if (_cachedUserId == userId && _cachedSets != null) return _cachedSets;

        _cachedSets = await ComputeAccessSetsAsync(userId, cancellationToken);
        _cachedUserId = userId;
        return _cachedSets;
    }

    private async Task<AccessSets> ComputeAccessSetsAsync(string userId, CancellationToken cancellationToken)
    {
        var folders = await _db.Folders.AsNoTracking()
            .Select(f => new FolderRow(f.Id, f.ParentFolderId))
            .ToListAsync(cancellationToken);

        if (folders.Count == 0)
            return new AccessSets(new HashSet<int>(), new HashSet<int>(), new HashSet<int>());

        var allIds = folders.Select(f => f.Id).ToHashSet();

        if (await IsManagementUserAsync(userId, cancellationToken))
            return new AccessSets(allIds, new HashSet<int>(), allIds);

        var roleIds = await GetUserRoleIdsAsync(userId, cancellationToken);
        var locationIds = await GetUserLocationIdsAsync(userId, cancellationToken);

        var fullRoots = await _db.FolderShares.AsNoTracking()
            .Where(fs => fs.AccessLevel == (byte)FolderAccessLevel.FullAccess)
            .Where(fs =>
                (fs.ShareType == 3 && fs.UserId == userId)
                || (fs.ShareType == 1 && fs.RoleId != null && roleIds.Contains(fs.RoleId))
                || (fs.ShareType == 2 && fs.LocationId != null && locationIds.Contains(fs.LocationId.Value)))
            .Select(fs => fs.FolderId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var viewRoots = await _db.FolderShares.AsNoTracking()
            .Where(fs => fs.AccessLevel == (byte)FolderAccessLevel.ViewOnly)
            .Where(fs =>
                (fs.ShareType == 3 && fs.UserId == userId)
                || (fs.ShareType == 1 && fs.RoleId != null && roleIds.Contains(fs.RoleId))
                || (fs.ShareType == 2 && fs.LocationId != null && locationIds.Contains(fs.LocationId.Value)))
            .Select(fs => fs.FolderId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var navigableShareRoots = await _db.FolderShares.AsNoTracking()
            .Where(fs =>
                fs.AccessLevel == (byte)FolderAccessLevel.ViewOnly
                || fs.AccessLevel == (byte)FolderAccessLevel.FullAccess)
            .Where(fs =>
                (fs.ShareType == 3 && fs.UserId == userId)
                || (fs.ShareType == 1 && fs.RoleId != null && roleIds.Contains(fs.RoleId))
                || (fs.ShareType == 2 && fs.LocationId != null && locationIds.Contains(fs.LocationId.Value)))
            .Select(fs => fs.FolderId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var fullSet = ExpandSubtree(folders, fullRoots);
        var viewSet = ExpandSubtree(folders, viewRoots);
        var navigableFromShares = ExpandSubtree(folders, navigableShareRoots);

        var docFolderIds = await (
            from ds in _db.DocumentShares.AsNoTracking()
            join d in _db.Documents.AsNoTracking() on ds.DocumentId equals d.Id
            where !d.IsDeleted
                  && ((ds.ShareType == 3 && ds.UserId == userId)
                      || (ds.ShareType == 1 && ds.RoleId != null && roleIds.Contains(ds.RoleId))
                      || (ds.ShareType == 2 && ds.LocationId != null && locationIds.Contains(ds.LocationId.Value)))
            select d.FolderId).Distinct().ToListAsync(cancellationToken);

        var parentById = folders.ToDictionary(f => f.Id, f => f.ParentFolderId);
        foreach (var fid in docFolderIds)
            AddAncestors(navigableFromShares, parentById, fid);

        return new AccessSets(fullSet, viewSet, navigableFromShares);
    }

    private readonly record struct FolderRow(int Id, int? ParentFolderId);

    private static void AddAncestors(
        HashSet<int> target,
        IReadOnlyDictionary<int, int?> parentById,
        int folderId)
    {
        var current = (int?)folderId;
        var guard = 0;
        while (current.HasValue && guard++ < 10_000)
        {
            if (!target.Add(current.Value)) break;
            if (!parentById.TryGetValue(current.Value, out var parent)) break;
            current = parent;
        }
    }

    private static HashSet<int> ExpandSubtree(IReadOnlyList<FolderRow> folders, IReadOnlyList<int> rootIds)
    {
        var children = new Dictionary<int, List<int>>();
        foreach (var row in folders)
        {
            if (row.ParentFolderId is { } p)
            {
                if (!children.TryGetValue(p, out var list))
                {
                    list = new List<int>();
                    children[p] = list;
                }

                list.Add(row.Id);
            }
        }

        var result = new HashSet<int>();
        var q = new Queue<int>(rootIds);
        while (q.Count > 0)
        {
            var id = q.Dequeue();
            if (!result.Add(id)) continue;
            if (!children.TryGetValue(id, out var ch)) continue;
            foreach (var c in ch) q.Enqueue(c);
        }

        return result;
    }

    private async Task<bool> IsManagementUserAsync(string userId, CancellationToken cancellationToken) =>
        await _db.UserRoles
            .Where(ur => ur.UserId == userId)
            .Join(
                _db.Roles.Where(r => r.NormalizedName == ManagementRoleNormalized),
                ur => ur.RoleId,
                r => r.Id,
                (_, _) => 1)
            .AnyAsync(cancellationToken);

    private async Task<HashSet<string>> GetUserRoleIdsAsync(string userId, CancellationToken cancellationToken)
    {
        var list = await _db.UserRoles.AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync(cancellationToken);
        return list.ToHashSet();
    }

    private async Task<HashSet<int>> GetUserLocationIdsAsync(string userId, CancellationToken cancellationToken)
    {
        var list = await _db.UserLocations.AsNoTracking()
            .Where(ul => ul.UserId == userId)
            .Join(
                _db.Locations.AsNoTracking().Where(l => l.RecordStatusLIID == RecordStatus.Active),
                ul => ul.LocationId,
                l => l.Id,
                (ul, _) => ul.LocationId)
            .ToListAsync(cancellationToken);
        return list.ToHashSet();
    }

    private sealed class AccessSets
    {
        public AccessSets(HashSet<int> full, HashSet<int> viewOnly, HashSet<int> navigable)
        {
            Full = full;
            ViewOnly = viewOnly;
            Navigable = navigable;
        }

        public HashSet<int> Full { get; }
        public HashSet<int> ViewOnly { get; }
        public HashSet<int> Navigable { get; }
    }
}
