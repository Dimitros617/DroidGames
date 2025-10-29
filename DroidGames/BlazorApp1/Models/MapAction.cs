namespace BlazorApp1.Models;

public class MapAction
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; }
    public int ElapsedSeconds { get; set; }
    public MapActionType ActionType { get; set; }
    public int BlockX { get; set; }
    public int BlockY { get; set; }
    public int? TargetX { get; set; }
    public int? TargetY { get; set; }
    public string RefereeId { get; set; } = string.Empty;
}
