using Microsoft.AspNetCore.Mvc;
using SpeedReading.API.Models;

namespace SpeedReading.API.Controllers;

/// <summary>
/// Simplified controller for analytics
/// </summary>
[ApiController]
[Route("api/v1/analytics")]
[Produces("application/json")]
public class SimpleAnalyticsController : ControllerBase
{
    private readonly ILogger<SimpleAnalyticsController> _logger;

    public SimpleAnalyticsController(ILogger<SimpleAnalyticsController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets user performance report
    /// </summary>
    [HttpGet("user/{userId:guid}/report")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<object>> GetUserReport([FromRoute] Guid userId)
    {
        try
        {
            var report = new
            {
                UserId = userId,
                TotalExercises = 15,
                CompletedExercises = 12,
                AverageScore = 78.5,
                ReadingSpeed = 225, // Words per minute
                ComprehensionRate = 85.3,
                LastActivity = DateTime.UtcNow.AddDays(-1),
                PerformanceLevel = "Intermediate",
                Recommendations = new[]
                {
                    "Hız okuma egzersizlerinize odaklanın",
                    "Kelime dağarcığınızı genişletmek için vocabulary testleri yapın",
                    "Uzun metinlerle pratik yapın"
                }
            };

            return Ok(ApiResponse<object>.Ok(report));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating user report for {UserId}", userId);
            return StatusCode(500, ApiResponse<object>.Fail("An error occurred"));
        }
    }

    /// <summary>
    /// Gets user performance trends
    /// </summary>
    [HttpGet("user/{userId:guid}/trends")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<object>> GetUserTrends([FromRoute] Guid userId)
    {
        try
        {
            var trends = new[]
            {
                new { Date = DateTime.UtcNow.AddDays(-7), Score = 65.0, Speed = 180 },
                new { Date = DateTime.UtcNow.AddDays(-6), Score = 70.0, Speed = 190 },
                new { Date = DateTime.UtcNow.AddDays(-5), Score = 68.0, Speed = 195 },
                new { Date = DateTime.UtcNow.AddDays(-4), Score = 75.0, Speed = 200 },
                new { Date = DateTime.UtcNow.AddDays(-3), Score = 72.0, Speed = 205 },
                new { Date = DateTime.UtcNow.AddDays(-2), Score = 78.0, Speed = 215 },
                new { Date = DateTime.UtcNow.AddDays(-1), Score = 80.0, Speed = 225 }
            };

            return Ok(ApiResponse<object>.Ok(new { Trends = trends, UserId = userId }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating trends for {UserId}", userId);
            return StatusCode(500, ApiResponse<object>.Fail("An error occurred"));
        }
    }

    /// <summary>
    /// Gets system dashboard
    /// </summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<object>> GetSystemDashboard()
    {
        try
        {
            var dashboard = new
            {
                TotalUsers = 1250,
                ActiveUsers = 892,
                TotalExercises = 45,
                CompletedAttempts = 18753,
                AverageCompletionRate = 78.3,
                TopPerformingCategories = new[] { "History", "Geography", "Literature" },
                RecentActivity = new[]
                {
                    new { User = "Ahmet K.", Activity = "Completed Speed Reading Exercise", Time = DateTime.UtcNow.AddMinutes(-15) },
                    new { User = "Fatma A.", Activity = "Started Comprehension Test", Time = DateTime.UtcNow.AddMinutes(-23) },
                    new { User = "Mehmet B.", Activity = "Achieved 300 WPM milestone", Time = DateTime.UtcNow.AddMinutes(-45) }
                }
            };

            return Ok(ApiResponse<object>.Ok(dashboard));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating dashboard");
            return StatusCode(500, ApiResponse<object>.Fail("An error occurred"));
        }
    }
}