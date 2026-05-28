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

public class DocumentShareService : IDocumentShareService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IEmailSender _email;
    private readonly IEmailTemplates _templates;
    private readonly SpaPublicOptions _spa;

    public DocumentShareService(
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

    public async Task<IReadOnlyList<DocumentShareDto>> ListAsync(string userId, int documentId, CancellationToken cancellationToken = default)
    {
        if (!await IsMgmt(userId)) return Array.Empty<DocumentShareDto>();

        var shares = await _db.DocumentShares.AsNoTracking()
            .Include(s => s.Role)
            .Include(s => s.Location)
            .Include(s => s.User)
            .Include(s => s.CreatedByUser)
            .Where(s => s.DocumentId == documentId)
            .OrderBy(s => s.ShareType)
            .ToListAsync(cancellationToken);

        return shares.Select(s => new DocumentShareDto
        {
            Id = s.Id,
            DocumentId = s.DocumentId,
            ShareType = (ShareType)s.ShareType,
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

    public async Task<(bool Ok, string? Error, int? ShareId)> AddAsync(string userId, int documentId, CreateDocumentShareRequest request, CancellationToken cancellationToken = default)
    {
        if (!await IsMgmt(userId)) return (false, "Forbidden.", null);

        var doc = await _db.Documents.AsNoTracking().FirstOrDefaultAsync(d => d.Id == documentId && !d.IsDeleted, cancellationToken);
        if (doc == null) return (false, "Document not found.", null);

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

        var share = new DocumentShare
        {
            DocumentId = documentId,
            ShareType = (byte)request.ShareType,
            RoleId = request.ShareType == ShareType.Role ? request.RoleId : null,
            LocationId = request.ShareType == ShareType.Location ? request.LocationId : null,
            UserId = request.ShareType == ShareType.User ? request.UserId : null,
            CreatedOn = DateTime.UtcNow,
            CreatedBy = userId
        };
        _db.DocumentShares.Add(share);
        await _db.SaveChangesAsync(cancellationToken);

        var created = await _db.DocumentShares.AsNoTracking()
            .Include(s => s.Role)
            .Include(s => s.Location)
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == share.Id, cancellationToken);
        if (created != null)
            await NotifyDocumentAccessGrantedAsync(documentId, created, userId, cancellationToken);

        return (true, null, share.Id);
    }

    public async Task<bool> RemoveAsync(string userId, int documentId, int shareId, CancellationToken cancellationToken = default)
    {
        if (!await IsMgmt(userId)) return false;
        var share = await _db.DocumentShares
            .Include(s => s.Role)
            .Include(s => s.Location)
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == shareId, cancellationToken);
        if (share == null || share.DocumentId != documentId) return false;

        await NotifyDocumentAccessRevokedAsync(share, userId, cancellationToken);

        _db.DocumentShares.Remove(share);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task NotifyDocumentAccessGrantedAsync(int documentId, DocumentShare share, string managerUserId, CancellationToken ct)
    {
        var doc = await _db.Documents.AsNoTracking().Include(d => d.Folder).FirstOrDefaultAsync(d => d.Id == documentId, ct);
        if (doc == null) return;

        var mgr = await _users.FindByIdAsync(managerUserId);
        var mgrDisplay = FormatDisplayName(mgr);
        var desc = DescribeShare(share);
        var docsUrl = $"{_spa.BaseUrl.TrimEnd('/')}/documents";
        var recipients = await CollectRecipientEmailsAsync(share, ct);
        if (recipients.Count == 0) return;

        var folderName = doc.Folder?.Name?.Trim() ?? "Folder";
        var content = _templates.DocumentAccessGranted(doc.OriginalFileName, folderName, mgrDisplay, desc, docsUrl);
        foreach (var to in recipients)
            await _email.SendAsync(to, content.Subject, content.HtmlBody, ct);
    }

    private async Task NotifyDocumentAccessRevokedAsync(DocumentShare share, string managerUserId, CancellationToken ct)
    {
        var doc = await _db.Documents.AsNoTracking().Include(d => d.Folder).FirstOrDefaultAsync(d => d.Id == share.DocumentId, ct);
        if (doc == null) return;

        var mgr = await _users.FindByIdAsync(managerUserId);
        var mgrDisplay = FormatDisplayName(mgr);
        var desc = DescribeShare(share);
        var docsUrl = $"{_spa.BaseUrl.TrimEnd('/')}/documents";
        var recipients = await CollectRecipientEmailsAsync(share, ct);
        if (recipients.Count == 0) return;

        var folderName = doc.Folder?.Name?.Trim() ?? "Folder";
        var content = _templates.DocumentAccessRevoked(doc.OriginalFileName, folderName, mgrDisplay, desc, docsUrl);
        foreach (var to in recipients)
            await _email.SendAsync(to, content.Subject, content.HtmlBody, ct);
    }

    private static string DescribeShare(DocumentShare s) => (ShareType)s.ShareType switch
    {
        ShareType.Role => $"Role: {s.Role?.Name?.Trim() ?? "Unknown"}",
        ShareType.Location => $"Location: {s.Location?.Name?.Trim() ?? "Unknown"}",
        ShareType.User => !string.IsNullOrEmpty(s.User?.Email) ? $"Direct user: {s.User.Email}" : "Direct user",
        _ => "Shared access"
    };

    private static string FormatDisplayName(ApplicationUser? u)
    {
        if (u == null) return "Management";
        var d = $"{u.FirstName} {u.LastName}".Trim();
        return string.IsNullOrEmpty(d) ? u.Email ?? "Management" : d;
    }

    private async Task<List<string>> CollectRecipientEmailsAsync(DocumentShare share, CancellationToken ct)
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
