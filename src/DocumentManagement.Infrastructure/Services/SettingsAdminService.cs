using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DocumentManagement.Application.Abstractions;
using DocumentManagement.Application.Admin;
using DocumentManagement.Application.Folders;
using DocumentManagement.Domain;
using DocumentManagement.Infrastructure.Data;
using DocumentManagement.Infrastructure.Data.Entities;
using DocumentManagement.Infrastructure.Identity;

namespace DocumentManagement.Infrastructure.Services;

public class SettingsAdminService : ISettingsAdminService
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly RoleManager<ApplicationRole> _roles;
    private readonly ApplicationDbContext _db;
    private readonly IFolderPhysicalStorage _folderPhysical;
    private readonly IFolderService _folders;

    public SettingsAdminService(
        UserManager<ApplicationUser> users,
        RoleManager<ApplicationRole> roles,
        ApplicationDbContext db,
        IFolderPhysicalStorage folderPhysical,
        IFolderService folders)
    {
        _users = users;
        _roles = roles;
        _db = db;
        _folderPhysical = folderPhysical;
        _folders = folders;
    }

    public async Task<(bool Ok, string? Error, int? Id)> AddLocationAsync(
        string managerUserId,
        CreateLocationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await IsManagementAsync(managerUserId, cancellationToken))
            return (false, "Forbidden.", null);

        var name = request.Name.Trim();
        if (string.IsNullOrEmpty(name))
            return (false, "Name is required.", null);

        var entity = new Location
        {
            Name = name,
            Code = "",
            RecordStatusLIID = request.Inactive ? RecordStatus.Inactive : RecordStatus.Active,
            CreatedOn = DateTime.UtcNow,
            CreatedBy = managerUserId
        };
        _db.Locations.Add(entity);

        // Keep folders aligned with locations: under the default folder when one exists, otherwise at root (legacy).
        var defaultParentId = await _db.Folders.AsNoTracking()
            .Where(f => f.IsDefault)
            .OrderBy(f => f.Id)
            .Select(f => (int?)f.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var hasSiblingFolder = await _db.Folders.AnyAsync(
            f => f.ParentFolderId == defaultParentId && f.Name == name,
            cancellationToken);
        Folder? locationFolder = null;
        if (!hasSiblingFolder)
        {
            locationFolder = new Folder
            {
                Name = name,
                ParentFolderId = defaultParentId,
                CreatedOn = DateTime.UtcNow,
                CreatedBy = managerUserId
            };
            _db.Folders.Add(locationFolder);
        }

        await _db.SaveChangesAsync(cancellationToken);
        if (locationFolder != null)
            await _folderPhysical.EnsureDirectoryExistsAsync(locationFolder.Id, cancellationToken);
        return (true, null, entity.Id);
    }

    public async Task<(bool Ok, string? Error)> UpdateLocationAsync(
        string managerUserId,
        int locationId,
        UpdateLocationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await IsManagementAsync(managerUserId, cancellationToken))
            return (false, "Forbidden.");

        var newName = request.Name.Trim();
        if (string.IsNullOrEmpty(newName))
            return (false, "Name is required.");
        if (newName.Length > 200)
            return (false, "Name is too long.");

        var location = await _db.Locations.FirstOrDefaultAsync(l => l.Id == locationId, cancellationToken);
        if (location == null)
            return (false, "Location not found.");

        var desiredStatus = request.Inactive ? RecordStatus.Inactive : RecordStatus.Active;
        var nameUnchanged = string.Equals(location.Name, newName, StringComparison.Ordinal);
        if (nameUnchanged && location.RecordStatusLIID == desiredStatus)
            return (true, null);

        if (!nameUnchanged)
        {
            var defaultParentId = await _db.Folders.AsNoTracking()
                .Where(f => f.IsDefault)
                .OrderBy(f => f.Id)
                .Select(f => (int?)f.Id)
                .FirstOrDefaultAsync(cancellationToken);

            var linkedFolders = await _db.Folders
                .Where(f => f.ParentFolderId == defaultParentId && f.Name == location.Name)
                .ToListAsync(cancellationToken);

            if (linkedFolders.Count == 1)
            {
                var renamed = await _folders.RenameAsync(
                    managerUserId,
                    linkedFolders[0].Id,
                    new RenameFolderRequest { Name = newName },
                    cancellationToken);
                if (renamed == null)
                    return (false, "Could not rename the linked folder. Another folder may already use this name.");
            }

            location.Name = newName;
        }

        location.RecordStatusLIID = desiredStatus;
        location.LastModifiedOn = DateTime.UtcNow;
        location.LastModifiedBy = managerUserId;
        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Ok, string? Error, string? RoleId)> AddRoleAsync(
        string managerUserId,
        CreateRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await IsManagementAsync(managerUserId, cancellationToken))
            return (false, "Forbidden.", null);

        var name = request.Name.Trim();
        if (string.IsNullOrEmpty(name))
            return (false, "Role name is required.", null);

        if (await _roles.RoleExistsAsync(name))
            return (false, "A role with this name already exists.", null);

        var role = new ApplicationRole()
        {
            Name=name,
            CreatedOn = DateTime.UtcNow,
            CreatedBy = managerUserId
        };
        var result = await _roles.CreateAsync(role);
        if (!result.Succeeded)
            return (false, string.Join(" ", result.Errors.Select(e => e.Description)), null);

        return (true, null, role.Id);
    }

    public async Task<(bool Ok, string? Error)> UpdateRoleAsync(
        string managerUserId,
        string roleId,
        UpdateRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await IsManagementAsync(managerUserId, cancellationToken))
            return (false, "Forbidden.");

        if (string.IsNullOrWhiteSpace(roleId))
            return (false, "Role not found.");

        var role = await _roles.FindByIdAsync(roleId);
        if (role == null)
            return (false, "Role not found.");

        if (string.Equals(role.Name, AppRoles.Management, StringComparison.Ordinal))
            return (false, "The Management role cannot be renamed.");

        var name = request.Name.Trim();
        if (string.IsNullOrEmpty(name))
            return (false, "Role name is required.");
        if (name.Length > 256)
            return (false, "Role name is too long.");

        if (string.Equals(role.Name, name, StringComparison.Ordinal))
            return (true, null);

        var existing = await _roles.FindByNameAsync(name);
        if (existing != null && existing.Id != role.Id)
            return (false, "A role with this name already exists.");

        role.Name = name;
        role.LastModifiedOn = DateTime.UtcNow;
        role.LastModifiedBy = managerUserId;
        var result = await _roles.UpdateAsync(role);
        if (!result.Succeeded)
            return (false, string.Join(" ", result.Errors.Select(e => e.Description)));

        return (true, null);
    }

    private async Task<bool> IsManagementAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await _users.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null) return false;
        return await _users.IsInRoleAsync(user, AppRoles.Management);
    }
}
