namespace BlazorApp1.Models;

public class RefereeScore
{
    public string RefereeId { get; set; } = string.Empty;
    public Dictionary<string, int> ScoreBreakdown { get; set; } = new();
    public int TotalScore { get; set; }
    public DateTime SubmittedAt { get; set; }
}
