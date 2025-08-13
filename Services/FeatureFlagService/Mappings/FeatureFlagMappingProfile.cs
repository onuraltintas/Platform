using AutoMapper;
using EgitimPlatform.Services.FeatureFlagService.Models.DTOs;
using EgitimPlatform.Services.FeatureFlagService.Models.Entities;

namespace EgitimPlatform.Services.FeatureFlagService.Mappings;

public class FeatureFlagMappingProfile : Profile
{
    public FeatureFlagMappingProfile()
    {
        // FeatureFlag mappings
        CreateMap<CreateFeatureFlagRequest, FeatureFlag>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Assignments, opt => opt.Ignore())
            .ForMember(dest => dest.Events, opt => opt.Ignore());

        CreateMap<UpdateFeatureFlagRequest, FeatureFlag>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Key, opt => opt.Ignore())
            .ForMember(dest => dest.Type, opt => opt.Ignore())
            .ForMember(dest => dest.Environment, opt => opt.Ignore())
            .ForMember(dest => dest.ApplicationId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Assignments, opt => opt.Ignore())
            .ForMember(dest => dest.Events, opt => opt.Ignore());

        CreateMap<FeatureFlag, FeatureFlagResponse>();

        // FeatureFlagAssignment mappings
        CreateMap<FeatureFlagAssignment, FeatureFlagAssignmentResponse>();

        // FeatureFlagEvent mappings
        CreateMap<LogEventRequest, FeatureFlagEvent>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.OccurredAt, opt => opt.Ignore())
            .ForMember(dest => dest.FeatureFlag, opt => opt.Ignore());

        CreateMap<FeatureFlagEvent, FeatureFlagEventResponse>();


    }
}