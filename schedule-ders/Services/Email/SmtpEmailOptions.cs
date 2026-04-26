namespace schedule_ders.Services.Email;

public class SmtpEmailOptions
{
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "Schedule DERS";
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
}
