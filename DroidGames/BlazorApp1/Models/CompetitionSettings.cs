namespace BlazorApp1.Models;

public class CompetitionSettings
{
    public int CurrentRound { get; set; } = 1;
    public CompetitionStatus Status { get; set; } = CompetitionStatus.NotStarted;
    public int RoundDurationSeconds { get; set; } = 90;
    public int PreparationDurationSeconds { get; set; } = 60;
    public int TotalRounds { get; set; } = 5;
    public int MaxTeams { get; set; } = 16;
    public int MaxTeamsPerSchool { get; set; } = 3;
    public DateTime? TimerStartedAt { get; set; }
    public int TimerRemainingSeconds { get; set; } = 90;
    public TimerStatus TimerStatus { get; set; } = TimerStatus.Stopped;
    public string? CurrentTeamAId { get; set; }
    public string? CurrentTeamBId { get; set; }
    public string? NextTeamAId { get; set; }
    public string? NextTeamBId { get; set; }
    public string HardwareApiKey { get; set; } = Guid.NewGuid().ToString();
}
