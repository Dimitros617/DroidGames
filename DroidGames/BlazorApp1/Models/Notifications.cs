namespace BlazorApp1.Models;

/// <summary>
/// Notifikace o dokončení jízdy s výsledky
/// </summary>
public class RoundCompletedNotification
{
    public string TeamId { get; set; } = string.Empty;
    public int RoundNumber { get; set; }
    public int CrystalPoints { get; set; }
    public int SulfurPenalty { get; set; }
    public int BonusPoints { get; set; }
    public int TotalScore { get; set; }
    public DateTime CompletedAt { get; set; }
}

/// <summary>
/// Notifikace o získání achievementu
/// </summary>
public class AchievementUnlockedNotification
{
    public string TeamId { get; set; } = string.Empty;
    public string AchievementId { get; set; } = string.Empty;
    public string AchievementName { get; set; } = string.Empty;
    public string AchievementDescription { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public AchievementRarity Rarity { get; set; }
    public int Points { get; set; }
    public DateTime UnlockedAt { get; set; }
}

/// <summary>
/// Typ notifikace pro kartičky
/// </summary>
public enum NotificationType
{
    RoundCompleted,
    AchievementUnlocked,
    Info,
    Warning,
    Success
}
