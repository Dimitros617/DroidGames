using BlazorApp1.Models;
using Microsoft.AspNetCore.SignalR;

namespace BlazorApp1.Hubs;

public class ScoreboardHub : Hub
{
    public async Task SubmitRefereeScore(string teamId, int roundNumber, RefereeScore score)
    {
        await Clients.Group("headreferee").SendAsync("OnRefereeScoreReceived", teamId, roundNumber, score);
    }

    public async Task ApproveScore(string teamId, int roundNumber, int finalScore)
    {
        await Clients.All.SendAsync("OnScoreApproved", teamId, roundNumber, finalScore);
        await Clients.All.SendAsync("OnLeaderboardUpdated");
    }
    
    // New method for real-time referee score updates
    public async Task NotifyRefereeScoreUpdated(string teamId, int roundNumber, string refereeId)
    {
        Console.WriteLine($"[ScoreboardHub] NotifyRefereeScoreUpdated - Team: {teamId}, Round: {roundNumber}, Referee: {refereeId}");
        
        // Notify admin/head referee
        await Clients.Group("headreferee").SendAsync("RefereeScoreUpdated", teamId, roundNumber, refereeId);
        
        // Notify all connected clients about score update
        await Clients.All.SendAsync("ScoreUpdated", teamId, roundNumber);
    }
    
    // New method for approval/rejection notifications
    public async Task NotifyScoreApprovalChanged(string teamId, int roundNumber, string refereeId, bool isApproved)
    {
        Console.WriteLine($"[ScoreboardHub] NotifyScoreApprovalChanged - Team: {teamId}, Referee: {refereeId}, Approved: {isApproved}");
        
        // Notify the specific referee
        await Clients.All.SendAsync("ScoreApprovalChanged", teamId, roundNumber, refereeId, isApproved);
    }

    public async Task SubscribeToLeaderboard()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "public");
    }

    public async Task SubscribeAsHeadReferee()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "headreferee");
        Console.WriteLine($"[ScoreboardHub] Client subscribed as head referee: {Context.ConnectionId}");
    }
}
