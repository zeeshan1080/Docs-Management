using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using DocumentManagement.Application.Abstractions;
using DocumentManagement.Infrastructure.Options;

namespace DocumentManagement.Infrastructure.Services;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _opts;

    public SmtpEmailSender(IOptions<EmailOptions> options)
    {
        _opts = options.Value;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var fromEmail = _opts.DefaultFromEmail?.Trim();
        if (string.IsNullOrWhiteSpace(fromEmail))
            throw new InvalidOperationException("Email:DefaultFromEmail is required.");

        var smtp = _opts.Smtp;
        if (string.IsNullOrWhiteSpace(smtp.Host))
            throw new InvalidOperationException("Email:Smtp:Host is required.");
        if (string.IsNullOrWhiteSpace(smtp.Username) || string.IsNullOrWhiteSpace(smtp.Password))
            throw new InvalidOperationException("Email:Smtp:Username and Email:Smtp:Password are required.");

        using var message = new MailMessage
        {
            From = string.IsNullOrWhiteSpace(_opts.DefaultFromName)
                ? new MailAddress(fromEmail)
                : new MailAddress(fromEmail, _opts.DefaultFromName.Trim()),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(new MailAddress(to));

        using var client = new SmtpClient(smtp.Host, smtp.Port)
        {
            EnableSsl = smtp.UseStartTls,
            Credentials = new NetworkCredential(smtp.Username, smtp.Password)
        };

        cancellationToken.ThrowIfCancellationRequested();
        await client.SendMailAsync(message).ConfigureAwait(false);
    }
}
