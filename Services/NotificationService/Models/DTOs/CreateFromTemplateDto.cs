using System;
using System.Collections.Generic;

namespace EgitimPlatform.Services.NotificationService.Models.DTOs
{
    public class CreateFromTemplateDto
    {
        public string TemplateName { get; set; } = null!;
        public string? UserId { get; set; }
        public string ToEmail { get; set; } = null!;
        public Dictionary<string, string> TemplateData { get; set; } = new();
        public string? Subject { get; set; }
        public DateTime? ScheduledAt { get; set; }
    }
}
