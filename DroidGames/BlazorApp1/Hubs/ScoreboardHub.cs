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

    public async Task SubscribeToLeaderboard()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "public");
    }

    public async Task SubscribeAsHeadReferee()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "headreferee");
    }
}
