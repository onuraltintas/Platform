using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpeedReading.ContentService.Data;

namespace SpeedReading.ContentService.Controllers;

[ApiController]
[Route("api/v1/admin/exercise-types")]
public class ExerciseTypesAdminController : ControllerBase
{
    private readonly ContentDbContext _db;
    public ExerciseTypesAdminController(ContentDbContext db) { _db = db; }

    public record ExerciseTypeDto(Guid ExerciseTypeId, string TypeName, string? Description);
    public record UpsertExerciseTypeRequest(string TypeName, string? Description);

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var items = await _db.ExerciseTypes.AsNoTracking().OrderBy(x => x.TypeName)
            .Select(x => new ExerciseTypeDto(x.ExerciseTypeId, x.TypeName, x.Description)).ToListAsync();
        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult<ExerciseTypeDto>> Create([FromBody] UpsertExerciseTypeRequest r)
    {
        var x = new ExerciseTypeEntity { ExerciseTypeId = Guid.NewGuid(), TypeName = r.TypeName, Description = r.Description };
        _db.ExerciseTypes.Add(x);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = x.ExerciseTypeId }, new ExerciseTypeDto(x.ExerciseTypeId, x.TypeName, x.Description));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ExerciseTypeDto>> Get(Guid id)
    {
        var x = await _db.ExerciseTypes.AsNoTracking().FirstOrDefaultAsync(t => t.ExerciseTypeId == id);
        if (x == null) return NotFound();
        return Ok(new ExerciseTypeDto(x.ExerciseTypeId, x.TypeName, x.Description));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ExerciseTypeDto>> Update(Guid id, [FromBody] UpsertExerciseTypeRequest r)
    {
        var x = await _db.ExerciseTypes.FirstOrDefaultAsync(t => t.ExerciseTypeId == id);
        if (x == null) return NotFound();
        x.TypeName = r.TypeName; x.Description = r.Description;
        await _db.SaveChangesAsync();
        return Ok(new ExerciseTypeDto(x.ExerciseTypeId, x.TypeName, x.Description));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var x = await _db.ExerciseTypes.FirstOrDefaultAsync(t => t.ExerciseTypeId == id);
        if (x == null) return NotFound();
        _db.ExerciseTypes.Remove(x);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

