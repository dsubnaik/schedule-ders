using System.Threading.Channels;
using Microsoft.AspNetCore.Identity.UI.Services;
using schedule_ders.Services.Interfaces;

namespace schedule_ders.Services;

public class BackgroundEmailQueue : BackgroundService, IBackgroundEmailQueue
{
    private readonly Channel<QueuedEmail> _queue = Channel.CreateUnbounded<QueuedEmail>();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BackgroundEmailQueue> _logger;

    public BackgroundEmailQueue(IServiceScopeFactory scopeFactory, ILogger<BackgroundEmailQueue> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public void QueueEmail(string recipient, string subject, string htmlMessage)
    {
        if (!_queue.Writer.TryWrite(new QueuedEmail(recipient, subject, htmlMessage)))
        {
            _logger.LogWarning("Email could not be queued. To='{Recipient}', Subject='{Subject}'", recipient, subject);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var email in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
                await emailSender.SendEmailAsync(email.Recipient, email.Subject, email.HtmlMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Background email failed. To='{Recipient}', Subject='{Subject}'",
                    email.Recipient,
                    email.Subject);
            }
        }
    }

    private sealed record QueuedEmail(string Recipient, string Subject, string HtmlMessage);
}
