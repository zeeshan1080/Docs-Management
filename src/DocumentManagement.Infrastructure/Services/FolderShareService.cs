using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using DocumentManagement.Application.Abstractions;
using DocumentManagement.Application.Sharing;
using DocumentManagement.Domain;
using DocumentManagement.Infrastructure.Data;
using DocumentManagement.Infrastructure.Data.Entities;
using DocumentManagement.Infrastructure.Identity;
using DocumentManagement.Infrastructure.Options;

namespace DocumentManagement.Infrastructure.Services;

public class FolderShareService : IFolderShareService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IEmailSender _email;
    private readonly IEmailTemplates _templates;
    private readonly SpaPublicOptions _spa;

    public FolderShareService(
        ApplicationDbContext db,
        UserManager<ApplicationUser> users,
        IEmailSender email,
        IEmailTemplates templates,
        IOptions<SpaPublicOptions> spa)
    {
        _db = db;
        _users = users;
        _email = email;
        _templates = templates;
        _spa = spa.Value;
    }

    public async Task<IReadOnlyList<FolderShareDto>> ListAsync(string userId, int folderId, CancellationToken cancellationToken = default)
    {
        if (!await IsMgmt(userId)) return Array.Empty<FolderShareDto>();

        var shares = await _db.FolderShares.AsNoTracking()
            .Include(s => s.Role)
            .Include(s => s.Location)
            .Include(s => s.User)
            .Include(s => s.CreatedByUser)
            .Where(s => s.FolderId == folderId)
            .OrderBy(s => s.ShareType)
            .ToListAsync(cancellationToken);

        return shares.Select(s => new FolderShareDto
        {
            Id = s.Id,
            FolderId = s.FolderId,
            ShareType = (ShareType)s.ShareType,
            AccessLevel = s.AccessLevel == (byte)FolderAccessLevel.ViewOnly
                ? FolderAccessLevel.ViewOnly
                : FolderAccessLevel.FullAccess,
            RoleId = s.RoleId,
            RoleName = s.Role?.Name,
            LocationId = s.LocationId,
            LocationName =
                s.Location != null && s.Location.RecordStatusLIID == RecordStatus.Active
                    ? s.Location.Name
                    : null,
            UserId = s.UserId,
            UserEmail = s.User?.Email,
            CreatedOn = s.CreatedOn,
            CreatedByUserId = s.CreatedBy,
            CreatedByDisplayName = UserDisplayName.Format(s.CreatedByUser)
        }).ToList();
    }

    public async Task<(bool Ok, string? Error, int? ShareId)> AddAsync(string userId, int folderId, CreateFolderShareRequest request, CancellationToken cancellationToken = default)
    {
        if (!await IsMgmt(userId)) return (false, "Forbidden.", null);

        var folder = await _db.Folders.FindAsync(new object[] { folderId }, cancellationToken);
        if (folder == null) return (false, "Folder not found.", null);

        if (request.AccessLevel != FolderAccessLevel.ViewOnly &&
            request.AccessLevel != FolderAccessLevel.FullAccess)
            return (false, "Invalid access level.", null);

        switch (request.ShareType)
        {
            case ShareType.Role:
                if (string.IsNullOrEmpty(request.RoleId)) return (false, "RoleId required.", null);
                if (!await _db.Roles.AnyAsync(r => r.Id == request.RoleId, cancellationToken))
                    return (false, "Invalid role.", null);
                break;
            case ShareType.Location:
                if (request.LocationId is not { } lid) return (false, "LocationId required.", null);
                if (!await _db.Locations.AnyAsync(
                        l => l.Id == lid && l.RecordStatusLIID == RecordStatus.Active,
                        cancellationToken))
                    return (false, "Invalid location.", null);
                break;
            case ShareType.User:
                if (string.IsNullOrEmpty(request.UserId)) return (false, "UserId required.", null);
                if (!await _db.Users.AnyAsync(u => u.Id == request.UserId, cancellationToken))
                    return (false, "Invalid user.", null);
                break;
            default:
                return (false, "Invalid share type.", null);
        }

        var share = new FolderShare
        {
            FolderId = folderId,
            ShareType = (byte)request.ShareType,
            AccessLevel = (byte)request.AccessLevel,
            RoleId = request.ShareType == ShareType.Role ? request.RoleId : null,
            LocationId = request.ShareType == ShareType.Location ? request.LocationId : null,
            UserId = request.ShareType == ShareType.User ? request.UserId : null,
            CreatedOn = DateTime.UtcNow,
            CreatedBy = userId
        };
        _db.FolderShares.Add(share);
        await _db.SaveChangesAsync(cancellationToken);

        var created = await _db.FolderShares.AsNoTracking()
            .Include(s => s.Role)
            .Include(s => s.Location)
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == share.Id, cancellationToken);
        if (created != null)
            await NotifyFolderAccessGrantedAsync(folderId, created, userId, cancellationToken);

        return (true, null, share.Id);
    }

    public async Task<bool> RemoveAsync(string userId, int shareId, CancellationToken cancellationToken = default)
    {
        if (!await IsMgmt(userId)) return false;
        var share = await _db.FolderShares
            .Include(s => s.Role)
            .Include(s => s.Location)
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == shareId, cancellationToken);
        if (share == null) return false;

        await NotifyFolderAccessRevokedAsync(share, userId, cancellationToken);

        _db.FolderShares.Remove(share);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task NotifyFolderAccessGrantedAsync(int folderId, FolderShare share, string managerUserId, CancellationToken ct)
    {
        var folder = await _db.Folders.AsNoTracking().FirstOrDefaultAsync(f => f.Id == folderId, ct);
        if (folder == null) return;

        var mgr = await _users.FindByIdAsync(managerUserId);
        var mgrDisplay = FormatDisplayName(mgr);
        var desc = DescribeShare(share);
        var docsUrl = $"{_spa.BaseUrl.TrimEnd('/')}/documents";
        var recipients = await CollectRecipientEmailsAsync(share, ct);
        if (recipients.Count == 0) return;

        var content = _templates.FolderAccessGranted(folder.Name, mgrDisplay, desc, docsUrl);
        foreach (var to in recipients)
            await _email.SendAsync(to, content.Subject, content.HtmlBody, ct);
    }

    private async Task NotifyFolderAccessRevokedAsync(FolderShare share, string managerUserId, CancellationToken ct)
    {
        var folder = await _db.Folders.AsNoTracking().FirstOrDefaultAsync(f => f.Id == share.FolderId, ct);
        if (folder == null) return;

        var mgr = await _users.FindByIdAsync(managerUserId);
        var mgrDisplay = FormatDisplayName(mgr);
        var desc = DescribeShare(share);
        var docsUrl = $"{_spa.BaseUrl.TrimEnd('/')}/documents";
        var recipients = await CollectRecipientEmailsAsync(share, ct);
        if (recipients.Count == 0) return;

        var content = _templates.FolderAccessRevoked(folder.Name, mgrDisplay, desc, docsUrl);
        foreach (var to in recipients)
            await _email.SendAsync(to, content.Subject, content.HtmlBody, ct);
    }

    private static string DescribeShare(FolderShare s)
    {
        var who = (ShareType)s.ShareType switch
        {
            ShareType.Role => $"Role: {s.Role?.Name?.Trim() ?? "Unknown"}",
            ShareType.Location => $"Location: {s.Location?.Name?.Trim() ?? "Unknown"}",
            ShareType.User => !string.IsNullOrEmpty(s.User?.Email) ? $"Direct user: {s.User.Email}" : "Direct user",
            _ => "Shared access"
        };
        var level = (FolderAccessLevel)s.AccessLevel == FolderAccessLevel.ViewOnly
            ? "View only"
            : "Full access";
        return $"{who} — {level}";
    }

    private static string FormatDisplayName(ApplicationUser? u)
    {
        if (u == null) return "Management";
        var d = $"{u.FirstName} {u.LastName}".Trim();
        return string.IsNullOrEmpty(d) ? u.Email ?? "Management" : d;
    }

    private async Task<List<string>> CollectRecipientEmailsAsync(FolderShare share, CancellationToken ct)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        switch ((ShareType)share.ShareType)
        {
            case ShareType.User:
                if (!string.IsNullOrEmpty(share.UserId))
                {
                    var u = await _users.FindByIdAsync(share.UserId);
                    if (!string.IsNullOrEmpty(u?.Email)) set.Add(u.Email);
                }
                break;
            case ShareType.Role:
                if (!string.IsNullOrEmpty(share.RoleId))
                {
                    var role = await _db.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == share.RoleId, ct);
                    if (!string.IsNullOrEmpty(role?.Name))
                    {
                        foreach (var u in await _users.GetUsersInRoleAsync(role.Name))
                            if (!string.IsNullOrEmpty(u.Email)) set.Add(u.Email);
                    }
                }
                break;
            case ShareType.Location:
                if (share.LocationId is { } lid)
                {
                    var userIds = await _db.UserLocations.AsNoTracking()
                        .Where(ul => ul.LocationId == lid)
                        .Select(ul => ul.UserId)
                        .Distinct()
                        .ToListAsync(ct);
                    foreach (var id in userIds)
                    {
                        var u = await _users.FindByIdAsync(id);
                        if (!string.IsNullOrEmpty(u?.Email)) set.Add(u.Email);
                    }
                }
                break;
        }
        return set.ToList();
    }

    private async Task<bool> IsMgmt(string userId)
    {
        var u = await _users.FindByIdAsync(userId);
        return u != null && await _users.IsInRoleAsync(u, AppRoles.Management);
    }
}
