using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DocumentManagement.Application.Abstractions;
using DocumentManagement.Application.Roles;
using DocumentManagement.Infrastructure.Identity;

namespace DocumentManagement.Infrastructure.Services;

public class RolesService : IRolesService
{
    private readonly RoleManager<ApplicationRole> _roles;

    public RolesService(RoleManager<ApplicationRole> roles) => _roles = roles;

    public async Task<IReadOnlyList<RoleOptionDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _roles.Roles.AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => new RoleOptionDto { Id = r.Id, Name = r.Name! })
            .ToListAsync(cancellationToken);
    }
}
