namespace BlazorApp1.Models;

public class TeamAchievement
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string TeamId { get; set; } = string.Empty;
    public string AchievementId { get; set; } = string.Empty;
    public DateTime UnlockedAt { get; set; }
    public Dictionary<string, object> UnlockData { get; set; } = new(); // Extra data o odemčení
}
