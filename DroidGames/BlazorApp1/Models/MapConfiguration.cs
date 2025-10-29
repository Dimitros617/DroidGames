namespace BlazorApp1.Models;

public class MapConfiguration
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public int RoundNumber { get; set; }
    public bool IsPublished { get; set; }
    public List<MapBlock> LeftSide { get; set; } = new();
    public List<MapBlock> RightSide { get; set; } = new();
    public List<MapBlock> CenterLine { get; set; } = new();
    public double AverageScore { get; set; }
    public int TimesPlayed { get; set; }
}
