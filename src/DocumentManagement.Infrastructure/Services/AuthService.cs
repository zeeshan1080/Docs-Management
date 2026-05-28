using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using DocumentManagement.Application.Abstractions;
using DocumentManagement.Application.Auth;
using DocumentManagement.Domain;
using DocumentManagement.Infrastructure.Data;
using DocumentManagement.Infrastructure.Data.Entities;
using DocumentManagement.Infrastructure.Identity;
using DocumentManagement.Infrastructure.Options;

namespace DocumentManagement.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly RoleManager<ApplicationRole> _roles;
    private readonly ITokenIssuer _tokens;
    private readonly IEmailSender _email;
    private readonly IEmailTemplates _templates;
    private readonly ApplicationDbContext _db;
    private readonly SpaPublicOptions _spa;
    private readonly EmailOptions _emailOptions;

    public AuthService(
        UserManager<ApplicationUser> users,
        RoleManager<ApplicationRole> roles,
        ITokenIssuer tokens,
        IEmailSender email,
        IEmailTemplates templates,
        ApplicationDbContext db,
        IOptions<SpaPublicOptions> spa,
        IOptions<EmailOptions> emailOptions)
    {
        _users = users;
        _roles = roles;
        _tokens = tokens;
        _email = email;
        _templates = templates;
        _db = db;
        _spa = spa.Value;
        _emailOptions = emailOptions.Value;
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _users.FindByEmailAsync(request.Email);
        if (user == null) return null;
        if (!await _users.CheckPasswordAsync(user, request.Password)) return null;
        return await BuildAuthResponseAsync(user, cancellationToken);
    }

    public async Task<(bool Success, string? Error)> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var role = await _roles.FindByNameAsync(request.RequestedRoleName);
        if (role == null)
            return (false, "Invalid role.");

        var loc = await _db.Locations.FindAsync(new object[] { request.RequestedLocationId }, cancellationToken);
        if (loc == null || loc.RecordStatusLIID != RecordStatus.Active)
            return (false, "Invalid location.");

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            ApprovalStatus = (byte)ApprovalStatus.Pending,
            EmailConfirmed = true,
            CreatedOn = DateTime.UtcNow
        };

        var result = await _users.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return (false, string.Join(" ", result.Errors.Select(e => e.Description)));

        _db.EmployeeRegistrationRequests.Add(new EmployeeRegistrationRequest
        {
            UserId = user.Id,
            RequestedRoleId = role.Id,
            RequestedLocationId = request.RequestedLocationId,
            Status = (byte)RegistrationRequestStatus.Pending,
            CreatedOn = DateTime.UtcNow,
            CreatedBy = user.Id
        });
        await _db.SaveChangesAsync(cancellationToken);

        var baseUrl = _spa.BaseUrl.TrimEnd('/');
        var reviewUrl = $"{baseUrl}/admin/registrations";
        var employeeDisplay = $"{user.FirstName} {user.LastName}".Trim();
        if (string.IsNullOrEmpty(employeeDisplay)) employeeDisplay = user.Email ?? "";
        var content = _templates.RegistrationPendingManagement(
            user.Email ?? "",
            employeeDisplay,
            role.Name ?? "",
            loc.Name,
            reviewUrl);

        var approvalInbox = _emailOptions.ApprovalRequestRecipient?.Trim();
        if (!string.IsNullOrEmpty(approvalInbox))
            await _email.SendAsync(approvalInbox, content.Subject, content.HtmlBody, cancellationToken);
        else
        {
            var managers = await _users.GetUsersInRoleAsync(AppRoles.Management);
            foreach (var m in managers)
            {
                if (!string.IsNullOrEmpty(m.Email))
                    await _email.SendAsync(m.Email, content.Subject, content.HtmlBody, cancellationToken);
            }
        }

        if (!string.IsNullOrEmpty(user.Email))
        {
            var submitted = _templates.RegistrationSubmitted(
                employeeDisplay,
                role.Name ?? "",
                loc.Name);
            await _email.SendAsync(user.Email, submitted.Subject, submitted.HtmlBody, cancellationToken);
        }

        return (true, null);
    }

    public async Task RequestPasswordResetAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email?.Trim();
        if (string.IsNullOrEmpty(email)) return;

        var user = await _users.FindByEmailAsync(email);
        if (user == null) return;

        var rawToken = await _users.GeneratePasswordResetTokenAsync(user);
        var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));

        var baseUrl = _spa.BaseUrl.TrimEnd('/');
        var resetUrl =
            $"{baseUrl}/reset-password?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(code)}";

        var who = $"{user.FirstName} {user.LastName}".Trim();
        if (string.IsNullOrEmpty(who)) who = null;
        var content = _templates.ForgotPassword(resetUrl, who);

        if (!string.IsNullOrEmpty(user.Email))
            await _email.SendAsync(user.Email, content.Subject, content.HtmlBody, cancellationToken);
    }

    public async Task<(bool Success, string? Error)> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email?.Trim();
        if (string.IsNullOrEmpty(email)) return (false, "Email is required.");
        if (string.IsNullOrWhiteSpace(request.Token)) return (false, "Reset token is required.");
        if (string.IsNullOrWhiteSpace(request.NewPassword)) return (false, "Password is required.");

        var user = await _users.FindByEmailAsync(email);
        if (user == null) return (false, "Unable to reset password. Request a new reset link.");

        string rawToken;
        try
        {
            rawToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token));
        }
        catch
        {
            return (false, "Invalid or expired reset link.");
        }

        var result = await _users.ResetPasswordAsync(user, rawToken, request.NewPassword);
        if (!result.Succeeded)
            return (false, string.Join(" ", result.Errors.Select(e => e.Description)));

        if (!string.IsNullOrEmpty(user.Email))
        {
            var who = $"{user.FirstName} {user.LastName}".Trim();
            if (string.IsNullOrEmpty(who)) who = null;
            var confirm = _templates.PasswordChangedConfirmation(who);
            await _email.SendAsync(user.Email, confirm.Subject, confirm.HtmlBody, cancellationToken);
        }

        return (true, null);
    }

    public async Task<AuthResponse?> BuildAuthResponseAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _users.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null) return null;
        return await BuildAuthResponseAsync(user, cancellationToken);
    }

    private async Task<AuthResponse?> BuildAuthResponseAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var roles = await _users.GetRolesAsync(user);
        var status = (ApprovalStatus)user.ApprovalStatus;
        var display = $"{user.FirstName} {user.LastName}".Trim();
        if (string.IsNullOrEmpty(display)) display = user.Email ?? user.UserName ?? "";
        var token = _tokens.CreateAccessToken(user.Id, user.Email ?? "", roles.ToList(), status, display);
        return new AuthResponse
        {
            Token = token,
            Email = user.Email ?? "",
            UserId = user.Id,
            Roles = roles.ToList(),
            ApprovalStatus = status,
            DisplayName = display
        };
    }
}
