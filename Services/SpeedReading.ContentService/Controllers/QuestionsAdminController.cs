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
[Route("api/v1/admin/questions")]
public class QuestionsAdminController : ControllerBase
{
    private readonly ContentDbContext _db;
    private readonly IAuditService _audit;
    public QuestionsAdminController(ContentDbContext db, IAuditService audit) { _db = db; _audit = audit; }

    public record QuestionDto(Guid QuestionId, Guid TextId, string QuestionText, string? QuestionType, string? CorrectAnswer, string? OptionsJson, Guid? LevelId, string? LevelName);
    public record UpsertQuestionRequest(Guid TextId, string QuestionText, string? QuestionType, string? CorrectAnswer, string? OptionsJson, Guid? LevelId);

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? textId = null, [FromQuery] Guid? levelId = null, [FromQuery] string? questionType = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var q = _db.Questions.AsNoTracking();
        if (textId.HasValue) q = q.Where(x => x.TextId == textId.Value);
        if (levelId.HasValue) q = q.Where(x => x.LevelId == levelId.Value);
        if (!string.IsNullOrWhiteSpace(questionType)) q = q.Where(x => x.QuestionType == questionType);
        var total = await q.CountAsync();
        var items = await q.OrderBy(x => x.QuestionText)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new QuestionDto(x.QuestionId, x.TextId, x.QuestionText, x.QuestionType, x.CorrectAnswer, x.OptionsJson, x.LevelId, _db.ReadingLevels.Where(l => l.LevelId == x.LevelId).Select(l => l.LevelName).FirstOrDefault())).ToListAsync();
        return Ok(new { items, total });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<QuestionDto>> Get(Guid id)
    {
        var x = await _db.Questions.AsNoTracking().FirstOrDefaultAsync(q => q.QuestionId == id);
        if (x == null) return NotFound();
        var levelName = await _db.ReadingLevels.AsNoTracking().Where(l => l.LevelId == x.LevelId).Select(l => l.LevelName).FirstOrDefaultAsync();
        return Ok(new QuestionDto(x.QuestionId, x.TextId, x.QuestionText, x.QuestionType, x.CorrectAnswer, x.OptionsJson, x.LevelId, levelName));
    }

    [HttpPost]
    public async Task<ActionResult<QuestionDto>> Create([FromBody] UpsertQuestionRequest r)
    {
        var x = new ComprehensionQuestionEntity { QuestionId = Guid.NewGuid(), TextId = r.TextId, QuestionText = r.QuestionText, QuestionType = r.QuestionType, CorrectAnswer = r.CorrectAnswer, OptionsJson = r.OptionsJson, LevelId = r.LevelId };
        _db.Questions.Add(x);
        await _db.SaveChangesAsync();
        await _audit.LogAuditAsync(new AuditEntry { EntityType = nameof(ComprehensionQuestionEntity), EntityId = x.QuestionId.ToString(), Action = AuditAction.Insert, NewValuesObject = new() { ["QuestionText"] = x.QuestionText } });
        var levelName2 = await _db.ReadingLevels.AsNoTracking().Where(l => l.LevelId == x.LevelId).Select(l => l.LevelName).FirstOrDefaultAsync();
        return CreatedAtAction(nameof(Get), new { id = x.QuestionId }, new QuestionDto(x.QuestionId, x.TextId, x.QuestionText, x.QuestionType, x.CorrectAnswer, x.OptionsJson, x.LevelId, levelName2));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<QuestionDto>> Update(Guid id, [FromBody] UpsertQuestionRequest r)
    {
        var x = await _db.Questions.FirstOrDefaultAsync(q => q.QuestionId == id);
        if (x == null) return NotFound();
        x.TextId = r.TextId; x.QuestionText = r.QuestionText; x.QuestionType = r.QuestionType; x.CorrectAnswer = r.CorrectAnswer; x.OptionsJson = r.OptionsJson; x.LevelId = r.LevelId;
        await _db.SaveChangesAsync();
        await _audit.LogAuditAsync(new AuditEntry { EntityType = nameof(ComprehensionQuestionEntity), EntityId = x.QuestionId.ToString(), Action = AuditAction.Update, NewValuesObject = new() { ["QuestionText"] = x.QuestionText } });
        var levelName3 = await _db.ReadingLevels.AsNoTracking().Where(l => l.LevelId == x.LevelId).Select(l => l.LevelName).FirstOrDefaultAsync();
        return Ok(new QuestionDto(x.QuestionId, x.TextId, x.QuestionText, x.QuestionType, x.CorrectAnswer, x.OptionsJson, x.LevelId, levelName3));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var x = await _db.Questions.FirstOrDefaultAsync(q => q.QuestionId == id);
        if (x == null) return NotFound();
        _db.Questions.Remove(x);
        await _db.SaveChangesAsync();
        await _audit.LogAuditAsync(new AuditEntry { EntityType = nameof(ComprehensionQuestionEntity), EntityId = id.ToString(), Action = AuditAction.Delete });
        return NoContent();
    }
}

