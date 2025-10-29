namespace BlazorApp1.Models;

public class RoundParticipation
{
    public int RoundNumber { get; set; }
    public string OpponentTeamId { get; set; } = string.Empty;
    public string MapConfigurationId { get; set; } = string.Empty;
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int? Duration { get; set; }
    public Dictionary<string, RefereeScore> RefereeScores { get; set; } = new();
    public bool IsApproved { get; set; }
    public int? FinalScore { get; set; }
    public List<MapAction> Actions { get; set; } = new();
}
