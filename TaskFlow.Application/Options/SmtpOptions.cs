namespace TaskFlow.Application.Options;

public class SmtpOptions
{
    public string Host      { get; set; } = string.Empty;
    public int    Port      { get; set; } = 1025;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName  { get; set; } = "TaskFlow";
    public string? Username { get; set; }
    public string? Password { get; set; }
}
