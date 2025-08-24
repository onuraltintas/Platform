using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpeedReading.ContentService.Data;

namespace SpeedReading.ContentService.Controllers;

[ApiController]
[Route("api/v1/texts")] 
public class PublicTextsController : ControllerBase
{
    private readonly ContentDbContext _db;
    public PublicTextsController(ContentDbContext db) { _db = db; }

    public record PublicTextDto(Guid TextId, string Title, string DifficultyLevel, Guid? LevelId, int? WordCount, DateTime? UpdatedAt, string? Content);

    [HttpGet]
    public async Task<ActionResult<object>> List([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null, [FromQuery] Guid? levelId = null, [FromQuery] string? difficultyLevel = null)
    {
        var query = _db.Texts.AsNoTracking().Where(t => !t.IsDeleted);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(t => t.Title.Contains(search));
        if (levelId.HasValue) query = query.Where(t => t.LevelId == levelId.Value);
        if (!string.IsNullOrWhiteSpace(difficultyLevel)) query = query.Where(t => t.DifficultyLevel == difficultyLevel);
        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(t => t.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new PublicTextDto(t.TextId, t.Title, t.DifficultyLevel, t.LevelId, t.WordCount, t.UpdatedAt, t.Content))
            .ToListAsync();
        return Ok(new { items, total, page, pageSize });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PublicTextDto>> Get(Guid id)
    {
        var t = await _db.Texts.AsNoTracking().FirstOrDefaultAsync(x => x.TextId == id && !x.IsDeleted);
        if (t == null) return NotFound();
        return Ok(new PublicTextDto(t.TextId, t.Title, t.DifficultyLevel, t.LevelId, t.WordCount, t.UpdatedAt, t.Content));
    }
}

