using Microsoft.Extensions.Logging;
using DocumentManagement.Application.Abstractions;

namespace DocumentManagement.Infrastructure.Services;

public class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger) => _logger = logger;

    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var preview = htmlBody.Length > 400 ? htmlBody.Substring(0, 400) + "…" : htmlBody;
        _logger.LogInformation(
            "Email (stub) To={To} Subject={Subject} BodyPreview={Preview}",
            to,
            subject,
            preview);
        return Task.CompletedTask;
    }
}
