using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using DocumentManagement.Application.Abstractions;
using DocumentManagement.Application.Registration;
using DocumentManagement.Domain;
using DocumentManagement.Infrastructure.Data;
using DocumentManagement.Infrastructure.Data.Entities;
using DocumentManagement.Infrastructure.Identity;
using DocumentManagement.Infrastructure.Options;

namespace DocumentManagement.Infrastructure.Services;

public class RegistrationAdminService : IRegistrationAdminService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IEmailSender _email;
    private readonly IEmailTemplates _templates;
    private readonly SpaPublicOptions _spa;

    public RegistrationAdminService(
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

    public Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default) =>
        _db.EmployeeRegistrationRequests.AsNoTracking()
            .CountAsync(r => r.Status == (byte)RegistrationRequestStatus.Pending, cancellationToken);

    public async Task<IReadOnlyList<RegistrationRequestDto>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        var list = await _db.EmployeeRegistrationRequests.AsNoTracking()
            .Include(r => r.User)
            .Include(r => r.RequestedRole)
            .Include(r => r.RequestedLocation)
            .Where(r => r.Status == (byte)RegistrationRequestStatus.Pending)
            .OrderBy(r => r.CreatedOn)
            .ToListAsync(cancellationToken);

        return list.Select(r => new RegistrationRequestDto
        {
            Id = r.Id,
            UserId = r.UserId,
            UserEmail = r.User?.Email ?? "",
            UserName = r.User?.UserName ?? "",
            RequestedRoleId = r.RequestedRoleId,
            RequestedRoleName = r.RequestedRole?.Name ?? "",
            RequestedLocationId = r.RequestedLocationId,
            RequestedLocationName =
                r.RequestedLocation != null && r.RequestedLocation.RecordStatusLIID == RecordStatus.Active
                    ? r.RequestedLocation.Name
                    : "",
            Status = (RegistrationRequestStatus)r.Status,
            CreatedOn = r.CreatedOn
        }).ToList();
    }

    public async Task<(bool Ok, string? Error)> ReviewAsync(string managerUserId, int requestId, ReviewRegistrationRequest review, CancellationToken cancellationToken = default)
    {
        var mgr = await _users.FindByIdAsync(managerUserId);
        if (mgr == null || !await _users.IsInRoleAsync(mgr, AppRoles.Management))
            return (false, "Forbidden.");

        var req = await _db.EmployeeRegistrationRequests
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);
        if (req == null) return (false, "Request not found.");
        if (req.Status != (byte)RegistrationRequestStatus.Pending) return (false, "Already processed.");

        var user = req.User ?? await _users.FindByIdAsync(req.UserId);
        if (user == null) return (false, "User missing.");

        if (!review.Approve)
        {
            user.ApprovalStatus = (byte)ApprovalStatus.Rejected;
            user.LastModifiedOn = DateTime.UtcNow;
            user.LastModifiedBy = managerUserId;
            req.Status = (byte)RegistrationRequestStatus.Rejected;
            req.ReviewedByUserId = managerUserId;
            req.ReviewedAtUtc = DateTime.UtcNow;
            req.Notes = review.Notes;
            req.LastModifiedOn = DateTime.UtcNow;
            req.LastModifiedBy = managerUserId;
            await _users.UpdateAsync(user);
            await _db.SaveChangesAsync(cancellationToken);

            if (!string.IsNullOrEmpty(user.Email))
            {
                var display = EmployeeDisplayName(user);
                var content = _templates.RegistrationRejected(display, review.Notes);
                await _email.SendAsync(user.Email, content.Subject, content.HtmlBody, cancellationToken);
            }

            return (true, null);
        }

        var roleId = review.AssignedRoleId ?? req.RequestedRoleId;
        var locationId = review.AssignedLocationId ?? req.RequestedLocationId;

        var role = await _db.Roles.FindAsync(new object[] { roleId }, cancellationToken);
        if (role == null) return (false, "Invalid assigned role.");
        var loc = await _db.Locations.FirstOrDefaultAsync(
            l => l.Id == locationId && l.RecordStatusLIID == RecordStatus.Active,
            cancellationToken);
        if (loc == null) return (false, "Invalid assigned location.");

        var currentRoles = await _users.GetRolesAsync(user);
        if (currentRoles.Count > 0)
            await _users.RemoveFromRolesAsync(user, currentRoles);
        await _users.AddToRoleAsync(user, role.Name!);

        var existingLocs = _db.UserLocations.Where(ul => ul.UserId == user.Id);
        _db.UserLocations.RemoveRange(existingLocs);
        _db.UserLocations.Add(new UserLocation
        {
            UserId = user.Id,
            LocationId = locationId,
            IsPrimary = true,
            CreatedOn = DateTime.UtcNow,
            CreatedBy = managerUserId
        });

        user.ApprovalStatus = (byte)ApprovalStatus.Approved;
        user.LastModifiedOn = DateTime.UtcNow;
        user.LastModifiedBy = managerUserId;
        req.Status = (byte)RegistrationRequestStatus.Approved;
        req.ReviewedByUserId = managerUserId;
        req.ReviewedAtUtc = DateTime.UtcNow;
        req.Notes = review.Notes;
        req.LastModifiedOn = DateTime.UtcNow;
        req.LastModifiedBy = managerUserId;

        await _users.UpdateAsync(user);
        await _db.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrEmpty(user.Email))
        {
            var display = EmployeeDisplayName(user);
            var loginUrl = $"{_spa.BaseUrl.TrimEnd('/')}/login";
            var content = _templates.RegistrationApproved(
                display,
                role.Name ?? "",
                loc.Name,
                loginUrl);
            await _email.SendAsync(user.Email, content.Subject, content.HtmlBody, cancellationToken);
        }

        return (true, null);
    }

    private static string EmployeeDisplayName(ApplicationUser user)
    {
        var d = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrEmpty(d) ? user.Email ?? user.UserName ?? "there" : d;
    }
}
