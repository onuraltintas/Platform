using MediatR;
using SpeedReading.Application.DTOs;
using SpeedReading.Application.Interfaces;
using SpeedReading.Application.Queries;

namespace SpeedReading.Application.Handlers;

public class GetCitiesHandler : IRequestHandler<GetCitiesQuery, List<CityDto>>
{
    private readonly ICityRepository _cityRepository;

    public GetCitiesHandler(ICityRepository cityRepository)
    {
        _cityRepository = cityRepository;
    }

    public async Task<List<CityDto>> Handle(GetCitiesQuery request, CancellationToken cancellationToken)
    {
        var cities = string.IsNullOrEmpty(request.SearchTerm)
            ? await _cityRepository.GetAllAsync(cancellationToken)
            : await _cityRepository.SearchAsync(request.SearchTerm, cancellationToken);

        return cities.Select(city => new CityDto
        {
            Id = city.Id,
            Name = city.Name,
            PlateCode = city.PlateCode,
            Region = city.Region,
            Districts = request.IncludeDistricts
                ? city.Districts.Select(d => new DistrictDto
                {
                    Id = d.Id,
                    CityId = d.CityId,
                    Name = d.Name,
                    CityName = city.Name
                }).ToList()
                : new List<DistrictDto>()
        }).ToList();
    }
}

public class GetDistrictsByCityHandler : IRequestHandler<GetDistrictsByCityQuery, List<DistrictDto>>
{
    private readonly IDistrictRepository _districtRepository;
    private readonly ICityRepository _cityRepository;

    public GetDistrictsByCityHandler(IDistrictRepository districtRepository, ICityRepository cityRepository)
    {
        _districtRepository = districtRepository;
        _cityRepository = cityRepository;
    }

    public async Task<List<DistrictDto>> Handle(GetDistrictsByCityQuery request, CancellationToken cancellationToken)
    {
        var city = await _cityRepository.GetByIdAsync(request.CityId, cancellationToken);
        if (city == null)
        {
            return new List<DistrictDto>();
        }

        var districts = string.IsNullOrEmpty(request.SearchTerm)
            ? await _districtRepository.GetByCityIdAsync(request.CityId, cancellationToken)
            : await _districtRepository.SearchByCityAsync(request.CityId, request.SearchTerm, cancellationToken);

        return districts.Select(district => new DistrictDto
        {
            Id = district.Id,
            CityId = district.CityId,
            Name = district.Name,
            CityName = city.Name
        }).ToList();
    }
}

public class GetEducationLevelsHandler : IRequestHandler<GetEducationLevelsQuery, List<EducationLevelDto>>
{
    private readonly IEducationLevelRepository _educationLevelRepository;

    public GetEducationLevelsHandler(IEducationLevelRepository educationLevelRepository)
    {
        _educationLevelRepository = educationLevelRepository;
    }

    public async Task<List<EducationLevelDto>> Handle(GetEducationLevelsQuery request, CancellationToken cancellationToken)
    {
        var educationLevels = request.Category.HasValue
            ? await _educationLevelRepository.GetByCategoryAsync(request.Category.Value, cancellationToken)
            : await _educationLevelRepository.GetAllAsync(cancellationToken);

        return educationLevels.Select(level => new EducationLevelDto
        {
            Id = level.Id,
            Name = level.Name,
            Category = (int)level.Category,
            CategoryDisplay = GetCategoryDisplay(level.Category),
            GradeNumber = level.GradeNumber,
            AgeRange = level.AgeRange
        }).ToList();
    }

    private static string GetCategoryDisplay(Domain.Enums.EducationCategory category) => category switch
    {
        Domain.Enums.EducationCategory.Elementary => "İlkokul",
        Domain.Enums.EducationCategory.MiddleSchool => "Ortaokul",
        Domain.Enums.EducationCategory.HighSchool => "Lise",
        Domain.Enums.EducationCategory.University => "Üniversite",
        Domain.Enums.EducationCategory.Graduate => "Lisansüstü",
        Domain.Enums.EducationCategory.Adult => "Yetişkin",
        _ => "Diğer"
    };
}

public class ValidateCityDistrictHandler : IRequestHandler<ValidateCityDistrictQuery, bool>
{
    private readonly IDistrictRepository _districtRepository;

    public ValidateCityDistrictHandler(IDistrictRepository districtRepository)
    {
        _districtRepository = districtRepository;
    }

    public async Task<bool> Handle(ValidateCityDistrictQuery request, CancellationToken cancellationToken)
    {
        return await _districtRepository.ValidateCityDistrictAsync(request.CityName, request.DistrictName, cancellationToken);
    }
}