using System.Net;
using DocumentManagement.Application.Abstractions;
using DocumentManagement.Application.Email;
using DocumentManagement.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace DocumentManagement.Infrastructure.Services;

public class EmailTemplates : IEmailTemplates
{
    private const string BrandName = "josephanthony";

    private readonly EmailOptions _email;
    private readonly SpaPublicOptions _spa;

    public EmailTemplates(IOptions<EmailOptions> email, IOptions<SpaPublicOptions> spa)
    {
        _email = email.Value;
        _spa = spa.Value;
    }

    private static string E(string? s) => WebUtility.HtmlEncode(s ?? "");

    private string ResolveLogoUrl()
    {
        var configured = _email.PublicLogoUrl?.Trim();
        if (!string.IsNullOrEmpty(configured))
            return configured;
        return $"{_spa.BaseUrl.TrimEnd('/')}/assets/logo.png";
    }

    public EmailContent RegistrationPendingManagement(
        string employeeEmail,
        string employeeDisplayName,
        string requestedRoleName,
        string requestedLocationName,
        string reviewRegistrationsUrl)
        => new(
            $"{BrandName}: New employee registration",
            Wrap(
                "New registration pending approval",
                $"""
                <p><strong>{E(employeeDisplayName)}</strong> ({E(employeeEmail)}) has registered.</p>
                <ul style="margin:0 0 16px 0;padding-left:20px;">
                  <li>Role: <strong>{E(requestedRoleName)}</strong></li>
                  <li>Location: <strong>{E(requestedLocationName)}</strong></li>
                </ul>
                {CtaButton(E(reviewRegistrationsUrl), "Review in Approvals")}
                """));

    public EmailContent RegistrationSubmitted(
        string employeeDisplayName,
        string requestedRoleName,
        string requestedLocationName)
        => new(
            $"{BrandName}: We received your registration",
            Wrap(
                "Registration received",
                $"""
                <p>Hi {E(employeeDisplayName)},</p>
                <p>Thanks for registering. Your request is <strong>pending approval</strong> from Management.</p>
                <ul style="margin:0 0 16px 0;padding-left:20px;">
                  <li>Role: <strong>{E(requestedRoleName)}</strong></li>
                  <li>Location: <strong>{E(requestedLocationName)}</strong></li>
                </ul>
                <p>You will receive another email when your account has been approved or if more information is needed.</p>
                """));

    public EmailContent RegistrationApproved(
        string employeeDisplayName,
        string assignedRoleName,
        string assignedLocationName,
        string signInUrl)
        => new(
            $"{BrandName}: Your account is approved",
            Wrap(
                "You’re approved",
                $"""
                <p>Hi {E(employeeDisplayName)},</p>
                <p>Your account has been <strong>approved</strong>. You can sign in with the role and location assigned by Management.</p>
                <ul style="margin:0 0 16px 0;padding-left:20px;">
                  <li>Role: <strong>{E(assignedRoleName)}</strong></li>
                  <li>Location: <strong>{E(assignedLocationName)}</strong></li>
                </ul>
                {CtaButton(E(signInUrl), "Sign in")}
                """));

    public EmailContent RegistrationRejected(string employeeDisplayName, string? notes)
        => new(
            $"{BrandName}: Registration update",
            Wrap(
                "Registration not approved",
                $"""
                <p>Hi {E(employeeDisplayName)},</p>
                <p>Your registration request was <strong>not approved</strong> at this time.</p>
                {(string.IsNullOrWhiteSpace(notes) ? "" : $"<p><strong>Notes from Management:</strong><br/>{E(notes)}</p>")}
                <p>If you believe this is a mistake, contact your administrator.</p>
                """));

    public EmailContent ForgotPassword(string resetUrl, string? recipientDisplayName)
        => new(
            $"{BrandName}: Reset your password",
            Wrap(
                "Password reset",
                $"""
                <p>Hi{(string.IsNullOrWhiteSpace(recipientDisplayName) ? "" : $" {E(recipientDisplayName)}")},</p>
                <p>We received a request to reset the password for your account.</p>
                {CtaButton(E(resetUrl), "Choose a new password")}
                <p style="margin-top:24px;color:#64748b;font-size:14px;">If you did not request this, you can ignore this email. The link will stop working after a short time.</p>
                """));

