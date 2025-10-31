using BlazorApp1.Data;
using BlazorApp1.Hubs;
using BlazorApp1.Models;
using Microsoft.AspNetCore.SignalR;

namespace BlazorApp1.Services;

public interface IGameStatusService
{
    Task SetGameStatusAsync(GameStatus status);
    GameStatus GetCurrentStatus();
    Task NotifyGameStatusChanged(GameStatus newStatus);
    
    // Event for Blazor components to subscribe to
    event Func<GameStatus, Task>? OnGameStatusChanged;
}

public class GameStatusService : IGameStatusService
{
    private readonly IHubContext<ScoreboardHub> _hubContext;
    private readonly CompetitionSettings _settings;
    private readonly IRepository<CompetitionSettings> _settingsRepository;
    private readonly ILogger<GameStatusService> _logger;

    public GameStatusService(
        IHubContext<ScoreboardHub> hubContext,
        CompetitionSettings settings,
        IRepository<CompetitionSettings> settingsRepository,
        ILogger<GameStatusService> logger)
    {
        _hubContext = hubContext;
        _settings = settings;
        _settingsRepository = settingsRepository;
        _logger = logger;
    }

    public event Func<GameStatus, Task>? OnGameStatusChanged;

    public async Task SetGameStatusAsync(GameStatus status)
    {
        var oldStatus = _settings.GameStatus;
        
        if (oldStatus == status)
        {
            _logger.LogDebug("[GameStatusService] Status unchanged: {Status}", status);
            return;
        }
        
        _logger.LogInformation("[GameStatusService] Changing game status: {OldStatus} → {NewStatus}", 
            oldStatus, status);
        
        _settings.GameStatus = status;
        
        // Persist to database
        var allSettings = await _settingsRepository.GetAllAsync();
        var settingsEntity = allSettings.FirstOrDefault();
        
        if (settingsEntity != null)
        {
            settingsEntity.GameStatus = status;
            await _settingsRepository.UpdateAsync(settingsEntity);
        }
        
        // Notify all clients via WebSocket
        await NotifyGameStatusChanged(status);
    }

    public GameStatus GetCurrentStatus()
    {
        return _settings.GameStatus;
    }

    public async Task NotifyGameStatusChanged(GameStatus newStatus)
    {
        _logger.LogInformation("[GameStatusService] Broadcasting game status: {Status}", newStatus);
        
        // Notify ALL clients via SignalR Hub
        await _hubContext.Clients.All
            .SendAsync("GameStatusChanged", newStatus.ToString());
        
        // Trigger local event for Blazor components in same server instance
        if (OnGameStatusChanged != null)
        {
            await OnGameStatusChanged.Invoke(newStatus);
        }
    }
}
