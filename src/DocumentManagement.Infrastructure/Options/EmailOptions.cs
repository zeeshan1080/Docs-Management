namespace DocumentManagement.Infrastructure.Options;

public class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>Logging, Smtp, or GmailSmtp.</summary>
    public string Provider { get; set; } = "Logging";

    /// <summary>From address for transactional mail.</summary>
    public string DefaultFromEmail { get; set; } = "";

    public string? DefaultFromName { get; set; }

    /// <summary>Absolute URL to the logo image in HTML emails. If empty, uses {SpaPublic:BaseUrl}/assets/logo.png.</summary>
    public string? PublicLogoUrl { get; set; }

    /// <summary>
    /// If set, new registration approval-queue emails go only here (e.g. portal inbox).
    /// If empty, every user in the Management role receives the notification (previous behavior).
    /// </summary>
    public string? ApprovalRequestRecipient { get; set; }

    /// <summary>SMTP settings. For Gmail use smtp.gmail.com:587 with STARTTLS and an app password.</summary>
    public SmtpEmailOptions Smtp { get; set; } = new();
}

public class SmtpEmailOptions
{
    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public bool UseStartTls { get; set; } = true;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}

