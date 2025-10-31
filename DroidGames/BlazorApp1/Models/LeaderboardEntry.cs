namespace BlazorApp1.Models;

/// <summary>
/// Položka leaderboardu s kompletními informacemi o týmu
/// </summary>
public class LeaderboardEntry
{
    public string TeamId { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public int TotalScore { get; set; }
    public int CompletedRounds { get; set; }
    public int Position { get; set; }
    public int PreviousPosition { get; set; } // Pro animaci změny pozice
    public List<FinalRoundScore> RoundScores { get; set; } = new();
}
