using EgitimPlatform.Services.UserService.Models.DTOs;
using EgitimPlatform.Services.UserService.Services;
using EgitimPlatform.Shared.Errors.Common;
using Microsoft.AspNetCore.Mvc;

namespace EgitimPlatform.Services.UserService.Controllers.V1;

[ApiController]
[Route("api/v1/profiles")]
public class UserProfilesV1Controller : ControllerBase
{
    private readonly IUserProfileService _profileService;

    public UserProfilesV1Controller(IUserProfileService profileService)
    {
        _profileService = profileService;
    }

    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> GetByUserId([FromRoute] Guid userId, CancellationToken ct)
    {
        var result = await _profileService.GetByUserIdAsync(userId, ct);
        return result is not null
            ? Ok(ApiResponse<UserProfileDto>.Ok(result))
            : NotFound(ApiResponse<UserProfileDto>.Fail(ErrorCodes.NOT_FOUND, "User profile not found"));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> Create([FromBody] CreateUserProfileRequest request, CancellationToken ct)
    {
        var created = await _profileService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetByUserId), new { userId = created.UserId }, ApiResponse<UserProfileDto>.Ok(created));
    }

    [HttpPut("{userId:guid}")]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> Update([FromRoute] Guid userId, [FromBody] UpdateUserProfileRequest request, CancellationToken ct)
    {
        var updated = await _profileService.UpdateAsync(userId, request, ct);
        return updated is not null
            ? Ok(ApiResponse<UserProfileDto>.Ok(updated))
            : NotFound(ApiResponse<UserProfileDto>.Fail(ErrorCodes.NOT_FOUND, "User profile not found"));
    }
}