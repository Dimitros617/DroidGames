namespace BlazorApp1.Models;

public class AchievementCondition
{
    public AchievementConditionType Type { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
}
