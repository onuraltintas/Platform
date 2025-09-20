using SpeedReading.Domain.Enums;

namespace SpeedReading.Domain.Entities;

public class EducationLevel
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Name { get; private set; } = string.Empty;
    public EducationCategory Category { get; private set; }
    public int GradeNumber { get; private set; }
    public string? AgeRange { get; private set; }
    public bool IsActive { get; private set; }

    private EducationLevel() { }

    public EducationLevel(int id, string name, EducationCategory category, int gradeNumber, string? ageRange = null)
    {
        Id = id;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Category = category;
        GradeNumber = gradeNumber;
        AgeRange = ageRange;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }
}