using SpeedReading.Domain.Enums;

namespace SpeedReading.Domain.ValueObjects;

public class Demographics
{
    public DateTime? DateOfBirth { get; private set; }
    public Gender Gender { get; private set; }
    public string? City { get; private set; }
    public string? District { get; private set; }
    public int? GradeLevel { get; private set; }
    public SchoolType SchoolType { get; private set; }

    private Demographics() { }

    public Demographics(
        DateTime? dateOfBirth = null,
        Gender gender = Gender.NotSpecified,
        string? city = null,
        string? district = null,
        int? gradeLevel = null,
        SchoolType schoolType = SchoolType.NotSpecified)
    {
        DateOfBirth = dateOfBirth;
        Gender = gender;
        City = city?.Trim();
        District = district?.Trim();
        GradeLevel = gradeLevel;
        SchoolType = schoolType;
    }

    public int? CalculateAge()
    {
        if (!DateOfBirth.HasValue) return null;
        
        var today = DateTime.Today;
        var age = today.Year - DateOfBirth.Value.Year;
        
        if (DateOfBirth.Value.Date > today.AddYears(-age))
            age--;
            
        return age;
    }

    public EducationCategory GetEducationCategory()
    {
        if (!GradeLevel.HasValue) return EducationCategory.Adult;
        
        return GradeLevel.Value switch
        {
            >= 1 and <= 4 => EducationCategory.Elementary,
            >= 5 and <= 8 => EducationCategory.MiddleSchool,
            >= 9 and <= 12 => EducationCategory.HighSchool,
            >= 13 and <= 16 => EducationCategory.University,
            >= 17 => EducationCategory.Graduate,
            _ => EducationCategory.Adult
        };
    }

    public bool IsProfileComplete()
    {
        return DateOfBirth.HasValue &&
               Gender != Gender.NotSpecified &&
               !string.IsNullOrWhiteSpace(City) &&
               !string.IsNullOrWhiteSpace(District) &&
               GradeLevel.HasValue &&
               SchoolType != SchoolType.NotSpecified;
    }

    // Equality implementation removed - was: protected override IEnumerable<object?> GetEqualityComponents()
}