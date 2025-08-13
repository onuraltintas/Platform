using AutoMapper;
using EgitimPlatform.Services.IdentityService.Models.DTOs;
using EgitimPlatform.Services.IdentityService.Models.Entities;
using Microsoft.Extensions.Logging;

namespace EgitimPlatform.Services.IdentityService.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        Console.WriteLine("MappingProfile constructor called!");
        
        // User mappings
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
            .ForMember(dest => dest.Roles, opt => opt.MapFrom(src => 
                src.UserRoles
                    .Where(ur => ur.IsActive && (ur.ExpiresAt == null || ur.ExpiresAt > DateTime.UtcNow) && ur.Role != null)
                    .Select(ur => ur.Role!.Name)
                    .ToList()))
            .ForMember(dest => dest.Categories, opt => opt.MapFrom(src => 
                src.UserCategories
                    .Where(uc => uc.IsActive && (uc.ExpiresAt == null || uc.ExpiresAt > DateTime.UtcNow) && uc.Category != null)
                    .Select(uc => uc.Category!.Name)
                    .ToList()))
            .ForMember(dest => dest.Permissions, opt => opt.Ignore()); // Will be set in service
        
        // Role mappings
        CreateMap<Role, RoleDto>()
            .ForMember(dest => dest.Permissions, opt => opt.MapFrom(src => 
                src.RolePermissions.Where(rp => rp.IsActive)
                    .Select(rp => rp.Permission.Name).ToList()));
        
        // Category mappings
        CreateMap<Category, CategoryDto>();
        
        // Permission mappings
        CreateMap<Permission, PermissionDto>();
        
        // UserRole mappings
        CreateMap<UserRole, UserRoleDto>()
            .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role.Name))
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.UserName));
        
        // UserCategory mappings
        CreateMap<UserCategory, UserCategoryDto>()
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.Name))
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.UserName));
        
        // RefreshToken mappings
        CreateMap<RefreshToken, RefreshTokenDto>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.UserName))
            .ForMember(dest => dest.IsExpired, opt => opt.MapFrom(src => src.ExpiresAt < DateTime.UtcNow))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => !src.IsRevoked && src.ExpiresAt >= DateTime.UtcNow));
    }
}