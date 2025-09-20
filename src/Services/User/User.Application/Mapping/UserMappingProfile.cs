using AutoMapper;
using User.Core.DTOs;
using User.Core.DTOs.Requests;
using User.Core.Entities;

namespace User.Application.Mapping;

/// <summary>
/// AutoMapper profile for User entities and DTOs
/// </summary>
public class UserMappingProfile : Profile
{
    /// <summary>
    /// Constructor - Configure mappings
    /// </summary>
    public UserMappingProfile()
    {
        // UserProfile mappings
        CreateMap<UserProfile, UserProfileDto>()
            .ForMember(dest => dest.Addresses, opt => opt.MapFrom(src => src.Addresses))
            .ForMember(dest => dest.Preferences, opt => opt.MapFrom(src => src.Preferences));

        CreateMap<CreateUserProfileRequest, UserProfile>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Addresses, opt => opt.Ignore())
            .ForMember(dest => dest.Activities, opt => opt.Ignore())
            .ForMember(dest => dest.Documents, opt => opt.Ignore())
            .ForMember(dest => dest.GdprRequests, opt => opt.Ignore())
            .ForMember(dest => dest.EmailVerifications, opt => opt.Ignore())
            .ForMember(dest => dest.Preferences, opt => opt.Ignore());

        CreateMap<UpdateUserProfileRequest, UserProfile>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Addresses, opt => opt.Ignore())
            .ForMember(dest => dest.Activities, opt => opt.Ignore())
            .ForMember(dest => dest.Documents, opt => opt.Ignore())
            .ForMember(dest => dest.GdprRequests, opt => opt.Ignore())
            .ForMember(dest => dest.EmailVerifications, opt => opt.Ignore())
            .ForMember(dest => dest.Preferences, opt => opt.Ignore())
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

        // UserPreferences mappings
        CreateMap<UserPreferences, UserPreferencesDto>();
        CreateMap<UserPreferencesDto, UserPreferences>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.UserProfile, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore());

        // UserAddress mappings
        CreateMap<UserAddress, UserAddressDto>();
        CreateMap<UserAddressDto, UserAddress>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.UserProfile, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore());

        // UserActivity mappings
        CreateMap<UserActivity, UserActivityDto>();

        // UserDocument mappings  
        CreateMap<UserDocument, UserDocumentDto>();

        // GdprRequest mappings
        CreateMap<GdprRequest, GdprRequestDto>();

        // EmailVerification mappings
        CreateMap<EmailVerification, EmailVerificationDto>();
    }
}