using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;

namespace schedule_ders.Services.Email;

public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpEmailOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;
    private const int SendTimeoutMilliseconds = 15_000;

    public SmtpEmailSender(IOptions<SmtpEmailOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        if (string.IsNullOrWhiteSpace(_options.Host) || string.IsNullOrWhiteSpace(_options.FromAddress))
        {
            _logger.LogWarning(
                "Email not sent because SMTP settings are incomplete. To='{To}', Subject='{Subject}'",
                email,
                subject);
            return;
        }

        _logger.LogInformation(
            "Sending email via SMTP. Host='{Host}', Port={Port}, EnableSsl={EnableSsl}, From='{From}', To='{To}', Subject='{Subject}'",
            _options.Host,
            _options.Port,
            _options.EnableSsl,
            _options.FromAddress,
            email,
            subject);

        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromName),
            Subject = subject,
            Body = htmlMessage,
            IsBodyHtml = true
        };
        message.To.Add(email);

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            Timeout = SendTimeoutMilliseconds
        };

        if (!string.IsNullOrWhiteSpace(_options.Username))
        {
            client.Credentials = new NetworkCredential(_options.Username, _options.Password);
        }

        try
        {
            await client.SendMailAsync(message);
            _logger.LogInformation("Email sent successfully. To='{To}', Subject='{Subject}'", email, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP email send failed. To='{To}', Subject='{Subject}'", email, subject);
            throw;
        }
    }
}
