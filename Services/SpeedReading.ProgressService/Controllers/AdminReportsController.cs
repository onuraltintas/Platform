using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpeedReading.ProgressService.Data;
using Microsoft.AspNetCore.Authorization;
using EgitimPlatform.Shared.Security.Constants;

namespace SpeedReading.ProgressService.Controllers;

[ApiController]
[Authorize(Policy = Permissions.SpeedReading.ProgressReadAll)]
[Route("api/v1/admin")] 
public class AdminReportsController : ControllerBase
{
    private readonly ProgressDbContext _db;
    public AdminReportsController(ProgressDbContext db) { _db = db; }
    [HttpGet("sessions")]
    public async Task<IActionResult> ListSessions([FromQuery] string? userId = null, [FromQuery] string? textId = null, [FromQuery] DateTime? dateFrom = null, [FromQuery] DateTime? dateTo = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var q = _db.Sessions.AsNoTracking().AsQueryable();
        if (Guid.TryParse(userId, out var uid)) q = q.Where(x => x.UserId == uid);
        if (Guid.TryParse(textId, out var tid)) q = q.Where(x => x.TextId == tid);
        if (dateFrom.HasValue) q = q.Where(x => x.SessionStartDate >= dateFrom.Value);
        if (dateTo.HasValue) q = q.Where(x => (x.SessionEndDate ?? x.SessionStartDate) <= dateTo.Value);
        var total = await q.CountAsync();
        var items = await q.OrderByDescending(x => x.SessionStartDate)
          .Skip((page - 1) * pageSize)
          .Take(pageSize)
          .ToListAsync();
        return Ok(new { items, total });
    }

    [HttpGet("attempts")]
    public async Task<IActionResult> ListAttempts([FromQuery] string? userId = null, [FromQuery] string? exerciseId = null, [FromQuery] DateTime? dateFrom = null, [FromQuery] DateTime? dateTo = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var q = _db.Attempts.AsNoTracking().AsQueryable();
        if (Guid.TryParse(userId, out var uid)) q = q.Where(x => x.UserId == uid);
        if (Guid.TryParse(exerciseId, out var eid)) q = q.Where(x => x.ExerciseId == eid);
        if (dateFrom.HasValue) q = q.Where(x => x.AttemptDate >= dateFrom.Value);
        if (dateTo.HasValue) q = q.Where(x => x.AttemptDate <= dateTo.Value);
        var total = await q.CountAsync();
        var items = await q.OrderByDescending(x => x.AttemptDate)
          .Skip((page - 1) * pageSize)
          .Take(pageSize)
          .ToListAsync();
        return Ok(new { items, total });
    }

    [HttpGet("responses")]
    public async Task<IActionResult> ListResponses([FromQuery] string? attemptId = null, [FromQuery] string? textId = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var q = _db.Responses.AsNoTracking().AsQueryable();
        if (Guid.TryParse(attemptId, out var aid)) q = q.Where(x => x.AttemptId == aid);
        if (Guid.TryParse(textId, out var tid)) { /* bağ yok; ileride join */ }
        var total = await q.CountAsync();
        var items = await q.OrderByDescending(x => x.CreatedAt)
          .Skip((page - 1) * pageSize)
          .Take(pageSize)
          .ToListAsync();
        return Ok(new { items, total });
    }

    [HttpGet("export/sessions")]
    public async Task<IActionResult> ExportSessions([FromQuery] DateTime? dateFrom = null, [FromQuery] DateTime? dateTo = null)
    {
        var q = _db.Sessions.AsNoTracking().AsQueryable();
        if (dateFrom.HasValue) q = q.Where(x => x.SessionStartDate >= dateFrom.Value);
        if (dateTo.HasValue) q = q.Where(x => (x.SessionEndDate ?? x.SessionStartDate) <= dateTo.Value);
        var rows = await q.OrderBy(x => x.SessionStartDate).ToListAsync();
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("id,userId,textId,startedAt,endedAt,duration,wpm,comprehension");
        foreach (var s in rows) sb.AppendLine($"{s.SessionId},{s.UserId},{s.TextId},{s.SessionStartDate:o},{s.SessionEndDate:o},{s.DurationSeconds},{s.WPM},{s.ComprehensionScore}");
        return File(System.Text.Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"sessions_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HttpGet("export/attempts")]
    public async Task<IActionResult> ExportAttempts([FromQuery] DateTime? dateFrom = null, [FromQuery] DateTime? dateTo = null)
    {
        var q = _db.Attempts.AsNoTracking().AsQueryable();
        if (dateFrom.HasValue) q = q.Where(x => x.AttemptDate >= dateFrom.Value);
        if (dateTo.HasValue) q = q.Where(x => x.AttemptDate <= dateTo.Value);
        var rows = await q.OrderBy(x => x.AttemptDate).ToListAsync();
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("id,userId,exerciseId,attemptDate,duration,score,wpm");
        foreach (var a in rows) sb.AppendLine($"{a.AttemptId},{a.UserId},{a.ExerciseId},{a.AttemptDate:o},{a.DurationSeconds},{a.Score},{a.WPM}");
        return File(System.Text.Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"attempts_{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}

