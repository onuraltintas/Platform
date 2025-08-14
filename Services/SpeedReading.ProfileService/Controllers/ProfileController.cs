using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpeedReading.ProfileService.Data;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace SpeedReading.ProfileService.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/profile")] 
public class ProfileController : ControllerBase
{
    private readonly ProfileDbContext _db;

    public ProfileController(ProfileDbContext db) { _db = db; }

    public record ProfileDto(Guid UserId, Guid? CurrentReadingLevelId, string? Goals, string? LearningStyle, string? AccessibilityNeeds, string? PreferencesJson);
    public record UpdateProfileRequest(string? Goals, string? LearningStyle, string? AccessibilityNeeds, string? PreferencesJson);

    [HttpGet("me")]
    public async Task<ActionResult<ProfileDto>> GetMe()
    {
        var userId = GetUserId();
        var p = await _db.Profiles.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId);
        if (p == null)
        {
            return Ok(new ProfileDto(userId, null, null, null, null, null));
        }
        return Ok(new ProfileDto(p.UserId, p.CurrentReadingLevelId, p.Goals, p.LearningStyle, p.AccessibilityNeeds, p.PreferencesJson));
    }

    [HttpPut("me")]
    public async Task<ActionResult<ProfileDto>> UpdateMe([FromBody] UpdateProfileRequest request)
    {
        var userId = GetUserId();
        var p = await _db.Profiles.FirstOrDefaultAsync(x => x.UserId == userId);
        if (p == null)
        {
            p = new ReadingProfile
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Goals = request.Goals,
                LearningStyle = request.LearningStyle,
                AccessibilityNeeds = request.AccessibilityNeeds,
                PreferencesJson = request.PreferencesJson
            };
            _db.Profiles.Add(p);
        }
        else
        {
            p.Goals = request.Goals;
            p.LearningStyle = request.LearningStyle;
            p.AccessibilityNeeds = request.AccessibilityNeeds;
            p.PreferencesJson = request.PreferencesJson;
            p.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();
        return Ok(new ProfileDto(p.UserId, p.CurrentReadingLevelId, p.Goals, p.LearningStyle, p.AccessibilityNeeds, p.PreferencesJson));
    }
    private Guid GetUserId()
    {
        var sub = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? HttpContext.User?.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var gid) ? gid : Guid.Empty;
    }
}

