using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpeedReading.ContentService.Data;

namespace SpeedReading.ContentService.Controllers;

[ApiController]
[Route("api/v1/questions")] 
public class PublicQuestionsController : ControllerBase
{
    private readonly ContentDbContext _db;
    public PublicQuestionsController(ContentDbContext db) { _db = db; }

    public record PublicQuestionDto(Guid QuestionId, Guid TextId, string QuestionText, string QuestionType, string CorrectAnswer, string? OptionsJson);

    [HttpGet]
    public async Task<ActionResult<object>> ListByText([FromQuery] Guid textId)
    {
        if (textId == Guid.Empty) return BadRequest();
        var items = await _db.Questions.AsNoTracking()
            .Where(q => q.TextId == textId)
            .OrderBy(q => q.QuestionText)
            .Select(q => new PublicQuestionDto(q.QuestionId, q.TextId, q.QuestionText, q.QuestionType ?? string.Empty, q.CorrectAnswer ?? string.Empty, q.OptionsJson))
            .ToListAsync();
        return Ok(new { items, total = items.Count });
    }
}

