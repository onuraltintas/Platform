namespace SpeedReading.Domain.Entities;

public class City
{
    public int Id { get; set; }
    public string Name { get; private set; } = string.Empty;
    public string PlateCode { get; private set; } = string.Empty;
    public string Region { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    private readonly List<District> _districts = new();
    public IReadOnlyList<District> Districts => _districts.AsReadOnly();

    private City() { }

    public City(int id, string name, string plateCode, string region)
    {
        Id = id;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        PlateCode = plateCode ?? throw new ArgumentNullException(nameof(plateCode));
        Region = region ?? throw new ArgumentNullException(nameof(region));
        IsActive = true;
    }

    public void AddDistrict(District district)
    {
        if (district == null) throw new ArgumentNullException(nameof(district));
        if (_districts.Any(d => d.Name.Equals(district.Name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"District {district.Name} already exists in {Name}");
        
        _districts.Add(district);
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