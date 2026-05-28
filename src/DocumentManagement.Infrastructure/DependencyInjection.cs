using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DocumentManagement.Application.Abstractions;
using DocumentManagement.Infrastructure.Data;
using DocumentManagement.Infrastructure.Identity;
using DocumentManagement.Infrastructure.Options;
using DocumentManagement.Infrastructure.Services;

namespace DocumentManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FileStorageOptions>(configuration.GetSection(FileStorageOptions.SectionName));
        services.Configure<DocumentViewerOptions>(configuration.GetSection(DocumentViewerOptions.SectionName));
        services.Configure<DevSeedOptions>(configuration.GetSection(DevSeedOptions.SectionName));
        services.Configure<SpaPublicOptions>(configuration.GetSection(SpaPublicOptions.SectionName));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services
            .AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;
                options.Password.RequiredUniqueChars = 1;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.AddSingleton<IEmailTemplates, EmailTemplates>();
        services.AddScoped<IAccessControlService, EfAccessControlService>();
        services.AddScoped<IAuditService, EfAuditService>();
        services.AddScoped<IFileStorage, LocalFileStorage>();
        services.AddScoped<IFolderPhysicalStorage, FolderPhysicalStorage>();
        services.AddScoped<IEmailSender>(sp =>
        {
            var provider = sp.GetRequiredService<IOptions<EmailOptions>>().Value.Provider?.Trim().ToLowerInvariant();
            return provider switch
            {
                "smtp" or "gmailsmtp" => new SmtpEmailSender(
                    sp.GetRequiredService<IOptions<EmailOptions>>()),
                _ => new LoggingEmailSender(sp.GetRequiredService<ILogger<LoggingEmailSender>>()),
            };
        });
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IFolderService, FolderService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IFolderShareService, FolderShareService>();
        services.AddScoped<IDocumentShareService, DocumentShareService>();
        services.AddScoped<IRegistrationAdminService, RegistrationAdminService>();
        services.AddScoped<ILocationService, LocationService>();
        services.AddScoped<IRolesService, RolesService>();
        services.AddScoped<IUserAdminService, UserAdminService>();
        services.AddScoped<ISettingsAdminService, SettingsAdminService>();
        services.AddHostedService<DevSeedHostedService>();

        return services;
    }
}
