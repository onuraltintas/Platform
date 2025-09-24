using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SpeedReading.API.Models;

namespace SpeedReading.API.Controllers;

/// <summary>
/// Controller for metadata endpoints
/// </summary>
[ApiController]
[Route("metadata")]
[Produces("application/json")]
public class MetadataController : ControllerBase
{
    private readonly ILogger<MetadataController> _logger;

    public MetadataController(ILogger<MetadataController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets available categories
    /// </summary>
    [HttpGet("categories")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(string[]), StatusCodes.Status200OK)]
    public ActionResult<string[]> GetCategories()
    {
        try
        {
            var categories = new[]
            {
                "History",
                "Science",
                "Literature",
                "Technology",
                "Health",
                "Business",
                "Arts",
                "Sports",
                "Politics",
                "Education"
            };

            return Ok(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving categories");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets available tags
    /// </summary>
    [HttpGet("tags")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(string[]), StatusCodes.Status200OK)]
    public ActionResult<string[]> GetTags()
    {
        try
        {
            var tags = new[]
            {
                "beginner",
                "intermediate",
                "advanced",
                "academic",
                "popular",
                "technical",
                "narrative",
                "analytical",
                "descriptive",
                "argumentative"
            };

            return Ok(tags);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tags");
            return StatusCode(500, "Internal server error");
        }
    }
}