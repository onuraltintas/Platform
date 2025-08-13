namespace EgitimPlatform.Services.NotificationService.Models.DTOs;

public class CreateEmailFromTemplateDto
{
    public string TemplateName { get; set; } = null!;
    public string ToEmail { get; set; } = null!;
    public Dictionary<string, object> TemplateData { get; set; } = new();
}
