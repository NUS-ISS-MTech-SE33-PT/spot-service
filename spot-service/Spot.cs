public class Spot
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string FoodType { get; set; } = string.Empty;
    public double Rating { get; set; }
    public string OpeningHours { get; set; } = string.Empty;
    public List<string> Photos { get; set; } = new();
    public string PlaceType { get; set; } = string.Empty;

    // Optional properties
    public double? AvgPrice { get; set; }
    public bool Open { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? TasteAvg { get; set; }
    public double? ServiceAvg { get; set; }
    public double? EnvironmentAvg { get; set; }
    public string? District { get; set; }
    public string? ThumbnailUrl { get; set; }
    public bool IsCenter { get; set; }
    public CenterSummary? ParentCenter { get; set; }
}