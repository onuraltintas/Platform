using AutoMapper;
using EgitimPlatform.Services.UserService.Models.DTOs;
using EgitimPlatform.Services.UserService.Models.Entities;

namespace EgitimPlatform.Services.UserService.Mappings;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<UserProfile, UserProfileDto>().ReverseMap();
        CreateMap<CreateUserProfileRequest, UserProfile>();
        CreateMap<UpdateUserProfileRequest, UserProfile>();

        CreateMap<UserSettings, UserSettingsDto>().ReverseMap();
        CreateMap<UpdateUserSettingsRequest, UserSettings>();
    }
}

