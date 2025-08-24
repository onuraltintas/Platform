using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpeedReading.ProgressService.Data;

namespace SpeedReading.ProgressService.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/users")] 
public class UserReportsController : ControllerBase
{
    private readonly ProgressDbContext _db;
    public UserReportsController(ProgressDbContext db) { _db = db; }

    public record ProgressSummaryDto(
        int TotalSessions,
        int AverageWPM,
        int TotalReadingTime,
        int ImprovementRate,
        int BestWPM,
        IEnumerable<BackendSessionDto> RecentSessions
    );

    public record BackendSessionDto(
        Guid SessionId,
        Guid UserId,
        Guid? TextId,
        DateTime SessionStartDate,
        DateTime? SessionEndDate,
        int? DurationSeconds,
        int? WPM,
        decimal? ComprehensionScore,
        string? EyeTrackingMetricsJson,
        DateTime CreatedAt
    );

    public record StatsResponse(
        int TotalSessions,
        int TotalWords,
        int AverageWPM,
        int ImprovementRate,
        IEnumerable<object> SessionsByDate,
        IEnumerable<object> WpmTrend,
        IEnumerable<object> ComprehensionTrend
    );

    public record ExerciseStatsResponse(
        string Exercise,
        int TotalCount,
        int AverageWPM,
        decimal AverageScore,
        int TotalDurationSeconds,
        IEnumerable<object> TrendByDate
    );

    public record DashboardSummaryResponse(
        ProgressSummaryDto Summary,
        StatsResponse Stats,
        IEnumerable<BackendSessionDto> RecentSessions,
        ExerciseStatsResponse ReadingExercise,
        ExerciseStatsResponse MuscleExercise
    );

    [HttpGet("{userId:guid}/summary")]
    public async Task<IActionResult> GetUserSummary([FromRoute] Guid userId)
    {
        var sessions = await _db.Sessions
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.SessionStartDate)
            .ToListAsync();

        var totalSessions = sessions.Count;
        var avgWpm = sessions.Where(s => s.WPM.HasValue).Select(s => s.WPM!.Value).DefaultIfEmpty(0).Average();
        var bestWpm = sessions.Where(s => s.WPM.HasValue).Select(s => s.WPM!.Value).DefaultIfEmpty(0).Max();
        var totalReadingSeconds = sessions.Select(s => s.DurationSeconds ?? 0).Sum();

        // Basit iyileşme oranı: son 7 oturum ortalaması - ilk 7 oturum ortalaması
        var firstSlice = sessions.TakeLast(Math.Min(7, totalSessions)).Where(s => s.WPM.HasValue).Select(s => s.WPM!.Value).DefaultIfEmpty(0).Average();
        var lastSlice = sessions.Take(Math.Min(7, totalSessions)).Where(s => s.WPM.HasValue).Select(s => s.WPM!.Value).DefaultIfEmpty(0).Average();
        var improvementRate = (int)Math.Round(lastSlice - firstSlice);

        var recent = sessions.Take(10).Select(MapToBackendDto).ToList();

        var dto = new ProgressSummaryDto(
            TotalSessions: totalSessions,
            AverageWPM: (int)Math.Round(avgWpm),
            TotalReadingTime: totalReadingSeconds,
            ImprovementRate: improvementRate,
            BestWPM: bestWpm,
            RecentSessions: recent
        );

