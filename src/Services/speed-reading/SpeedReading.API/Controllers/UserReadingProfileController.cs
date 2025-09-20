using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SpeedReading.API.Models;

namespace SpeedReading.API.Controllers;

/// <summary>
/// Controller for managing user reading profiles
/// </summary>
[ApiController]
[Route("api/v1/user-reading-profiles")]
[Produces("application/json")]
[ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
public class UserReadingProfileController : ControllerBase
{
    private readonly ILogger<UserReadingProfileController> _logger;

    /// <summary>
    /// Initializes a new instance of the UserReadingProfileController
    /// </summary>
    public UserReadingProfileController(ILogger<UserReadingProfileController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets user profile by ID
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>User profile</returns>
    [HttpGet("{id}")]
    [Authorize(Policy = "user-reading-profiles.read")]
    [ProducesResponseType(typeof(SpeedReadingProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public ActionResult<SpeedReadingProfileDto> GetProfile([FromRoute] string id)
    {
        try
        {
            // Enhanced mock response matching frontend expectations
            var profile = new SpeedReadingProfileDto
            {
                UserId = id,
                CurrentLevel = "Intermediate",
                CurrentWPM = 250,
                TargetWPM = 400,
                ComprehensionRate = 85.5,
                TotalTextsRead = 15,
                TotalTimeSpent = 120, // minutes
                Achievements = new[] { "Speed Demon", "Comprehension Master" },
                Preferences = new SpeedReadingPreferences
                {
                    FontSize = 16,
                    FontFamily = "Arial",
                    BackgroundColor = "#ffffff",
                    TextColor = "#000000",
                    LineHeight = 1.5,
                    WordsPerLine = 12
                },
                Statistics = new SpeedReadingStatistics
                {
                    AverageWPM = 240,
                    AverageComprehension = 83.2,
                    BestWPM = 320,
                    BestComprehension = 95.0,
                    StreakDays = 7,
                    TotalSessions = 25
                },
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow
            };

            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving profile {Id}", id);
            return StatusCode(500, ApiResponse<object>.Fail("An error occurred while retrieving the profile"));
        }
    }

    /// <summary>
    /// Updates user profile
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="request">Profile update request</param>
    /// <returns>Updated profile</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(SpeedReadingProfileDto), StatusCodes.Status200OK)]
    public ActionResult<SpeedReadingProfileDto> UpdateProfile([FromRoute] string id, [FromBody] UpdateProfileRequest request)
    {
        try
        {
            // Mock update - return updated profile
            var profile = new SpeedReadingProfileDto
            {
                UserId = id,
                CurrentLevel = "Intermediate",
                CurrentWPM = 250,
                TargetWPM = request.TargetWPM ?? 400,
                ComprehensionRate = 85.5,
                TotalTextsRead = 15,
                TotalTimeSpent = 120,
                Achievements = new[] { "Speed Demon", "Comprehension Master" },
                Preferences = request.Preferences ?? new SpeedReadingPreferences
                {
                    FontSize = 16,
                    FontFamily = "Arial",
                    BackgroundColor = "#ffffff",
                    TextColor = "#000000",
                    LineHeight = 1.5,
                    WordsPerLine = 12
                },
                Statistics = new SpeedReadingStatistics
                {
                    AverageWPM = 240,
                    AverageComprehension = 83.2,
                    BestWPM = 320,
                    BestComprehension = 95.0,
                    StreakDays = 7,
                    TotalSessions = 25
                },
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow
            };

            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile");
            return StatusCode(500, ApiResponse<object>.Fail("An error occurred while updating the profile"));
        }
    }

    /// <summary>
    /// Gets profile statistics
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="period">Statistics period</param>
    /// <returns>Profile statistics</returns>
    [HttpGet("{id}/statistics")]
    [ProducesResponseType(typeof(ProfileStatisticsDto), StatusCodes.Status200OK)]
    public ActionResult<ProfileStatisticsDto> GetProfileStatistics([FromRoute] string id, [FromQuery] string period = "weekly")
    {
        try
        {
            var stats = new ProfileStatisticsDto
            {
                UserId = id,
                Period = period,
                WpmProgress = new[]
                {
                    new WpmProgressItem { Date = "2024-01-01", Wpm = 200, Comprehension = 80 },
                    new WpmProgressItem { Date = "2024-01-02", Wpm = 210, Comprehension = 82 },
                    new WpmProgressItem { Date = "2024-01-03", Wpm = 225, Comprehension = 85 },
                    new WpmProgressItem { Date = "2024-01-04", Wpm = 240, Comprehension = 83 },
                    new WpmProgressItem { Date = "2024-01-05", Wpm = 250, Comprehension = 86 }
                },
                TextsByDifficulty = new[]
                {
                    new TextsByDifficultyItem { Level = "Easy", Count = 8 },
                    new TextsByDifficultyItem { Level = "Medium", Count = 5 },
                    new TextsByDifficultyItem { Level = "Hard", Count = 2 }
                },
                TimeByDay = new[]
                {
                    new TimeByDayItem { Day = "Monday", Minutes = 30 },
                    new TimeByDayItem { Day = "Tuesday", Minutes = 25 },
                    new TimeByDayItem { Day = "Wednesday", Minutes = 35 },
                    new TimeByDayItem { Day = "Thursday", Minutes = 20 },
                    new TimeByDayItem { Day = "Friday", Minutes = 40 }
                },
                Achievements = new[]
                {
                    new AchievementItem { Date = "2024-01-01", Achievement = "First Steps", Description = "Completed first reading session" },
                    new AchievementItem { Date = "2024-01-03", Achievement = "Speed Boost", Description = "Reached 200+ WPM" }
                }
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statistics for {Id}", id);
            return StatusCode(500, ApiResponse<object>.Fail("An error occurred while retrieving statistics"));
        }
    }

    /// <summary>
    /// Gets leaderboard
    /// </summary>
    /// <param name="limit">Number of entries</param>
    /// <param name="category">Category to sort by</param>
    /// <returns>Leaderboard entries</returns>
    [HttpGet("leaderboard")]
    [ProducesResponseType(typeof(LeaderboardEntry[]), StatusCodes.Status200OK)]
    public ActionResult<LeaderboardEntry[]> GetLeaderboard([FromQuery] int limit = 10, [FromQuery] string category = "wpm")
    {
        try
        {
            var leaderboard = new[]
            {
                new LeaderboardEntry { UserId = "user1", UserName = "John Doe", Wpm = 450, ComprehensionRate = 92.5, Rank = 1 },
                new LeaderboardEntry { UserId = "user2", UserName = "Jane Smith", Wpm = 430, ComprehensionRate = 89.2, Rank = 2 },
                new LeaderboardEntry { UserId = "user3", UserName = "Bob Johnson", Wpm = 410, ComprehensionRate = 94.1, Rank = 3 },
                new LeaderboardEntry { UserId = "user4", UserName = "Alice Brown", Wpm = 395, ComprehensionRate = 91.8, Rank = 4 },
                new LeaderboardEntry { UserId = "user5", UserName = "Charlie Wilson", Wpm = 380, ComprehensionRate = 88.7, Rank = 5 }
            }.Take(limit).ToArray();

            return Ok(leaderboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting leaderboard");
            return StatusCode(500, ApiResponse<object>.Fail("An error occurred while retrieving leaderboard"));
        }
    }
}

// DTOs and Models

public class SpeedReadingProfileDto
{
    public string UserId { get; set; } = string.Empty;
    public string CurrentLevel { get; set; } = string.Empty;
    public int CurrentWPM { get; set; }
    public int TargetWPM { get; set; }
    public double ComprehensionRate { get; set; }
    public int TotalTextsRead { get; set; }
    public int TotalTimeSpent { get; set; }
    public string[] Achievements { get; set; } = Array.Empty<string>();
    public SpeedReadingPreferences Preferences { get; set; } = new();
    public SpeedReadingStatistics Statistics { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class SpeedReadingPreferences
{
    public int FontSize { get; set; }
    public string FontFamily { get; set; } = string.Empty;
    public string BackgroundColor { get; set; } = string.Empty;
    public string TextColor { get; set; } = string.Empty;
    public double LineHeight { get; set; }
    public int WordsPerLine { get; set; }
}

public class SpeedReadingStatistics
{
    public double AverageWPM { get; set; }
    public double AverageComprehension { get; set; }
    public double BestWPM { get; set; }
    public double BestComprehension { get; set; }
    public int StreakDays { get; set; }
    public int TotalSessions { get; set; }
}

public class UpdateProfileRequest
{
    public int? TargetWPM { get; set; }
    public SpeedReadingPreferences? Preferences { get; set; }
}

public class ProfileStatisticsDto
{
    public string UserId { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public WpmProgressItem[] WpmProgress { get; set; } = Array.Empty<WpmProgressItem>();
    public TextsByDifficultyItem[] TextsByDifficulty { get; set; } = Array.Empty<TextsByDifficultyItem>();
    public TimeByDayItem[] TimeByDay { get; set; } = Array.Empty<TimeByDayItem>();
    public AchievementItem[] Achievements { get; set; } = Array.Empty<AchievementItem>();
}

public class WpmProgressItem
{
    public string Date { get; set; } = string.Empty;
    public double Wpm { get; set; }
    public double Comprehension { get; set; }
}

public class TextsByDifficultyItem
{
    public string Level { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class TimeByDayItem
{
    public string Day { get; set; } = string.Empty;
    public int Minutes { get; set; }
}

public class AchievementItem
{
    public string Date { get; set; } = string.Empty;
    public string Achievement { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class LeaderboardEntry
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public double Wpm { get; set; }
    public double ComprehensionRate { get; set; }
    public int Rank { get; set; }
}