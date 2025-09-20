namespace SpeedReading.Domain.Entities;

public class District
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int CityId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    public City City { get; private set; } = null!;

    private District() { }

    public District(int id, int cityId, string name)
    {
        Id = id;
        CityId = cityId;
        Name = name ?? throw new ArgumentNullException(nameof(name));
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