using schedule_ders.Models;

namespace schedule_ders.Services.Interfaces;

public interface IRequestNotificationEmailService
{
    Task NotifyRequestSubmittedAsync(SIRequest request);
    Task NotifyRequestEditedAsync(SIRequest request);
    Task NotifyRequestStatusUpdatedAsync(SIRequest request);
}
