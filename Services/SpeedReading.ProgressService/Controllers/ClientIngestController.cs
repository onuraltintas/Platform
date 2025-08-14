using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpeedReading.ProgressService.Data;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace SpeedReading.ProgressService.Controllers;

[ApiController]
[Authorize]
[Route("api/v1")] 
public class ClientIngestController : ControllerBase
{
    private readonly ProgressDbContext _db;
    public ClientIngestController(ProgressDbContext db) { _db = db; }
    public record SessionStartRequest(Guid SessionId, Guid? TextId, DateTime StartedAtUtc);
    public record SessionEndRequest(Guid SessionId, DateTime EndedAtUtc, int DurationSeconds, int? WPM, decimal? ComprehensionScore, string? EyeTrackingMetricsJson);
    public record ExerciseCompleteRequest(Guid AttemptId, Guid ExerciseId, DateTime CompletedAtUtc, int DurationSeconds, int? WPM, decimal? Score, string? EyeTrackingMetricsJson, string? Feedback);
    public record QuestionResponseRequest(Guid ResponseId, Guid AttemptId, Guid QuestionId, string GivenAnswer, bool? IsCorrect, int? ResponseTimeMs);

    [HttpPost("session/start")]
    public async Task<IActionResult> StartSession([FromBody] SessionStartRequest request)
    {
        var userId = GetUserId();
        var exists = await _db.Sessions.AnyAsync(x => x.SessionId == request.SessionId);
        if (!exists)
        {
            _db.Sessions.Add(new UserReadingSession
            {
                SessionId = request.SessionId,
                UserId = userId,
                TextId = request.TextId,
                SessionStartDate = request.StartedAtUtc,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }
        return Accepted(new { request.SessionId });
    }

    [HttpPost("session/end")]
    public async Task<IActionResult> EndSession([FromBody] SessionEndRequest request)
    {
        var s = await _db.Sessions.FirstOrDefaultAsync(x => x.SessionId == request.SessionId);
        if (s == null) return NotFound();
        s.SessionEndDate = request.EndedAtUtc; s.DurationSeconds = request.DurationSeconds; s.WPM = request.WPM; s.ComprehensionScore = request.ComprehensionScore; s.EyeTrackingMetricsJson = request.EyeTrackingMetricsJson;
        await _db.SaveChangesAsync();
        return Accepted(new { request.SessionId });
    }

    [HttpPost("exercise/complete")]
    public async Task<IActionResult> CompleteExercise([FromBody] ExerciseCompleteRequest request)
    {
        var userId = GetUserId();
        var exists = await _db.Attempts.AnyAsync(a => a.AttemptId == request.AttemptId);
        if (!exists)
        {
            _db.Attempts.Add(new UserExerciseAttempt
            {
                AttemptId = request.AttemptId,
                UserId = userId,
                ExerciseId = request.ExerciseId,
                AttemptDate = request.CompletedAtUtc,
                DurationSeconds = request.DurationSeconds,
                WPM = request.WPM,
                Score = request.Score,
                EyeTrackingMetricsJson = request.EyeTrackingMetricsJson,
                Feedback = request.Feedback,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }
        return Accepted(new { request.AttemptId });
    }

    [HttpPost("response")] 
    public async Task<IActionResult> RecordResponse([FromBody] QuestionResponseRequest request)
    {
        var exists = await _db.Responses.AnyAsync(r => r.ResponseId == request.ResponseId);
        if (!exists)
        {
            _db.Responses.Add(new QuestionResponse
            {
                ResponseId = request.ResponseId,
                AttemptId = request.AttemptId,
                QuestionId = request.QuestionId,
                GivenAnswer = request.GivenAnswer,
                IsCorrect = request.IsCorrect,
                ResponseTimeMs = request.ResponseTimeMs
            });
            await _db.SaveChangesAsync();
        }
        return Accepted(new { request.ResponseId });
    }
    private Guid GetUserId()
    {
        var sub = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? HttpContext.User?.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var gid) ? gid : Guid.Empty;
    }
}

