namespace SpeedReading.Application.DTOs;

public class CityDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PlateCode { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public List<DistrictDto> Districts { get; set; } = new();
}

public class DistrictDto
{
    public int Id { get; set; }
    public int CityId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CityName { get; set; } = string.Empty;
}

public class EducationLevelDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Category { get; set; }
    public string CategoryDisplay { get; set; } = string.Empty;
    public int GradeNumber { get; set; }
    public string? AgeRange { get; set; }
}