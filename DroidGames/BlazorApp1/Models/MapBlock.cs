namespace BlazorApp1.Models;

public class MapBlock
{
    public int X { get; set; } // Column (0-8)
    public int Y { get; set; } // Row (0-5)
    public MapBlockType Type { get; set; }
    public string? CustomTag { get; set; } // Optional custom tags like "Shromaždiště", "Startovní pozice", etc.
    public int TouchCount { get; set; }
    public int MoveCount { get; set; }
    public int DisruptionCount { get; set; }
}