        return Ok(dto);
    }

    [HttpGet("{userId:guid}/stats")]
    public async Task<IActionResult> GetUserStats([FromRoute] Guid userId, [FromQuery] string period = "week")
    {
        var fromDate = period.ToLower() switch
        {
            "day" => DateTime.UtcNow.Date,
            "week" => DateTime.UtcNow.Date.AddDays(-7),
            "month" => DateTime.UtcNow.Date.AddMonths(-1),
            "year" => DateTime.UtcNow.Date.AddYears(-1),
            _ => DateTime.UtcNow.Date.AddDays(-7)
        };

        var sessionsQ = _db.Sessions.AsNoTracking().Where(s => s.UserId == userId && s.SessionStartDate >= fromDate);
        var sessions = await sessionsQ.OrderBy(s => s.SessionStartDate).ToListAsync();

        var avgWpm = sessions.Where(s => s.WPM.HasValue).Select(s => s.WPM!.Value).DefaultIfEmpty(0).Average();
        var improvementRate = 0;
        if (sessions.Count >= 2)
        {
            var first = sessions.FirstOrDefault(s => s.WPM.HasValue)?.WPM ?? 0;
            var last = sessions.LastOrDefault(s => s.WPM.HasValue)?.WPM ?? 0;
            improvementRate = last - first;
        }

        var sessionsByDate = sessions
            .GroupBy(s => s.SessionStartDate.Date)
            .Select(g => new { date = g.Key, count = g.Count() })
            .ToList();

        var wpmTrend = sessions
            .Where(s => s.WPM.HasValue)
            .Select(s => new { date = s.SessionStartDate.Date, wpm = s.WPM })
            .ToList();

        var compTrend = sessions
            .Where(s => s.ComprehensionScore.HasValue)
            .Select(s => new { date = s.SessionStartDate.Date, comprehension = s.ComprehensionScore })
            .ToList();

        var response = new StatsResponse(
            TotalSessions: sessions.Count,
            TotalWords: 0, // Kelime sayısı metrikleri mevcut olmadığı için 0
            AverageWPM: (int)Math.Round(avgWpm),
            ImprovementRate: improvementRate,
            SessionsByDate: sessionsByDate,
            WpmTrend: wpmTrend,
            ComprehensionTrend: compTrend
        );

        return Ok(response);
    }

    [HttpGet("{userId:guid}/sessions")]
    public async Task<IActionResult> GetUserSessions([FromRoute] Guid userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 20 : pageSize;

        var query = _db.Sessions.AsNoTracking().Where(s => s.UserId == userId);
        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(s => s.SessionStartDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => MapToBackendDto(s))
            .ToListAsync();

        return Ok(new { items, total, page, pageSize });
    }

    [HttpGet("{userId:guid}/exercise-stats")] 
    public async Task<IActionResult> GetExerciseStats([FromRoute] Guid userId, [FromQuery] string exercise = "reading", [FromQuery] string period = "week")
    {
        var fromDate = period.ToLower() switch
        {
            "day" => DateTime.UtcNow.Date,
            "week" => DateTime.UtcNow.Date.AddDays(-7),
            "month" => DateTime.UtcNow.Date.AddMonths(-1),
            "year" => DateTime.UtcNow.Date.AddYears(-1),
            _ => DateTime.UtcNow.Date.AddDays(-7)
        };

        if (exercise.Equals("reading", StringComparison.OrdinalIgnoreCase))
        {
            var q = _db.Sessions.AsNoTracking().Where(s => s.UserId == userId && s.SessionStartDate >= fromDate);
            var list = await q.ToListAsync();
            var avgWpm = list.Where(s => s.WPM.HasValue).Select(s => s.WPM!.Value).DefaultIfEmpty(0).Average();
            var totalDuration = list.Sum(s => s.DurationSeconds ?? 0);
            var trend = list
                .GroupBy(s => s.SessionStartDate.Date)
                .Select(g => new { date = g.Key, count = g.Count(), avgWpm = g.Where(x => x.WPM.HasValue).DefaultIfEmpty().Average(x => x?.WPM ?? 0) })
                .ToList();

            var resp = new ExerciseStatsResponse(
                Exercise: "reading",
                TotalCount: list.Count,
                AverageWPM: (int)Math.Round(avgWpm),
                AverageScore: 0,
                TotalDurationSeconds: totalDuration,
                TrendByDate: trend
            );
            return Ok(resp);
        }
        else // muscle
        {
            var q = _db.Attempts.AsNoTracking().Where(a => a.UserId == userId && a.AttemptDate >= fromDate);
            var list = await q.ToListAsync();
            var avgWpm = list.Where(a => a.WPM.HasValue).Select(a => a.WPM!.Value).DefaultIfEmpty(0).Average();
            var avgScore = list.Where(a => a.Score.HasValue).Select(a => a.Score!.Value).DefaultIfEmpty(0).Average();
            var totalDuration = list.Sum(a => a.DurationSeconds);
            var trend = list
                .GroupBy(a => a.AttemptDate.Date)
                .Select(g => new { date = g.Key, count = g.Count(), avgScore = g.Where(x => x.Score.HasValue).DefaultIfEmpty().Average(x => x?.Score ?? 0) })
                .ToList();

            var resp = new ExerciseStatsResponse(
                Exercise: "muscle",
                TotalCount: list.Count,
                AverageWPM: (int)Math.Round(avgWpm),
                AverageScore: (decimal)Math.Round((double)avgScore, 2),
                TotalDurationSeconds: totalDuration,
                TrendByDate: trend
            );
            return Ok(resp);
        }
    }

    [HttpGet("{userId:guid}/dashboard-summary")] 
    public async Task<IActionResult> GetDashboardSummary([FromRoute] Guid userId, [FromQuery] string period = "week")
    {
        // Compute summary
        var sessionsAll = await _db.Sessions
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.SessionStartDate)
            .ToListAsync();

        var totalSessions = sessionsAll.Count;
        var avgWpm = sessionsAll.Where(s => s.WPM.HasValue).Select(s => s.WPM!.Value).DefaultIfEmpty(0).Average();
        var bestWpm = sessionsAll.Where(s => s.WPM.HasValue).Select(s => s.WPM!.Value).DefaultIfEmpty(0).Max();
        var totalReadingSeconds = sessionsAll.Select(s => s.DurationSeconds ?? 0).Sum();
        var firstSlice = sessionsAll.TakeLast(Math.Min(7, totalSessions)).Where(s => s.WPM.HasValue).Select(s => s.WPM!.Value).DefaultIfEmpty(0).Average();
        var lastSlice = sessionsAll.Take(Math.Min(7, totalSessions)).Where(s => s.WPM.HasValue).Select(s => s.WPM!.Value).DefaultIfEmpty(0).Average();
        var improvementRate = (int)Math.Round(lastSlice - firstSlice);
        var recent = sessionsAll.Take(10).Select(MapToBackendDto).ToList();
        var summary = new ProgressSummaryDto(
            TotalSessions: totalSessions,
            AverageWPM: (int)Math.Round(avgWpm),
            TotalReadingTime: totalReadingSeconds,
            ImprovementRate: improvementRate,
            BestWPM: bestWpm,
            RecentSessions: recent
        );

        // Compute period-based stats
        var fromDate = period.ToLower() switch
        {
            "day" => DateTime.UtcNow.Date,
            "week" => DateTime.UtcNow.Date.AddDays(-7),
            "month" => DateTime.UtcNow.Date.AddMonths(-1),
            "year" => DateTime.UtcNow.Date.AddYears(-1),
            _ => DateTime.UtcNow.Date.AddDays(-7)
        };
        var sessionsPeriod = await _db.Sessions.AsNoTracking()
            .Where(s => s.UserId == userId && s.SessionStartDate >= fromDate)
            .OrderBy(s => s.SessionStartDate)
            .ToListAsync();
        var avgWpmPeriod = sessionsPeriod.Where(s => s.WPM.HasValue).Select(s => s.WPM!.Value).DefaultIfEmpty(0).Average();
        var improvementRatePeriod = 0;
        if (sessionsPeriod.Count >= 2)
        {
            var first = sessionsPeriod.FirstOrDefault(s => s.WPM.HasValue)?.WPM ?? 0;
            var last = sessionsPeriod.LastOrDefault(s => s.WPM.HasValue)?.WPM ?? 0;
            improvementRatePeriod = last - first;
        }
        var sessionsByDate = sessionsPeriod
            .GroupBy(s => s.SessionStartDate.Date)
            .Select(g => new { date = g.Key, count = g.Count() })
            .ToList();
        var wpmTrend = sessionsPeriod
            .Where(s => s.WPM.HasValue)
            .Select(s => new { date = s.SessionStartDate.Date, wpm = s.WPM })
            .ToList();
        var compTrend = sessionsPeriod
            .Where(s => s.ComprehensionScore.HasValue)
            .Select(s => new { date = s.SessionStartDate.Date, comprehension = s.ComprehensionScore })
            .ToList();
        var stats = new StatsResponse(
            TotalSessions: sessionsPeriod.Count,
            TotalWords: 0,
            AverageWPM: (int)Math.Round(avgWpmPeriod),
            ImprovementRate: improvementRatePeriod,
            SessionsByDate: sessionsByDate,
            WpmTrend: wpmTrend,
            ComprehensionTrend: compTrend
        );

        // Recent sessions limited
        var recentSessions = sessionsAll.Take(5).Select(MapToBackendDto).ToList();

        // Exercise stats for reading and muscle in period
        ExerciseStatsResponse readingEx;
        {
            var list = sessionsPeriod;
            var avgWpmR = list.Where(s => s.WPM.HasValue).Select(s => s.WPM!.Value).DefaultIfEmpty(0).Average();
            var totalDur = list.Sum(s => s.DurationSeconds ?? 0);
            var trend = list.GroupBy(s => s.SessionStartDate.Date)
                .Select(g => new { date = g.Key, count = g.Count(), avgWpm = g.Where(x => x.WPM.HasValue).DefaultIfEmpty().Average(x => x?.WPM ?? 0) })
                .ToList();
            readingEx = new ExerciseStatsResponse("reading", list.Count, (int)Math.Round(avgWpmR), 0, totalDur, trend);
        }
        ExerciseStatsResponse muscleEx;
        {
            var list = await _db.Attempts.AsNoTracking()
                .Where(a => a.UserId == userId && a.AttemptDate >= fromDate)
                .OrderBy(a => a.AttemptDate)
                .ToListAsync();
            var avgWpmM = list.Where(a => a.WPM.HasValue).Select(a => a.WPM!.Value).DefaultIfEmpty(0).Average();
            var avgScore = list.Where(a => a.Score.HasValue).Select(a => a.Score!.Value).DefaultIfEmpty(0).Average();
            var totalDur = list.Sum(a => a.DurationSeconds);
            var trend = list.GroupBy(a => a.AttemptDate.Date)
                .Select(g => new { date = g.Key, count = g.Count(), avgScore = g.Where(x => x.Score.HasValue).DefaultIfEmpty().Average(x => x?.Score ?? 0) })
                .ToList();
            muscleEx = new ExerciseStatsResponse("muscle", list.Count, (int)Math.Round(avgWpmM), (decimal)Math.Round((double)avgScore, 2), totalDur, trend);
        }

        var response = new DashboardSummaryResponse(summary, stats, recentSessions, readingEx, muscleEx);
        return Ok(response);
    }

    private static BackendSessionDto MapToBackendDto(UserReadingSession s) => new(
        SessionId: s.SessionId,
        UserId: s.UserId,
        TextId: s.TextId,
        SessionStartDate: s.SessionStartDate,
        SessionEndDate: s.SessionEndDate,
        DurationSeconds: s.DurationSeconds,
        WPM: s.WPM,
        ComprehensionScore: s.ComprehensionScore,
        EyeTrackingMetricsJson: s.EyeTrackingMetricsJson,
        CreatedAt: s.CreatedAt
    );
}

