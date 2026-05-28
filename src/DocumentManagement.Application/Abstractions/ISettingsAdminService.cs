using DocumentManagement.Application.Admin;

namespace DocumentManagement.Application.Abstractions;

public interface ISettingsAdminService
{
    Task<(bool Ok, string? Error, int? Id)> AddLocationAsync(
        string managerUserId,
        CreateLocationRequest request,
        CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error)> UpdateLocationAsync(
        string managerUserId,
        int locationId,
        UpdateLocationRequest request,
        CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error, string? RoleId)> AddRoleAsync(
        string managerUserId,
        CreateRoleRequest request,
        CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error)> UpdateRoleAsync(
        string managerUserId,
        string roleId,
        UpdateRoleRequest request,
        CancellationToken cancellationToken = default);
}
