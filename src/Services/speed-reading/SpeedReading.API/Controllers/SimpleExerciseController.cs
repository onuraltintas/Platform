using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SpeedReading.API.Models;

namespace SpeedReading.API.Controllers;

/// <summary>
/// Simplified controller for exercises
/// </summary>
[ApiController]
[Route("api/v1/exercises")]
[Produces("application/json")]
public class SimpleExerciseController : ControllerBase
{
    private readonly ILogger<SimpleExerciseController> _logger;

    public SimpleExerciseController(ILogger<SimpleExerciseController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets all exercises
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "exercises.read")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<object>> GetExercises()
    {
        try
        {
            var exercises = new[]
            {
                new
                {
                    Id = Guid.NewGuid(),
                    Title = "Temel Okuduğunu Anlama Testi",
                    Description = "Basit bir okuma metni ve anlama soruları",
                    Type = "ReadingComprehension",
                    DifficultyLevel = "Beginner",
                    QuestionCount = 5,
                    TimeLimit = 10,
                    CreatedAt = DateTime.UtcNow
                },
                new
                {
                    Id = Guid.NewGuid(),
                    Title = "Hız Okuma Egzersizi",
                    Description = "Okuma hızını artırmaya yönelik egzersiz",
                    Type = "SpeedReading",
                    DifficultyLevel = "Intermediate",
                    QuestionCount = 8,
                    TimeLimit = 15,
                    CreatedAt = DateTime.UtcNow
                }
            };

            return Ok(ApiResponse<object>.Ok(exercises));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving exercises");
            return StatusCode(500, ApiResponse<object>.Fail("An error occurred"));
        }
    }

    /// <summary>
    /// Gets exercise by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "exercises.read")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<object>> GetExercise([FromRoute] Guid id)
    {
        try
        {
            var exercise = new
            {
                Id = id,
                Title = "Test Egzersizi",
                Description = "Bu bir test egzersizidir",
                Type = "ReadingComprehension",
                DifficultyLevel = "Beginner",
                Questions = new[]
                {
                    new
                    {
                        Id = Guid.NewGuid(),
                        Text = "Metinde bahsedilen ana konu nedir?",
                        Type = "MultipleChoice",
                        Options = new[]
                        {
                            new { Id = Guid.NewGuid(), Text = "A) Tarih", IsCorrect = false },
                            new { Id = Guid.NewGuid(), Text = "B) Coğrafya", IsCorrect = true },
                            new { Id = Guid.NewGuid(), Text = "C) Edebiyat", IsCorrect = false },
                            new { Id = Guid.NewGuid(), Text = "D) Bilim", IsCorrect = false }
                        }
                    }
                },
                TimeLimit = 10,
                CreatedAt = DateTime.UtcNow
            };

            return Ok(ApiResponse<object>.Ok(exercise));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving exercise {Id}", id);
            return StatusCode(500, ApiResponse<object>.Fail("An error occurred"));
        }
    }

    /// <summary>
    /// Creates a new exercise
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "exercises.write")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    public ActionResult<ApiResponse<object>> CreateExercise([FromBody] CreateExerciseRequest request)
    {
        try
        {
            var exercise = new
            {
                Id = Guid.NewGuid(),
                Title = request.Title ?? "Yeni Egzersiz",
                Description = request.Description ?? "Açıklama",
                Type = request.Type ?? "ReadingComprehension",
                CreatedAt = DateTime.UtcNow
            };

            return CreatedAtAction(nameof(GetExercise), new { id = exercise.Id },
                ApiResponse<object>.Ok(exercise));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating exercise");
            return StatusCode(500, ApiResponse<object>.Fail("An error occurred"));
        }
    }
}

/// <summary>
/// Request model for creating exercise
/// </summary>
public class CreateExerciseRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Type { get; set; }
}