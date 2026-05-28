using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using DocumentManagement.Application.Abstractions;
using DocumentManagement.Application.Admin;
using DocumentManagement.Domain;
using DocumentManagement.Infrastructure.Data;
using DocumentManagement.Infrastructure.Data.Entities;
using DocumentManagement.Infrastructure.Identity;
using DocumentManagement.Infrastructure.Options;

namespace DocumentManagement.Infrastructure.Services;

public class UserAdminService : IUserAdminService
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly RoleManager<ApplicationRole> _roles;
    private readonly ApplicationDbContext _db;
    private readonly IEmailSender _email;
    private readonly IEmailTemplates _templates;
    private readonly SpaPublicOptions _spa;

    public UserAdminService(
        UserManager<ApplicationUser> users,
        RoleManager<ApplicationRole> roles,
        ApplicationDbContext db,
        IEmailSender email,
        IEmailTemplates templates,
        IOptions<SpaPublicOptions> spa)
    {
        _users = users;
        _roles = roles;
        _db = db;
        _email = email;
        _templates = templates;
        _spa = spa.Value;
    }

    public async Task<(bool Ok, string? Error, string? UserId)> CreateUserAsync(
        CreateUserRequest request,
        string managerUserId,
        CancellationToken cancellationToken = default)
    {
        var mgr = await _users.FindByIdAsync(managerUserId);
        if (mgr == null || !await _users.IsInRoleAsync(mgr, AppRoles.Management))
            return (false, "Forbidden.", null);

        var normalizedEmail = _users.NormalizeEmail(request.Email);
        if (await _users.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken))
            return (false, "A user with this email already exists.", null);

        var role = await _roles.FindByIdAsync(request.RoleId);
        if (role == null) return (false, "Invalid role.", null);

        var loc = await _db.Locations.FirstOrDefaultAsync(
            l => l.Id == request.LocationId && l.RecordStatusLIID == RecordStatus.Active,
            cancellationToken);
        if (loc == null) return (false, "Invalid location.", null);

        var user = new ApplicationUser
        {
            UserName = request.Email.Trim(),
            Email = request.Email.Trim(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            EmailConfirmed = true,
            ApprovalStatus = (byte)ApprovalStatus.Approved,
            CreatedOn = DateTime.UtcNow,
            CreatedBy = managerUserId
        };

        var result = await _users.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return (false, string.Join(" ", result.Errors.Select(e => e.Description)), null);

        await _users.AddToRoleAsync(user, role.Name!);

        _db.UserLocations.Add(new UserLocation
        {
            UserId = user.Id,
            LocationId = request.LocationId,
            IsPrimary = true,
            CreatedOn = DateTime.UtcNow,
            CreatedBy = managerUserId
        });

        await _db.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrEmpty(user.Email))
        {
            var display = $"{user.FirstName} {user.LastName}".Trim();
            if (string.IsNullOrEmpty(display)) display = user.Email;
            var loginUrl = $"{_spa.BaseUrl.TrimEnd('/')}/login";
            var mgrDisplay = AdminDisplayName(mgr);
            var content = _templates.AccountCreatedByAdmin(
                display,
                role.Name ?? "",
                loc.Name,
                loginUrl,
                mgrDisplay);
            await _email.SendAsync(user.Email, content.Subject, content.HtmlBody, cancellationToken);
        }

        return (true, null, user.Id);
    }

    private static string AdminDisplayName(ApplicationUser? u)
    {
        if (u == null) return "Management";
        var d = $"{u.FirstName} {u.LastName}".Trim();
        return string.IsNullOrEmpty(d) ? u.Email ?? "Management" : d;
    }

    public async Task<IReadOnlyList<AdminUserListItemDto>> GetUsersAsync(
        string managerUserId,
        CancellationToken cancellationToken = default)
    {
        var mgr = await _users.FindByIdAsync(managerUserId);
        if (mgr == null || !await _users.IsInRoleAsync(mgr, AppRoles.Management))
            return Array.Empty<AdminUserListItemDto>();

        var users = await _users.Users
            .AsNoTracking()
            .OrderBy(u => u.Email)
            .ToListAsync(cancellationToken);

        var primaryLocs = await _db.UserLocations.AsNoTracking()
            .Where(ul => ul.IsPrimary)
            .ToDictionaryAsync(ul => ul.UserId, ul => ul.LocationId, cancellationToken);

        var locationNames = await _db.Locations.AsNoTracking()
            .Where(l => l.RecordStatusLIID == RecordStatus.Active)
            .ToDictionaryAsync(l => l.Id, l => l.Name, cancellationToken);

        var list = new List<AdminUserListItemDto>();
        foreach (var u in users)
        {
            var tracked = await _users.FindByIdAsync(u.Id);
            if (tracked == null) continue;
            var roles = await _users.GetRolesAsync(tracked);
            int? locId = primaryLocs.TryGetValue(u.Id, out var lid) ? lid : null;
            string? locName = locId is { } id && locationNames.TryGetValue(id, out var ln) ? ln : null;

            list.Add(new AdminUserListItemDto
            {
                Id = u.Id,
                Email = u.Email ?? "",
                FirstName = u.FirstName,
                LastName = u.LastName,
                ApprovalStatus = u.ApprovalStatus,
                Roles = roles.OrderBy(r => r).ToList(),
                PrimaryLocationId = locId,
                PrimaryLocationName = locName
            });
        }

        return list;
    }

    public async Task<(bool Ok, string? Error)> UpdateUserAsync(
        string targetUserId,
        UpdateUserRequest request,
        string managerUserId,
        CancellationToken cancellationToken = default)
    {
        var mgr = await _users.FindByIdAsync(managerUserId);
        if (mgr == null || !await _users.IsInRoleAsync(mgr, AppRoles.Management))
            return (false, "Forbidden.");

        var user = await _users.FindByIdAsync(targetUserId);
        if (user == null) return (false, "User not found.");

        if (!Enum.IsDefined(typeof(ApprovalStatus), request.ApprovalStatus))
            return (false, "Invalid approval status.");

        var newRole = await _roles.FindByIdAsync(request.RoleId);
        if (newRole == null) return (false, "Invalid role.");

        var loc = await _db.Locations.FirstOrDefaultAsync(
            l => l.Id == request.LocationId && l.RecordStatusLIID == RecordStatus.Active,
            cancellationToken);
        if (loc == null) return (false, "Invalid location.");

        var currentlyManagement = await _users.IsInRoleAsync(user, AppRoles.Management);
        var newIsManagement = string.Equals(newRole.Name, AppRoles.Management, StringComparison.Ordinal);
        if (currentlyManagement && !newIsManagement)
        {
            var mgmtUsers = await _users.GetUsersInRoleAsync(AppRoles.Management);
            if (mgmtUsers.Count <= 1)
                return (false, "There must be at least one Management user.");
        }

        var email = request.Email.Trim();
        var normalized = _users.NormalizeEmail(email);
        if (string.IsNullOrEmpty(normalized))
            return (false, "Email is required.");

        var duplicate = await _users.Users.AnyAsync(
            u => u.Id != user.Id && u.NormalizedEmail == normalized,
            cancellationToken);
        if (duplicate)
            return (false, "Another user already uses this email.");

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.ApprovalStatus = request.ApprovalStatus;
        user.LastModifiedOn = DateTime.UtcNow;
        user.LastModifiedBy = managerUserId;

        if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(user.UserName, email, StringComparison.OrdinalIgnoreCase))
        {
            user.UserName = email;
            user.Email = email;
            user.NormalizedUserName = normalized;
            user.NormalizedEmail = normalized;
        }

        var updateResult = await _users.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return (false, string.Join(" ", updateResult.Errors.Select(e => e.Description)));

        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            var token = await _users.GeneratePasswordResetTokenAsync(user);
            var pwResult = await _users.ResetPasswordAsync(user, token, request.NewPassword.Trim());
            if (!pwResult.Succeeded)
                return (false, string.Join(" ", pwResult.Errors.Select(e => e.Description)));
        }

        var existingRoles = await _users.GetRolesAsync(user);
        if (existingRoles.Count > 0)
            await _users.RemoveFromRolesAsync(user, existingRoles);
        await _users.AddToRoleAsync(user, newRole.Name!);

        var primary = await _db.UserLocations
            .FirstOrDefaultAsync(ul => ul.UserId == user.Id && ul.IsPrimary, cancellationToken);
        if (primary == null)
        {
            _db.UserLocations.Add(new UserLocation
            {
                UserId = user.Id,
                LocationId = request.LocationId,
                IsPrimary = true,
                CreatedOn = DateTime.UtcNow,
                CreatedBy = managerUserId
            });
        }
        else
        {
            primary.LocationId = request.LocationId;
            primary.LastModifiedOn = DateTime.UtcNow;
            primary.LastModifiedBy = managerUserId;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> DeleteUserAsync(
        string targetUserId,
        string managerUserId,
        CancellationToken cancellationToken = default)
    {
        var mgr = await _users.FindByIdAsync(managerUserId);
        if (mgr == null || !await _users.IsInRoleAsync(mgr, AppRoles.Management))
            return (false, "Forbidden.");

        if (string.Equals(targetUserId, managerUserId, StringComparison.Ordinal))
            return (false, "You cannot delete your own account.");

        var user = await _users.FindByIdAsync(targetUserId);
        if (user == null) return (false, "User not found.");

        if (await _users.IsInRoleAsync(user, AppRoles.Management))
        {
            var mgmtUsers = await _users.GetUsersInRoleAsync(AppRoles.Management);
            if (mgmtUsers.Count <= 1)
                return (false, "There must be at least one Management user.");
        }

        var folderShares = await _db.FolderShares.Where(fs => fs.UserId == targetUserId).ToListAsync(cancellationToken);
        _db.FolderShares.RemoveRange(folderShares);

        var docShares = await _db.DocumentShares.Where(ds => ds.UserId == targetUserId).ToListAsync(cancellationToken);
        _db.DocumentShares.RemoveRange(docShares);

        var folders = await _db.Folders.Where(f => f.CreatedBy == targetUserId).ToListAsync(cancellationToken);
        foreach (var f in folders)
        {
            f.CreatedBy = null;
            f.LastModifiedOn = DateTime.UtcNow;
            f.LastModifiedBy = managerUserId;
        }

        var documents = await _db.Documents.Where(d => d.CreatedBy == targetUserId).ToListAsync(cancellationToken);
        foreach (var d in documents)
        {
            d.CreatedBy = null;
            d.LastModifiedOn = DateTime.UtcNow;
            d.LastModifiedBy = managerUserId;
        }

        var fsCreated = await _db.FolderShares.Where(fs => fs.CreatedBy == targetUserId).ToListAsync(cancellationToken);
        foreach (var fs in fsCreated)
        {
            fs.CreatedBy = null;
            fs.LastModifiedOn = DateTime.UtcNow;
            fs.LastModifiedBy = managerUserId;
        }

        var dsCreated = await _db.DocumentShares.Where(ds => ds.CreatedBy == targetUserId).ToListAsync(cancellationToken);
        foreach (var ds in dsCreated)
        {
            ds.CreatedBy = null;
            ds.LastModifiedOn = DateTime.UtcNow;
            ds.LastModifiedBy = managerUserId;
        }

        var reviewedRegs = await _db.EmployeeRegistrationRequests
            .Where(r => r.ReviewedByUserId == targetUserId)
            .ToListAsync(cancellationToken);
        foreach (var r in reviewedRegs)
        {
            r.ReviewedByUserId = null;
            r.LastModifiedOn = DateTime.UtcNow;
            r.LastModifiedBy = managerUserId;
        }

        await _db.SaveChangesAsync(cancellationToken);

        var del = await _users.DeleteAsync(user);
        if (!del.Succeeded)
            return (false, string.Join(" ", del.Errors.Select(e => e.Description)));

        return (true, null);
    }
}
