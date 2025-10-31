using Microsoft.AspNetCore.SignalR;

namespace BlazorApp1.Hubs;

/// <summary>
/// SignalR Hub pro real-time aktualizace stavu soutěže
/// </summary>
public class CompetitionHub : Hub
{
    /// <summary>
    /// Notifikuje všechny klienty o změně aktuálního kola
    /// </summary>
    public async Task NotifyRoundChanged(int newRound)
    {
        Console.WriteLine($"[CompetitionHub] NotifyRoundChanged - New Round: {newRound}");
        await Clients.All.SendAsync("OnRoundChanged", newRound);
    }

    /// <summary>
    /// Notifikuje všechny klienty o změně stavu soutěže
    /// </summary>
    public async Task NotifyCompetitionStatusChanged(string newStatus)
    {
        Console.WriteLine($"[CompetitionHub] NotifyCompetitionStatusChanged - New Status: {newStatus}");
        await Clients.All.SendAsync("OnCompetitionStatusChanged", newStatus);
    }

    /// <summary>
    /// Notifikuje všechny klienty o změně aktuálního týmu
    /// </summary>
    public async Task NotifyCurrentTeamChanged(string? teamId)
    {
        Console.WriteLine($"[CompetitionHub] NotifyCurrentTeamChanged - Team: {teamId ?? "None"}");
        await Clients.All.SendAsync("OnCurrentTeamChanged", teamId);
    }

    /// <summary>
    /// Notifikuje všechny klienty o změně dalšího týmu
    /// </summary>
    public async Task NotifyNextTeamChanged(string? teamId)
    {
        Console.WriteLine($"[CompetitionHub] NotifyNextTeamChanged - Team: {teamId ?? "None"}");
        await Clients.All.SendAsync("OnNextTeamChanged", teamId);
    }

    /// <summary>
    /// Notifikuje všechny klienty o změně pořadí týmů v kole
    /// </summary>
    public async Task NotifyRoundOrderChanged(int roundNumber)
    {
        Console.WriteLine($"[CompetitionHub] NotifyRoundOrderChanged - Round: {roundNumber}");
        await Clients.All.SendAsync("OnRoundOrderChanged", roundNumber);
    }

    /// <summary>
    /// Notifikuje všechny klienty o aktualizaci žebříčku
    /// </summary>
    public async Task NotifyLeaderboardUpdated()
    {
        Console.WriteLine($"[CompetitionHub] NotifyLeaderboardUpdated");
        await Clients.All.SendAsync("OnLeaderboardUpdated");
    }

    /// <summary>
    /// Notifikuje konkrétní tým, že je na řadě
    /// </summary>
    public async Task NotifyTeamTurn(string teamId, string teamName, int position)
    {
        Console.WriteLine($"[CompetitionHub] NotifyTeamTurn - Team: {teamId} ({teamName}), Position: {position}");
        
        // Poslat všem klientům v team_X skupině
        await Clients.Group($"team_{teamId}").SendAsync("OnYourTurn", teamName, position);
        
        // Poslat také všem pro general update
        await Clients.All.SendAsync("OnTeamTurnNotification", teamId, teamName, position);
    }

    /// <summary>
    /// Přihlásit se do skupiny konkrétního týmu
    /// </summary>
    public async Task SubscribeToTeam(string teamId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"team_{teamId}");
        Console.WriteLine($"[CompetitionHub] Client {Context.ConnectionId} subscribed to team_{teamId}");
    }

    /// <summary>
    /// Odhlásit se ze skupiny týmu
    /// </summary>
    public async Task UnsubscribeFromTeam(string teamId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"team_{teamId}");
        Console.WriteLine($"[CompetitionHub] Client {Context.ConnectionId} unsubscribed from team_{teamId}");
    }

    /// <summary>
    /// Přihlásit se jako veřejný klient (dostává všechny aktualizace)
    /// </summary>
    public async Task SubscribeAsPublic()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "public");
        Console.WriteLine($"[CompetitionHub] Client {Context.ConnectionId} subscribed as public");
    }

    public override async Task OnConnectedAsync()
    {
        Console.WriteLine($"[CompetitionHub] Client connected: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine($"[CompetitionHub] Client disconnected: {Context.ConnectionId}");
        await base.OnDisconnectedAsync(exception);
    }
}
