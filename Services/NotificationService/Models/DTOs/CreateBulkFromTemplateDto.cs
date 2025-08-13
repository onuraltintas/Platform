using System;
using System.Collections.Generic;

namespace EgitimPlatform.Services.NotificationService.Models.DTOs
{
    public class CreateBulkFromTemplateDto
    {
        public string TemplateName { get; set; } = null!;
        public List<string>? UserIds { get; set; }
        public List<string>? EmailAddresses { get; set; }
        public Dictionary<string, string> TemplateData { get; set; } = new();
        public string? Subject { get; set; }
        public DateTime? ScheduledAt { get; set; }
    }
}
