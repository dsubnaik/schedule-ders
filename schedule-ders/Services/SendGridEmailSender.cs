using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using schedule_ders.Options;

namespace schedule_ders.Services;

public sealed class SendGridEmailSender : IEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly SendGridOptions _options;
    private readonly ILogger<SendGridEmailSender> _logger;

    public SendGridEmailSender(
        HttpClient httpClient,
        IOptions<SendGridOptions> options,
        ILogger<SendGridEmailSender> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey) || string.IsNullOrWhiteSpace(_options.FromEmail))
        {
            throw new InvalidOperationException(
                "SendGrid is not configured. Set SendGrid:ApiKey and SendGrid:FromEmail.");
        }

        var payload = new
        {
            personalizations = new[]
            {
                new
                {
                    to = new[] { new { email } },
                    subject
                }
            },
            from = new
            {
                email = _options.FromEmail,
                name = string.IsNullOrWhiteSpace(_options.FromName) ? "schedule-ders" : _options.FromName
            },
            content = new[]
            {
                new
                {
                    type = "text/html",
                    value = htmlMessage
                }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.sendgrid.com/v3/mail/send");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogError(
                "SendGrid request failed with status {StatusCode}. Response: {ResponseBody}",
                (int)response.StatusCode,
                responseBody);

            throw new InvalidOperationException("Failed to send email via SendGrid.");
        }
    }
}
