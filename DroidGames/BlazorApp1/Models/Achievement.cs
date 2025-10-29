namespace BlazorApp1.Models;

public class Achievement
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public AchievementRarity Rarity { get; set; }
    public bool IsHidden { get; set; }
    public AchievementCondition Condition { get; set; } = new();
}
