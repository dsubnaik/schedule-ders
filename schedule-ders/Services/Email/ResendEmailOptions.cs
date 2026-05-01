namespace schedule_ders.Services.Email;

public class ResendEmailOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string FromAddress { get; set; } = "onboarding@resend.dev";
    public string FromName { get; set; } = "Schedule DERS";
}
