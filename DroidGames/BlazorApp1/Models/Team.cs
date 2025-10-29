namespace BlazorApp1.Models;

public class Team
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string School { get; set; } = string.Empty;
    public List<string> Members { get; set; } = new();
    public string PinCode { get; set; } = string.Empty;
    public string? RobotPhotoUrl { get; set; }
    public string? RobotDescription { get; set; }
    public List<RoundParticipation> Rounds { get; set; } = new();
    public List<string> UnlockedAchievements { get; set; } = new();
    public int TotalScore { get; set; }
    public int CurrentPosition { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
