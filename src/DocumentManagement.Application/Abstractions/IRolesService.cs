using DocumentManagement.Application.Roles;

namespace DocumentManagement.Application.Abstractions;

public interface IRolesService
{
    Task<IReadOnlyList<RoleOptionDto>> GetAllAsync(CancellationToken cancellationToken = default);
}
