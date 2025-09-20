using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SpeedReading.API.Models;

namespace SpeedReading.API.Controllers;

/// <summary>
/// Simplified controller for managing reading texts
/// </summary>
[ApiController]
[Route("api/v1/reading-texts")]
[Produces("application/json")]
public class SimpleReadingTextController : ControllerBase
{
    private readonly ILogger<SimpleReadingTextController> _logger;

    public SimpleReadingTextController(ILogger<SimpleReadingTextController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets all reading texts
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "reading-texts.read")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<object>> GetReadingTexts()
    {
        try
        {
            var texts = new[]
            {
                new
                {
                    Id = Guid.NewGuid(),
                    Title = "Osmanlı İmparatorluğu'nun Kuruluşu",
                    Content = "Osmanlı İmparatorluğu 1299 yılında Osman Gazi tarafından kurulmuştur...",
                    WordCount = 350,
                    DifficultyLevel = "Intermediate",
                    Category = "History",
                    EstimatedReadingTime = 2
                },
                new
                {
                    Id = Guid.NewGuid(),
                    Title = "Türkiye'nin Coğrafi Özellikleri",
                    Content = "Türkiye, üç tarafı denizlerle çevrili yarımada durumundadır...",
                    WordCount = 420,
                    DifficultyLevel = "Beginner",
                    Category = "Geography",
                    EstimatedReadingTime = 3
                }
            };

            return Ok(ApiResponse<object>.Ok(texts));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reading texts");
            return StatusCode(500, ApiResponse<object>.Fail("An error occurred"));
        }
    }

    /// <summary>
    /// Gets reading text by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "reading-texts.read")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<object>> GetReadingText([FromRoute] Guid id)
    {
        try
        {
            var text = new
            {
                Id = id,
                Title = "Test Okuma Metni",
                Content = "Bu bir test okuma metnidir. Türkçe hız okuma becerilerinizi geliştirmek için kullanılabilir.",
                WordCount = 15,
                DifficultyLevel = "Beginner",
                Category = "Test",
                EstimatedReadingTime = 1
            };

            return Ok(ApiResponse<object>.Ok(text));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reading text {Id}", id);
            return StatusCode(500, ApiResponse<object>.Fail("An error occurred"));
        }
    }
}