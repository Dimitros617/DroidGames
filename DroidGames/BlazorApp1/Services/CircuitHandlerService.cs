using Microsoft.AspNetCore.Components.Server.Circuits;

namespace BlazorApp1.Services;

/// <summary>
/// Monitoruje lifecycle WebSocket spojení (Blazor Circuits).
/// Zajišťuje cleanup resources při odpojení klienta.
/// </summary>
public class CircuitHandlerService : CircuitHandler
{
    private readonly ILogger<CircuitHandlerService> _logger;

    public CircuitHandlerService(ILogger<CircuitHandlerService> logger)
    {
        _logger = logger;
    }

    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"[Circuit {circuit.Id}] Connection established");
        return Task.CompletedTask;
    }

    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _logger.LogWarning($"[Circuit {circuit.Id}] Connection lost - cleaning up");
        
        // Zde můžete přidat vlastní cleanup logiku:
        // - Odhlásit uživatele z aktivních sessions
        // - Uvolnit resources
        // - Logovat aktivitu
        
        return Task.CompletedTask;
    }

    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"[Circuit {circuit.Id}] Circuit opened (new session started)");
        return Task.CompletedTask;
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"[Circuit {circuit.Id}] Circuit closed (session ended)");
        
        // Circuit je uzavřen - uživatel odešel nebo reload stránky
        // Dispose se zavolá automaticky na všechny Scoped services
        
        return Task.CompletedTask;
    }
}
