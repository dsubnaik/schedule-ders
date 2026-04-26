namespace schedule_ders.Services.Interfaces;

public interface IBackgroundEmailQueue
{
    void QueueEmail(string recipient, string subject, string htmlMessage);
}
