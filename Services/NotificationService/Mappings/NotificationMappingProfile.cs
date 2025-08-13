using AutoMapper;
using EgitimPlatform.Services.NotificationService.Models.Entities;
using EgitimPlatform.Services.NotificationService.Models.DTOs;
using System.Text.Json;

namespace EgitimPlatform.Services.NotificationService.Mappings;

public class NotificationMappingProfile : Profile
{
    public NotificationMappingProfile()
    {
        // EmailNotification mappings
        CreateMap<EmailNotification, EmailNotificationDto>();
        
        CreateMap<CreateEmailNotificationDto, EmailNotification>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.TrackingId, opt => opt.MapFrom(src => Guid.NewGuid()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => "Pending"))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.RetryCount, opt => opt.MapFrom(src => 0))
            .ForMember(dest => dest.MaxRetries, opt => opt.MapFrom(src => 3));

        CreateMap<UpdateEmailNotificationDto, EmailNotification>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());

        // UserDevice mappings
        CreateMap<UserDevice, UserDeviceDto>();
        
        CreateMap<CreateUserDeviceDto, UserDevice>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true));

        CreateMap<UpdateUserDeviceDto, UserDevice>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));

        // BulkEmailNotification mappings
        CreateMap<BulkEmailNotificationDto, List<EmailNotification>>()
            .ConvertUsing((src, dest, context) =>
            {
                var notifications = new List<EmailNotification>();
                var trackingId = Guid.NewGuid();
                
                foreach (var emailDto in src.Notifications)
                {
                    var notification = context.Mapper.Map<EmailNotification>(emailDto);
                    notification.TrackingId = trackingId;
                    notifications.Add(notification);
                }
                
                return notifications;
            });

        // Template-based notifications
        CreateMap<CreateEmailFromTemplateDto, EmailNotification>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.TemplateName, opt => opt.MapFrom(src => src.TemplateName))
            .ForMember(dest => dest.TemplateData, opt => opt.MapFrom(src => src.TemplateData != null ? JsonSerializer.Serialize(src.TemplateData, (JsonSerializerOptions?)null) : null))
            .ForMember(dest => dest.ToEmail, opt => opt.MapFrom(src => src.ToEmail))
            .ForMember(dest => dest.TrackingId, opt => opt.MapFrom(src => Guid.NewGuid()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => "Pending"))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));
    }
}