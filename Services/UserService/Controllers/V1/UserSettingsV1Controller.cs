using EgitimPlatform.Services.UserService.Models.DTOs;
using EgitimPlatform.Services.UserService.Services;
using EgitimPlatform.Shared.Errors.Common;
using Microsoft.AspNetCore.Mvc;

namespace EgitimPlatform.Services.UserService.Controllers.V1;

[ApiController]
[Route("api/v1/settings")]
public class UserSettingsV1Controller : ControllerBase
{
    private readonly IUserSettingsService _settingsService;

    public UserSettingsV1Controller(IUserSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<ApiResponse<UserSettingsDto>>> GetByUserId([FromRoute] Guid userId, CancellationToken ct)
    {
        try
        {
            var result = await _settingsService.GetByUserIdAsync(userId, ct);
            if (result is not null)
            {
                return Ok(ApiResponse<UserSettingsDto>.Ok(result));
            }

            // Fallback: create defaults on first access
            var created = await _settingsService.UpsertAsync(userId, new UpdateUserSettingsRequest { }, ct);
            return Ok(ApiResponse<UserSettingsDto>.Ok(created));
        }
        catch (Exception)
        {
            // As a last resort, try to upsert defaults (handles transient or first-time DB object issues)
            try
            {
                var created = await _settingsService.UpsertAsync(userId, new UpdateUserSettingsRequest { }, ct);
                return Ok(ApiResponse<UserSettingsDto>.Ok(created));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<UserSettingsDto>.Fail(ErrorCodes.INTERNAL_SERVER_ERROR, ex.Message));
            }
        }
    }

    [HttpPut("{userId:guid}")]
    public async Task<ActionResult<ApiResponse<UserSettingsDto>>> Upsert([FromRoute] Guid userId, [FromBody] UpdateUserSettingsRequest request, CancellationToken ct)
    {
        var upserted = await _settingsService.UpsertAsync(userId, request, ct);
        return Ok(ApiResponse<UserSettingsDto>.Ok(upserted));
    }
}