namespace EgitimPlatform.Shared.Configuration.ConfigurationOptions;

public class EmailOptions
{
    public const string SectionName = "Email";
    
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public int Timeout { get; set; } = 30000;
}