using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpeedReading.ProfileService.Data;
using Microsoft.AspNetCore.Authorization;
using EgitimPlatform.Shared.Security.Constants;

namespace SpeedReading.ProfileService.Controllers;

[ApiController]
[Authorize(Policy = Permissions.SpeedReading.ProfileManage)]
[Route("api/v1/admin/profiles")]
public class AdminProfilesController : ControllerBase
{
    private readonly ProfileDbContext _db;
    public AdminProfilesController(ProfileDbContext db) { _db = db; }

    public record ProfileDto(Guid Id, Guid UserId, Guid? CurrentReadingLevelId, string? Goals, string? LearningStyle, string? AccessibilityNeeds, string? PreferencesJson, DateTime CreatedAt, DateTime UpdatedAt);
    public record UpsertAdminProfileRequest(Guid? CurrentReadingLevelId, string? Goals, string? LearningStyle, string? AccessibilityNeeds, string? PreferencesJson);

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var q = _db.Profiles.AsNoTracking();
        var total = await q.CountAsync();
        var items = await q.OrderByDescending(x => x.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProfileDto(p.Id, p.UserId, p.CurrentReadingLevelId, p.Goals, p.LearningStyle, p.AccessibilityNeeds, p.PreferencesJson, p.CreatedAt, p.UpdatedAt))
            .ToListAsync();
        return Ok(new { items, total });
    }

    [HttpGet("{userId}")]
    public async Task<ActionResult<ProfileDto>> Get(Guid userId)
    {
        var p = await _db.Profiles.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId);
        if (p == null) return NotFound();
        return Ok(new ProfileDto(p.Id, p.UserId, p.CurrentReadingLevelId, p.Goals, p.LearningStyle, p.AccessibilityNeeds, p.PreferencesJson, p.CreatedAt, p.UpdatedAt));
    }

    [HttpPut("{userId}")]
    public async Task<ActionResult<ProfileDto>> Update(Guid userId, [FromBody] UpsertAdminProfileRequest r)
    {
        var p = await _db.Profiles.FirstOrDefaultAsync(x => x.UserId == userId);
        if (p == null) return NotFound();
        p.CurrentReadingLevelId = r.CurrentReadingLevelId;
        p.Goals = r.Goals; p.LearningStyle = r.LearningStyle; p.AccessibilityNeeds = r.AccessibilityNeeds; p.PreferencesJson = r.PreferencesJson; p.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new ProfileDto(p.Id, p.UserId, p.CurrentReadingLevelId, p.Goals, p.LearningStyle, p.AccessibilityNeeds, p.PreferencesJson, p.CreatedAt, p.UpdatedAt));
    }
}

