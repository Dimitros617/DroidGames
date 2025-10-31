namespace BlazorApp1.Models;

public class RefereeScore
{
    public string RefereeId { get; set; } = string.Empty;
    public Dictionary<string, int> ScoreBreakdown { get; set; } = new();
    public int TotalScore { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime LastModifiedAt { get; set; }
    
    // Workflow status
    public bool IsSubmitted { get; set; } = false;
    public bool IsApproved { get; set; } = false;
    public bool IsRejected { get; set; } = false;
    public string? ApprovedByRefereeId { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? ApprovedAt { get; set; }
    
    // Detailed event data for auto-save
    public List<ScoringEventData> Events { get; set; } = new();
}

public class ScoringEventData
{
    public int Id { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public string BlockType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Points { get; set; }
    public DateTime Timestamp { get; set; }
}
