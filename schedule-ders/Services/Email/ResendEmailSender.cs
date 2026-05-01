using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;

namespace schedule_ders.Services.Email;

public class ResendEmailSender : IEmailSender
{
    private static readonly Uri SendEmailUri = new("https://api.resend.com/emails");

    private readonly HttpClient _httpClient;
    private readonly ResendEmailOptions _options;
    private readonly ILogger<ResendEmailSender> _logger;

    public ResendEmailSender(
        HttpClient httpClient,
        IOptions<ResendEmailOptions> options,
        ILogger<ResendEmailSender> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey) || string.IsNullOrWhiteSpace(_options.FromAddress))
        {
            _logger.LogWarning(
                "Email not sent because Resend settings are incomplete. To='{To}', Subject='{Subject}'",
                email,
                subject);
            return;
        }

        var from = string.IsNullOrWhiteSpace(_options.FromName)
            ? _options.FromAddress
            : $"{_options.FromName} <{_options.FromAddress}>";

        using var request = new HttpRequestMessage(HttpMethod.Post, SendEmailUri)
        {
            Content = JsonContent.Create(new
            {
                from,
                to = new[] { email },
                subject,
                html = htmlMessage
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        _logger.LogInformation(
            "Sending email via Resend. From='{From}', To='{To}', Subject='{Subject}'",
            from,
            email,
            subject);

        using var response = await _httpClient.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Email accepted by Resend. To='{To}', Subject='{Subject}'", email, subject);
            return;
        }

        var responseBody = await response.Content.ReadAsStringAsync();
        _logger.LogError(
            "Resend email send failed. StatusCode={StatusCode}, To='{To}', Subject='{Subject}', Response='{Response}'",
            (int)response.StatusCode,
            email,
            subject,
            responseBody);

        response.EnsureSuccessStatusCode();
    }
}
