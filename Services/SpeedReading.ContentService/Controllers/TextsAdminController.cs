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
[Route("api/v1/admin/texts")]
public class TextsAdminController : ControllerBase
{
    private readonly ContentDbContext _db;
    private readonly IAuditService _audit;

    public TextsAdminController(ContentDbContext db, IAuditService audit)
    {
        _db = db; _audit = audit;
    }

    public record TextDto(Guid TextId, string Title, string DifficultyLevel, Guid? LevelId, string? LevelName, int? WordCount, DateTime? UpdatedAt);
    public record UpsertTextRequest(string Title, string Content, string DifficultyLevel, Guid? LevelId, string? TagsJson);

    [HttpGet]
    public async Task<ActionResult<object>> List([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null, [FromQuery] Guid? levelId = null, [FromQuery] string? difficultyLevel = null)
    {
        var query = _db.Texts.AsNoTracking().Where(t => !t.IsDeleted);
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(t => t.Title.Contains(search));
        }
        if (levelId.HasValue)
        {
            query = query.Where(t => t.LevelId == levelId.Value);
        }
        if (!string.IsNullOrWhiteSpace(difficultyLevel))
        {
            query = query.Where(t => t.DifficultyLevel == difficultyLevel);
        }
        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(t => t.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TextDto(
                t.TextId,
                t.Title,
                t.DifficultyLevel,
                t.LevelId,
                _db.ReadingLevels.Where(l => l.LevelId == t.LevelId).Select(l => l.LevelName).FirstOrDefault(),
                t.WordCount,
                t.UpdatedAt))
            .ToListAsync();
        return Ok(new { items, total });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TextDto>> Get(Guid id)
    {
        var t = await _db.Texts.AsNoTracking().FirstOrDefaultAsync(x => x.TextId == id && !x.IsDeleted);
        if (t == null) return NotFound();
        var levelName = await _db.ReadingLevels.AsNoTracking().Where(l => l.LevelId == t.LevelId).Select(l => l.LevelName).FirstOrDefaultAsync();
        return Ok(new TextDto(t.TextId, t.Title, t.DifficultyLevel, t.LevelId, levelName, t.WordCount, t.UpdatedAt));
    }

    [HttpPost]
    public async Task<ActionResult<TextDto>> Create([FromBody] UpsertTextRequest request)
    {
        var e = new TextEntity
        {
            TextId = Guid.NewGuid(),
            Title = request.Title,
            Content = request.Content,
            DifficultyLevel = request.DifficultyLevel,
            LevelId = request.LevelId,
            WordCount = string.IsNullOrWhiteSpace(request.Content) ? 0 : request.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Texts.Add(e);
        await _db.SaveChangesAsync();
        await _audit.LogAuditAsync(new AuditEntry{ EntityType = nameof(TextEntity), EntityId = e.TextId.ToString(), Action = AuditAction.Insert, NewValuesObject = new(){ ["Title"] = e.Title } });
        var levelName = await _db.ReadingLevels.AsNoTracking().Where(l => l.LevelId == e.LevelId).Select(l => l.LevelName).FirstOrDefaultAsync();
        return CreatedAtAction(nameof(Get), new { id = e.TextId }, new TextDto(e.TextId, e.Title, e.DifficultyLevel, e.LevelId, levelName, e.WordCount, e.UpdatedAt));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<TextDto>> Update(Guid id, [FromBody] UpsertTextRequest request)
    {
        var e = await _db.Texts.FirstOrDefaultAsync(x => x.TextId == id && !x.IsDeleted);
        if (e == null) return NotFound();
        e.Title = request.Title;
        e.Content = request.Content;
        e.DifficultyLevel = request.DifficultyLevel;
        e.LevelId = request.LevelId;
        e.WordCount = string.IsNullOrWhiteSpace(request.Content) ? 0 : request.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        e.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.LogAuditAsync(new AuditEntry{ EntityType = nameof(TextEntity), EntityId = e.TextId.ToString(), Action = AuditAction.Update, NewValuesObject = new(){ ["Title"] = e.Title } });
        var levelName2 = await _db.ReadingLevels.AsNoTracking().Where(l => l.LevelId == e.LevelId).Select(l => l.LevelName).FirstOrDefaultAsync();
        return Ok(new TextDto(e.TextId, e.Title, e.DifficultyLevel, e.LevelId, levelName2, e.WordCount, e.UpdatedAt));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var e = await _db.Texts.FirstOrDefaultAsync(x => x.TextId == id && !x.IsDeleted);
        if (e == null) return NotFound();
        e.IsDeleted = true;
        e.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.LogAuditAsync(new AuditEntry{ EntityType = nameof(TextEntity), EntityId = id.ToString(), Action = AuditAction.SoftDelete });
        return NoContent();
    }
}

