using System.Linq;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DocumentManagement.Domain;
using DocumentManagement.Infrastructure.Identity;
using DocumentManagement.Infrastructure.Options;

namespace DocumentManagement.Infrastructure;

public class DevSeedHostedService : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<DevSeedHostedService> _logger;
    private readonly DevSeedOptions _options;

    public DevSeedHostedService(
        IServiceProvider services,
        IOptions<DevSeedOptions> options,
        ILogger<DevSeedHostedService> logger)
    {
        _services = services;
        _logger = logger;
        _options = options.Value;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled) return;

        using var scope = _services.CreateScope();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var name in AppRoles.All)
        {
            if (!await roles.RoleExistsAsync(name))
            {
                var r = await roles.CreateAsync(new ApplicationRole() { Name=name, CreatedOn = DateTime.UtcNow });
                if (!r.Succeeded)
                    _logger.LogWarning("Could not create role {Role}", name);
            }
        }

        if (await users.Users.AnyAsync(u => u.NormalizedEmail == users.NormalizeEmail(_options.AdminEmail), cancellationToken))
            return;

        var admin = new ApplicationUser
        {
            Email = _options.AdminEmail,
            UserName = _options.AdminEmail,
            EmailConfirmed = true,
            FirstName = "Admin",
            LastName = "User",
            ApprovalStatus = (byte)ApprovalStatus.Approved,
            CreatedOn = DateTime.UtcNow
        };

        var created = await users.CreateAsync(admin, _options.AdminPassword);
        if (!created.Succeeded)
        {
            _logger.LogWarning("Dev admin user not created: {Errors}", string.Join(",", created.Errors.Select(e => e.Description)));
            return;
        }

        await users.AddToRoleAsync(admin, AppRoles.Management);
        _logger.LogInformation("Dev admin user {Email} created with Management role.", _options.AdminEmail);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
