namespace BlazorApp1.Models;

public class MapConfiguration
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public int RoundNumber { get; set; }
    public bool IsPublished { get; set; }
    
    // Grid 6x9 (6 rows = Y axis, 9 columns = X axis)
    // Grid[Y][X] - where Y is row (0-5), X is column (0-8)
    public List<MapBlock> Blocks { get; set; } = new();
    
    public double AverageScore { get; set; }
    public int TimesPlayed { get; set; }
}
