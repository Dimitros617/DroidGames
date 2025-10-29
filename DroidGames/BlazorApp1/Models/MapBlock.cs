namespace BlazorApp1.Models;

public class MapBlock
{
    public int X { get; set; }
    public int Y { get; set; }
    public MapBlockType Type { get; set; }
    public int TouchCount { get; set; }
    public int MoveCount { get; set; }
    public int DisruptionCount { get; set; }
}
