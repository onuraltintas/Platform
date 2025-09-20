using MediatR;
using SpeedReading.Application.DTOs;

namespace SpeedReading.Application.Queries;

public class GetUserProfileQuery : IRequest<UserProfileDto?>
{
    public Guid UserId { get; set; }
    
    public GetUserProfileQuery(Guid userId)
    {
        UserId = userId;
    }
}

public class GetProfileCompletionStatusQuery : IRequest<ProfileCompletionStatusDto>
{
    public Guid UserId { get; set; }
    
    public GetProfileCompletionStatusQuery(Guid userId)
    {
        UserId = userId;
    }
}

public class GetCitiesQuery : IRequest<List<CityDto>>
{
    public string? SearchTerm { get; set; }
    public bool IncludeDistricts { get; set; } = false;
}

public class GetDistrictsByCityQuery : IRequest<List<DistrictDto>>
{
    public int CityId { get; set; }
    public string? SearchTerm { get; set; }
    
    public GetDistrictsByCityQuery(int cityId)
    {
        CityId = cityId;
    }
}

public class GetEducationLevelsQuery : IRequest<List<EducationLevelDto>>
{
    public int? Category { get; set; } // EducationCategory enum değeri
}

public class ValidateCityDistrictQuery : IRequest<bool>
{
    public string CityName { get; set; }
    public string DistrictName { get; set; }
    
    public ValidateCityDistrictQuery(string cityName, string districtName)
    {
        CityName = cityName;
        DistrictName = districtName;
    }
}