using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpeedReading.ContentService.Data;
using Microsoft.AspNetCore.Authorization;
using EgitimPlatform.Shared.Security.Constants;
using EgitimPlatform.Shared.Auditing.Services;
using EgitimPlatform.Shared.Auditing.Models;

namespace SpeedReading.ContentService.Controllers;

[ApiController]
[Authorize(Policy = Permissions.SpeedReading.ContentManage)]
[Route("api/v1/admin/levels")]
public class LevelsAdminController : ControllerBase
{
    private readonly ContentDbContext _db;
    private readonly IAuditService _audit;
    public LevelsAdminController(ContentDbContext db, IAuditService audit) { _db = db; _audit = audit; }

    public record LevelDto(Guid LevelId, string LevelName, int? MinAge, int? MaxAge, int? MinWPM, int? MaxWPM, decimal? TargetComprehension);
    public record UpsertLevelRequest(string LevelName, int? MinAge, int? MaxAge, int? MinWPM, int? MaxWPM, decimal? TargetComprehension);

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var items = await _db.ReadingLevels.AsNoTracking()
            .OrderBy(x => x.MinAge).ThenBy(x => x.LevelName)
            .Select(x => new LevelDto(x.LevelId, x.LevelName, x.MinAge, x.MaxAge, x.MinWPM, x.MaxWPM, x.TargetComprehension))
            .ToListAsync();
        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<LevelDto>> Get(Guid id)
    {
        var x = await _db.ReadingLevels.AsNoTracking().FirstOrDefaultAsync(l => l.LevelId == id);
        if (x == null) return NotFound();
        return Ok(new LevelDto(x.LevelId, x.LevelName, x.MinAge, x.MaxAge, x.MinWPM, x.MaxWPM, x.TargetComprehension));
    }

    [HttpPost]
    public async Task<ActionResult<LevelDto>> Create([FromBody] UpsertLevelRequest r)
    {
        var x = new ReadingLevelEntity { LevelId = Guid.NewGuid(), LevelName = r.LevelName, MinAge = r.MinAge, MaxAge = r.MaxAge, MinWPM = r.MinWPM, MaxWPM = r.MaxWPM, TargetComprehension = r.TargetComprehension };
        _db.ReadingLevels.Add(x);
        await _db.SaveChangesAsync();
        await _audit.LogAuditAsync(new AuditEntry { EntityType = nameof(ReadingLevelEntity), EntityId = x.LevelId.ToString(), Action = AuditAction.Insert, NewValuesObject = new() { ["LevelName"] = x.LevelName } });
        return CreatedAtAction(nameof(Get), new { id = x.LevelId }, new LevelDto(x.LevelId, x.LevelName, x.MinAge, x.MaxAge, x.MinWPM, x.MaxWPM, x.TargetComprehension));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<LevelDto>> Update(Guid id, [FromBody] UpsertLevelRequest r)
    {
        var x = await _db.ReadingLevels.FirstOrDefaultAsync(l => l.LevelId == id);
        if (x == null) return NotFound();
        x.LevelName = r.LevelName; x.MinAge = r.MinAge; x.MaxAge = r.MaxAge; x.MinWPM = r.MinWPM; x.MaxWPM = r.MaxWPM; x.TargetComprehension = r.TargetComprehension;
        await _db.SaveChangesAsync();
        await _audit.LogAuditAsync(new AuditEntry { EntityType = nameof(ReadingLevelEntity), EntityId = x.LevelId.ToString(), Action = AuditAction.Update, NewValuesObject = new() { ["LevelName"] = x.LevelName } });
        return Ok(new LevelDto(x.LevelId, x.LevelName, x.MinAge, x.MaxAge, x.MinWPM, x.MaxWPM, x.TargetComprehension));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var x = await _db.ReadingLevels.FirstOrDefaultAsync(l => l.LevelId == id);
        if (x == null) return NotFound();
        _db.ReadingLevels.Remove(x);
        await _db.SaveChangesAsync();
        await _audit.LogAuditAsync(new AuditEntry { EntityType = nameof(ReadingLevelEntity), EntityId = id.ToString(), Action = AuditAction.Delete });
        return NoContent();
    }
}

