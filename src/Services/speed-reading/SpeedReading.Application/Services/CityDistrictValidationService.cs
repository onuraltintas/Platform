using SpeedReading.Application.Interfaces;

namespace SpeedReading.Application.Services;

public class CityDistrictValidationService : ICityDistrictValidationService
{
    private readonly IDistrictRepository _districtRepository;
    private readonly ICityRepository _cityRepository;

    public CityDistrictValidationService(IDistrictRepository districtRepository, ICityRepository cityRepository)
    {
        _districtRepository = districtRepository;
        _cityRepository = cityRepository;
    }

    public async Task<bool> ValidateAsync(string cityName, string districtName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cityName) || string.IsNullOrWhiteSpace(districtName))
        {
            return false;
        }

        return await _districtRepository.ValidateCityDistrictAsync(cityName, districtName, cancellationToken);
    }

    public async Task<List<string>> GetCitySuggestionsAsync(string partialName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(partialName) || partialName.Length < 2)
        {
            return new List<string>();
        }

        var cities = await _cityRepository.SearchAsync(partialName, cancellationToken);
        
        return cities
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => c.Name)
            .Take(10)
            .ToList();
    }

    public async Task<List<string>> GetDistrictSuggestionsAsync(string cityName, string partialDistrictName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cityName) || 
            string.IsNullOrWhiteSpace(partialDistrictName) || 
            partialDistrictName.Length < 2)
        {
            return new List<string>();
        }

        var city = await _cityRepository.GetByNameAsync(cityName, cancellationToken);
        if (city == null)
        {
            return new List<string>();
        }

        var districts = await _districtRepository.SearchByCityAsync(city.Id, partialDistrictName, cancellationToken);
        
        return districts
            .Where(d => d.IsActive)
            .OrderBy(d => d.Name)
            .Select(d => d.Name)
            .Take(10)
            .ToList();
    }
}