using AutoMapper;
using AutoMapper.QueryableExtensions;
using EgitimPlatform.Services.UserService.Data;
using EgitimPlatform.Services.UserService.Models.DTOs;
using EgitimPlatform.Services.UserService.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Services.UserService.Services;

public class UserProfileService : IUserProfileService
{
    private readonly UserDbContext _dbContext;
    private readonly IMapper _mapper;

    public UserProfileService(UserDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<UserProfileDto?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserProfiles
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ProjectTo<UserProfileDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<UserProfileDto> CreateAsync(CreateUserProfileRequest request, CancellationToken cancellationToken = default)
    {
        var exists = await _dbContext.UserProfiles.AnyAsync(x => x.UserId == request.UserId, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException("Profile already exists for this user");
        }

        var entity = _mapper.Map<UserProfile>(request);
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        _dbContext.UserProfiles.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return _mapper.Map<UserProfileDto>(entity);
    }

    public async Task<UserProfileDto?> UpdateAsync(Guid userId, UpdateUserProfileRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.UserProfiles.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        if (entity == null)
        {
            return null;
        }

        _mapper.Map(request, entity);
        entity.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return _mapper.Map<UserProfileDto>(entity);
    }
}

