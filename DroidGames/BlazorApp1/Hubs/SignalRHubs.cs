using Microsoft.AspNetCore.SignalR;

namespace BlazorApp1.Hubs;

public class TimerHub : Hub
{
    public async Task StartTimer()
    {
        await Clients.All.SendAsync("OnTimerStarted");
    }

    public async Task StopTimer()
    {
        await Clients.All.SendAsync("OnTimerStopped");
    }

    public async Task ResetTimer()
    {
        await Clients.All.SendAsync("OnTimerReset");
    }

    public async Task TimerTick(int remainingSeconds)
    {
        await Clients.All.SendAsync("OnTimerTick", remainingSeconds);
    }
}

public class NotificationHub : Hub
{
    public async Task SubscribeTeam(string teamId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"team_{teamId}");
    }

    public async Task SendTeamNotification(string teamId, string message)
    {
        await Clients.Group($"team_{teamId}").SendAsync("OnNotification", message);
    }

    public async Task SendAchievementUnlocked(string teamId, string achievementId)
    {
        await Clients.Group($"team_{teamId}").SendAsync("OnAchievementUnlocked", achievementId);
    }
}

public class ProductionHub : Hub
{
    public async Task RequestCamera(int cameraId)
    {
        await Clients.Group("production").SendAsync("OnCameraRequested", cameraId);
    }

    public async Task RequestGraphic(string graphicId)
    {
        await Clients.Group("production").SendAsync("OnGraphicRequested", graphicId);
    }

    public async Task AcknowledgeRequest(string requestType, string requestId)
    {
        await Clients.Group("headreferee").SendAsync("OnRequestAcknowledged", requestType, requestId);
    }

    public async Task SubscribeAsProduction()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "production");
    }

    public async Task SubscribeAsHeadReferee()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "headreferee");
    }
}