    public EmailContent PasswordChangedConfirmation(string? recipientDisplayName)
        => new(
            $"{BrandName}: Your password was changed",
            Wrap(
                "Password updated",
                $"""
                <p>Hi{(string.IsNullOrWhiteSpace(recipientDisplayName) ? "" : $" {E(recipientDisplayName)}")},</p>
                <p>Your password was <strong>changed successfully</strong>.</p>
                <p style="color:#64748b;font-size:14px;">If you did not make this change, contact your administrator immediately.</p>
                """));

    public EmailContent FolderAccessGranted(
        string folderName,
        string actionByDisplayName,
        string accessDescription,
        string documentsUrl)
        => new(
            $"{BrandName}: Folder shared with you",
            Wrap(
                "New folder access",
                $"""
                <p>You have been granted access to a shared folder.</p>
                <ul style="margin:0 0 16px 0;padding-left:20px;">
                  <li>Folder: <strong>{E(folderName)}</strong></li>
                  <li>Access: <strong>{E(accessDescription)}</strong></li>
                  <li>Changed by: {E(actionByDisplayName)}</li>
                </ul>
                {CtaButton(E(documentsUrl), "Open documents")}
                """));

    public EmailContent FolderAccessRevoked(
        string folderName,
        string actionByDisplayName,
        string accessDescription,
        string documentsUrl)
        => new(
            $"{BrandName}: Folder access removed",
            Wrap(
                "Access removed",
                $"""
                <p>Your access to a shared folder has been <strong>removed or updated</strong> by Management.</p>
                <ul style="margin:0 0 16px 0;padding-left:20px;">
                  <li>Folder: <strong>{E(folderName)}</strong></li>
                  <li>Previous access: <strong>{E(accessDescription)}</strong></li>
                  <li>Changed by: {E(actionByDisplayName)}</li>
                </ul>
                {CtaButton(E(documentsUrl), "Go to documents", muted: true)}
                """));

    public EmailContent DocumentAccessGranted(
        string fileName,
        string folderName,
        string actionByDisplayName,
        string accessDescription,
        string documentsUrl)
        => new(
            $"{BrandName}: Document shared with you",
            Wrap(
                "New document access",
                $"""
                <p>You have been granted access to a shared file.</p>
                <ul style="margin:0 0 16px 0;padding-left:20px;">
                  <li>File: <strong>{E(fileName)}</strong></li>
                  <li>Folder: <strong>{E(folderName)}</strong></li>
                  <li>Access: <strong>{E(accessDescription)}</strong></li>
                  <li>Changed by: {E(actionByDisplayName)}</li>
                </ul>
                {CtaButton(E(documentsUrl), "Open documents")}
                """));

    public EmailContent DocumentAccessRevoked(
        string fileName,
        string folderName,
        string actionByDisplayName,
        string accessDescription,
        string documentsUrl)
        => new(
            $"{BrandName}: Document access removed",
            Wrap(
                "Document access removed",
                $"""
                <p>Your access to a shared file has been <strong>removed or updated</strong> by Management.</p>
                <ul style="margin:0 0 16px 0;padding-left:20px;">
                  <li>File: <strong>{E(fileName)}</strong></li>
                  <li>Folder: <strong>{E(folderName)}</strong></li>
                  <li>Previous access: <strong>{E(accessDescription)}</strong></li>
                  <li>Changed by: {E(actionByDisplayName)}</li>
                </ul>
                {CtaButton(E(documentsUrl), "Go to documents", muted: true)}
                """));

    public EmailContent AccountCreatedByAdmin(
        string employeeDisplayName,
        string assignedRoleName,
        string assignedLocationName,
        string signInUrl,
        string createdByDisplayName)
        => new(
            $"{BrandName}: Your account is ready",
            Wrap(
                "Account created",
                $"""
                <p>Hi {E(employeeDisplayName)},</p>
                <p><strong>{E(createdByDisplayName)}</strong> created an account for you in {E(BrandName)}.</p>
                <ul style="margin:0 0 16px 0;padding-left:20px;">
                  <li>Role: <strong>{E(assignedRoleName)}</strong></li>
                  <li>Location: <strong>{E(assignedLocationName)}</strong></li>
                </ul>
                <p>Sign in with your email and the password your administrator gave you. For security, change your password after first sign-in if prompted.</p>
                {CtaButton(E(signInUrl), "Sign in")}
                """));

