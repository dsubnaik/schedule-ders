using System.Net;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using schedule_ders.Models;
using schedule_ders.Services.Email;
using schedule_ders.Services.Interfaces;

namespace schedule_ders.Services;

public class RequestNotificationEmailService : IRequestNotificationEmailService
{
    private readonly IEmailSender _emailSender;
    private readonly NotificationEmailOptions _options;
    private readonly ILogger<RequestNotificationEmailService> _logger;

    public RequestNotificationEmailService(
        IEmailSender emailSender,
        IOptions<NotificationEmailOptions> options,
        ILogger<RequestNotificationEmailService> logger)
    {
        _emailSender = emailSender;
        _options = options.Value;
        _logger = logger;
    }

    public Task NotifyRequestSubmittedAsync(SIRequest request)
    {
        return SendAdminNotificationAsync(
            request,
            "New SI request submitted",
            "A professor submitted a new SI request.");
    }

    public Task NotifyRequestEditedAsync(SIRequest request)
    {
        return SendAdminNotificationAsync(
            request,
            "SI request edited",
            "A professor edited an existing SI request.");
    }

    public async Task NotifyRequestStatusUpdatedAsync(SIRequest request)
    {
        var recipient = string.IsNullOrWhiteSpace(_options.ProfessorStatusRecipientOverride)
            ? request.ProfessorEmail
            : _options.ProfessorStatusRecipientOverride;

        if (string.IsNullOrWhiteSpace(recipient))
        {
            _logger.LogWarning(
                "Status notification not sent because no recipient is configured for SI request {RequestId}.",
                request.SIRequestID);
            return;
        }

        var originalRecipientNote = string.Equals(recipient, request.ProfessorEmail, StringComparison.OrdinalIgnoreCase)
            ? string.Empty
            : $"<p><strong>Demo original recipient:</strong> {WebUtility.HtmlEncode(request.ProfessorEmail)}</p>";

        await _emailSender.SendEmailAsync(
            recipient,
            $"SI request status updated: {BuildCourseDisplay(request)}",
            $"""
            <p>Your SI request status was updated.</p>
            {originalRecipientNote}
            {BuildRequestDetailsHtml(request)}
            """);
    }

    private async Task SendAdminNotificationAsync(SIRequest request, string subject, string intro)
    {
        if (string.IsNullOrWhiteSpace(_options.AdminRecipient))
        {
            _logger.LogWarning(
                "Admin notification not sent because Notifications:AdminRecipient is not configured for SI request {RequestId}.",
                request.SIRequestID);
            return;
        }

        await _emailSender.SendEmailAsync(
            _options.AdminRecipient,
            $"{subject}: {BuildCourseDisplay(request)}",
            $"""
            <p>{WebUtility.HtmlEncode(intro)}</p>
            {BuildRequestDetailsHtml(request)}
            """);
    }

    private static string BuildRequestDetailsHtml(SIRequest request)
    {
        return $"""
        <dl>
            <dt>Request ID</dt>
            <dd>{request.SIRequestID}</dd>
            <dt>Course</dt>
            <dd>{WebUtility.HtmlEncode(BuildCourseDisplay(request))}</dd>
            <dt>Professor</dt>
            <dd>{WebUtility.HtmlEncode(request.ProfessorName)} ({WebUtility.HtmlEncode(request.ProfessorEmail)})</dd>
            <dt>Status</dt>
            <dd>{WebUtility.HtmlEncode(request.Status.ToString())}</dd>
            <dt>Potential SI Leader Status</dt>
            <dd>{WebUtility.HtmlEncode(request.PotentialSiLeaderStatus.ToString())}</dd>
            <dt>Notes</dt>
            <dd>{WebUtility.HtmlEncode(request.RequestNotes)}</dd>
            <dt>Admin Notes</dt>
            <dd>{WebUtility.HtmlEncode(request.AdminNotes)}</dd>
        </dl>
        """;
    }

    private static string BuildCourseDisplay(SIRequest request)
    {
        var name = request.RequestedCourseName.Trim();
        var title = request.RequestedCourseTitle.Trim();
        var section = request.RequestedCourseSection.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            return "Manual Course Entry";
        }

        var code = string.IsNullOrWhiteSpace(section) ? name : $"{name} ({section})";
        return string.IsNullOrWhiteSpace(title) ? code : $"{code} - {title}";
    }
}
