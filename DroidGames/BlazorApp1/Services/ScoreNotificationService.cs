using BlazorApp1.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace BlazorApp1.Services;

public interface IScoreNotificationService
{
    Task NotifyRefereeScoreUpdated(string teamId, int roundNumber, string refereeId);
    Task NotifyScoreApprovalChanged(string teamId, string refereeId);
    
    // Events for Blazor components to subscribe to
    event Func<string, int, string, Task>? OnRefereeScoreUpdated;
    event Func<string, string, Task>? OnScoreApprovalChanged;
}

public class ScoreNotificationService : IScoreNotificationService
{
    private readonly IHubContext<ScoreboardHub> _hubContext;
    private readonly ILogger<ScoreNotificationService> _logger;

    public ScoreNotificationService(
        IHubContext<ScoreboardHub> hubContext,
        ILogger<ScoreNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public event Func<string, int, string, Task>? OnRefereeScoreUpdated;
    public event Func<string, string, Task>? OnScoreApprovalChanged;

    public async Task NotifyRefereeScoreUpdated(string teamId, int roundNumber, string refereeId)
    {
        _logger.LogInformation("[ScoreNotificationService] Notifying referee score update - Team: {TeamId}, Round: {Round}, Referee: {RefereeId}", 
            teamId, roundNumber, refereeId);
        
        // Notify admin/head referee group via SignalR Hub (for external clients if needed)
        await _hubContext.Clients.Group("headreferee")
            .SendAsync("RefereeScoreUpdated", teamId, roundNumber, refereeId);
        
        // Notify all clients about score update
        await _hubContext.Clients.All
            .SendAsync("ScoreUpdated", teamId, roundNumber);
        
        // Trigger local event for Blazor components in same server instance
        if (OnRefereeScoreUpdated != null)
        {
            await OnRefereeScoreUpdated.Invoke(teamId, roundNumber, refereeId);
        }
    }

    public async Task NotifyScoreApprovalChanged(string teamId, string refereeId)
    {
        _logger.LogInformation("[ScoreNotificationService] Notifying score approval changed - Team: {TeamId}, Referee: {RefereeId}", 
            teamId, refereeId);
        
        // Notify all clients via SignalR Hub
        await _hubContext.Clients.All
            .SendAsync("ScoreApprovalChanged", teamId, refereeId);
        
        // Trigger local event for Blazor components
        if (OnScoreApprovalChanged != null)
        {
            await OnScoreApprovalChanged.Invoke(teamId, refereeId);
        }
    }
}
