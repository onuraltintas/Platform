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
[Route("api/v1/admin/exercises")]
public class ExercisesAdminController : ControllerBase
{
    private readonly ContentDbContext _db;
    private readonly IAuditService _audit;
    public ExercisesAdminController(ContentDbContext db, IAuditService audit) { _db = db; _audit = audit; }

    public record ExerciseDto(Guid ExerciseId, Guid ExerciseTypeId, string Title, string? Description, string DifficultyLevel, Guid? LevelId, string? LevelName, string? ContentJson, int? DurationMinutes);
    public record UpsertExerciseRequest(Guid ExerciseTypeId, string Title, string? Description, string DifficultyLevel, Guid? LevelId, string? ContentJson, int? DurationMinutes);

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null, [FromQuery] Guid? levelId = null, [FromQuery] string? difficultyLevel = null)
    {
        var q = _db.Exercises.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(e => e.Title.Contains(search));
        if (levelId.HasValue) q = q.Where(e => e.LevelId == levelId.Value);
        if (!string.IsNullOrWhiteSpace(difficultyLevel)) q = q.Where(e => e.DifficultyLevel == difficultyLevel);
        var total = await q.CountAsync();
        var items = await q.OrderBy(e => e.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new ExerciseDto(e.ExerciseId, e.ExerciseTypeId, e.Title, e.Description, e.DifficultyLevel, e.LevelId, _db.ReadingLevels.Where(l => l.LevelId == e.LevelId).Select(l => l.LevelName).FirstOrDefault(), e.ContentJson, e.DurationMinutes))
            .ToListAsync();
        return Ok(new { items, total });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ExerciseDto>> Get(Guid id)
    {
        var e = await _db.Exercises.AsNoTracking().FirstOrDefaultAsync(x => x.ExerciseId == id);
        if (e == null) return NotFound();
        var levelName = await _db.ReadingLevels.AsNoTracking().Where(l => l.LevelId == e.LevelId).Select(l => l.LevelName).FirstOrDefaultAsync();
        return Ok(new ExerciseDto(e.ExerciseId, e.ExerciseTypeId, e.Title, e.Description, e.DifficultyLevel, e.LevelId, levelName, e.ContentJson, e.DurationMinutes));
    }

    [HttpPost]
    public async Task<ActionResult<ExerciseDto>> Create([FromBody] UpsertExerciseRequest r)
    {
        var e = new ExerciseEntity { ExerciseId = Guid.NewGuid(), ExerciseTypeId = r.ExerciseTypeId, Title = r.Title, Description = r.Description, DifficultyLevel = r.DifficultyLevel, LevelId = r.LevelId, ContentJson = r.ContentJson, DurationMinutes = r.DurationMinutes };
        _db.Exercises.Add(e);
        await _db.SaveChangesAsync();
        await _audit.LogAuditAsync(new AuditEntry { EntityType = nameof(ExerciseEntity), EntityId = e.ExerciseId.ToString(), Action = AuditAction.Insert, NewValuesObject = new() { ["Title"] = e.Title } });
        var levelName2 = await _db.ReadingLevels.AsNoTracking().Where(l => l.LevelId == e.LevelId).Select(l => l.LevelName).FirstOrDefaultAsync();
        return CreatedAtAction(nameof(Get), new { id = e.ExerciseId }, new ExerciseDto(e.ExerciseId, e.ExerciseTypeId, e.Title, e.Description, e.DifficultyLevel, e.LevelId, levelName2, e.ContentJson, e.DurationMinutes));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ExerciseDto>> Update(Guid id, [FromBody] UpsertExerciseRequest r)
    {
        var e = await _db.Exercises.FirstOrDefaultAsync(x => x.ExerciseId == id);
        if (e == null) return NotFound();
        e.ExerciseTypeId = r.ExerciseTypeId; e.Title = r.Title; e.Description = r.Description; e.DifficultyLevel = r.DifficultyLevel; e.LevelId = r.LevelId; e.ContentJson = r.ContentJson; e.DurationMinutes = r.DurationMinutes;
        await _db.SaveChangesAsync();
        await _audit.LogAuditAsync(new AuditEntry { EntityType = nameof(ExerciseEntity), EntityId = e.ExerciseId.ToString(), Action = AuditAction.Update, NewValuesObject = new() { ["Title"] = e.Title } });
        var levelName3 = await _db.ReadingLevels.AsNoTracking().Where(l => l.LevelId == e.LevelId).Select(l => l.LevelName).FirstOrDefaultAsync();
        return Ok(new ExerciseDto(e.ExerciseId, e.ExerciseTypeId, e.Title, e.Description, e.DifficultyLevel, e.LevelId, levelName3, e.ContentJson, e.DurationMinutes));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var e = await _db.Exercises.FirstOrDefaultAsync(x => x.ExerciseId == id);
        if (e == null) return NotFound();
        _db.Exercises.Remove(e);
        await _db.SaveChangesAsync();
        await _audit.LogAuditAsync(new AuditEntry { EntityType = nameof(ExerciseEntity), EntityId = id.ToString(), Action = AuditAction.Delete });
        return NoContent();
    }
}

