using DocumentManagement.Application.Admin;

namespace DocumentManagement.Application.Abstractions;

public interface IUserAdminService
{
    Task<(bool Ok, string? Error, string? UserId)> CreateUserAsync(
        CreateUserRequest request,
        string managerUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminUserListItemDto>> GetUsersAsync(
        string managerUserId,
        CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error)> UpdateUserAsync(
        string targetUserId,
        UpdateUserRequest request,
        string managerUserId,
        CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error)> DeleteUserAsync(
        string targetUserId,
        string managerUserId,
        CancellationToken cancellationToken = default);
}