    /// <summary>Primary CTA — matches Angular <c>btn-primary</c> / Tailwind <c>portal-400</c>.</summary>
    private const string ButtonStyle =
        "display:inline-block;padding:12px 24px;background:#82c3ec;color:#ffffff !important;text-decoration:none;border-radius:8px;font-weight:600;font-family:system-ui,-apple-system,Segoe UI,Roboto,sans-serif;box-shadow:0 2px 10px rgba(61,155,217,0.28);";

    /// <summary>Secondary CTA — matches <c>btn-secondary</c> (white fill, portal border).</summary>
    private const string ButtonStyleMuted =
        "display:inline-block;padding:12px 24px;background:#ffffff;color:#2d82bc !important;text-decoration:none;border-radius:8px;font-weight:600;font-family:system-ui,-apple-system,Segoe UI,Roboto,sans-serif;border:2px solid #82c3ec;";

    /// <summary>Centered call-to-action row (table layout for email clients).</summary>
    private static string CtaButton(string encodedHref, string label, bool muted = false)
    {
        var style = muted ? ButtonStyleMuted : ButtonStyle;
        return $"""
<table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="margin:24px 0 0 0;border-collapse:collapse;">
  <tr>
    <td align="center" style="padding:0;">
      <a href="{encodedHref}" style="{style}">{E(label)}</a>
    </td>
  </tr>
</table>
""";
    }

    private string Wrap(string heading, string innerHtml)
    {
        var logoUrl = ResolveLogoUrl();
        var logoAlt = "Joseph Anthony retreat spa · salon — Salon Document Portal";
        return $"""
<!DOCTYPE html>
<html>
<head><meta charset="utf-8"/><meta name="viewport" content="width=device-width,initial-scale=1"/></head>
<body style="margin:0;padding:0;background:#eef6ff;font-family:system-ui,-apple-system,Segoe UI,Roboto,Helvetica Neue,sans-serif;">
  <table role="presentation" width="100%" cellspacing="0" cellpadding="0" bgcolor="#eef6ff" style="background:linear-gradient(165deg,#f4f9ff 0%,#eef6ff 42%,#fafdff 78%,#f0f7ff 100%);background-color:#eef6ff;padding:28px 14px;">
    <tr>
      <td align="center" bgcolor="#eef6ff">
        <table role="presentation" width="100%" cellspacing="0" cellpadding="0" bgcolor="#ffffff" style="max-width:560px;background:#ffffff;border-radius:14px;border:1px solid #e2f2fb;overflow:hidden;box-shadow:0 4px 24px -4px rgba(15,23,42,0.06),0 12px 36px -10px rgba(130,195,236,0.18);">
          <tr>
            <td bgcolor="#ffffff" style="padding:24px 28px 20px;text-align:center;background:linear-gradient(180deg,#ffffff 0%,#f0f8ff 100%);background-color:#ffffff;border-bottom:1px solid #e2f2fb;">
              <img src="{E(logoUrl)}" alt="{E(logoAlt)}" width="280" style="max-width:100%;width:280px;height:auto;display:block;margin:0 auto;border:0;outline:none;text-decoration:none;" />
            </td>
          </tr>
          <tr>
            <td bgcolor="#ffffff" style="padding:28px;color:#0f172a;font-size:16px;line-height:1.55;background:#ffffff;">
              <h1 style="margin:0 0 16px 0;font-size:20px;font-weight:700;color:#25587a;letter-spacing:-0.02em;">{E(heading)}</h1>
              {innerHtml}
            </td>
          </tr>
          <tr>
            <td bgcolor="#f8fafc" style="padding:16px 28px 24px;color:#64748b;font-size:12px;line-height:1.45;border-top:1px solid #e2f2fb;background:#f8fafc;">This message was sent automatically. Please do not reply.</td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>
""";
    }
}
