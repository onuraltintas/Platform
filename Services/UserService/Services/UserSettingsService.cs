using AutoMapper;
using AutoMapper.QueryableExtensions;
using EgitimPlatform.Services.UserService.Data;
using EgitimPlatform.Services.UserService.Models.DTOs;
using EgitimPlatform.Services.UserService.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Services.UserService.Services;

public class UserSettingsService : IUserSettingsService
{
    private readonly UserDbContext _dbContext;
    private readonly IMapper _mapper;

    public UserSettingsService(UserDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<UserSettingsDto?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserSettings
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ProjectTo<UserSettingsDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<UserSettingsDto> UpsertAsync(Guid userId, UpdateUserSettingsRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.UserSettings.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        if (entity == null)
        {
            entity = new UserSettings
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.UserSettings.Add(entity);
        }

        if (request.Theme != null) entity.Theme = request.Theme;
        if (request.Language != null) entity.Language = request.Language;
        if (request.TimeZone != null) entity.TimeZone = request.TimeZone;
        if (request.EmailNotifications.HasValue) entity.EmailNotifications = request.EmailNotifications.Value;
        if (request.PushNotifications.HasValue) entity.PushNotifications = request.PushNotifications.Value;
        if (request.SMSNotifications.HasValue) entity.SMSNotifications = request.SMSNotifications.Value;
        if (request.Preferences != null) entity.Preferences = request.Preferences;

        entity.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return _mapper.Map<UserSettingsDto>(entity);
    }
}

