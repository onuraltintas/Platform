using SpeedReading.Domain.Entities;

namespace SpeedReading.Application.Interfaces;

public interface IUserProfileRepository
{
    Task<UserReadingProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserReadingProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<UserReadingProfile>> GetActiveProfilesAsync(CancellationToken cancellationToken = default);
    Task<UserReadingProfile> AddAsync(UserReadingProfile profile, CancellationToken cancellationToken = default);
    Task<UserReadingProfile> UpdateAsync(UserReadingProfile profile, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int> GetTotalUserCountAsync(CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface ICityRepository
{
    Task<List<City>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<City>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<City?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<City?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<List<City>> GetByRegionAsync(string region, CancellationToken cancellationToken = default);
}

public interface IDistrictRepository
{
    Task<List<District>> GetByCityIdAsync(int cityId, CancellationToken cancellationToken = default);
    Task<List<District>> SearchByCityAsync(int cityId, string searchTerm, CancellationToken cancellationToken = default);
    Task<District?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<District?> GetByCityAndNameAsync(int cityId, string name, CancellationToken cancellationToken = default);
    Task<bool> ValidateCityDistrictAsync(string cityName, string districtName, CancellationToken cancellationToken = default);
}

public interface IEducationLevelRepository
{
    Task<List<EducationLevel>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<EducationLevel>> GetByCategoryAsync(int category, CancellationToken cancellationToken = default);
    Task<EducationLevel?> GetByGradeLevelAsync(int gradeLevel, CancellationToken cancellationToken = default);
    Task<EducationLevel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}