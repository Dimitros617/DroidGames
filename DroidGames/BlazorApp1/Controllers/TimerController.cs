using BlazorApp1.Models;
using BlazorApp1.Services;
using Microsoft.AspNetCore.Mvc;

namespace BlazorApp1.Controllers;

[ApiController]
[Route("api/hardware/timer")]
public class TimerController : ControllerBase
{
    private readonly ITimerService _timerService;
    private readonly CompetitionSettings _settings;
    private readonly ILogger<TimerController> _logger;

    public TimerController(
        ITimerService timerService,
        CompetitionSettings settings,
        ILogger<TimerController> logger)
    {
        _timerService = timerService;
        _settings = settings;
        _logger = logger;
    }

    private bool ValidateApiKey()
    {
        if (!Request.Headers.TryGetValue("X-Api-Key", out var apiKey))
        {
            return false;
        }
        return apiKey == _settings.HardwareApiKey;
    }

    [HttpPost("start")]
    public async Task<IActionResult> Start()
    {
        if (!ValidateApiKey())
        {
            return Unauthorized(new { error = "Invalid API key" });
        }

        await _timerService.StartAsync();
        _logger.LogInformation("Timer started via ESP32");

        return Ok(new
        {
            success = true,
            remainingSeconds = await _timerService.GetRemainingSecondsAsync(),
            startedAt = DateTime.UtcNow
        });
    }

    [HttpPost("stop")]
    public async Task<IActionResult> Stop()
    {
        if (!ValidateApiKey())
        {
            return Unauthorized(new { error = "Invalid API key" });
        }

        var elapsed = _settings.RoundDurationSeconds - await _timerService.GetRemainingSecondsAsync();
        await _timerService.StopAsync();
        _logger.LogInformation("Timer stopped via ESP32");

        return Ok(new
        {
            success = true,
            stoppedAt = DateTime.UtcNow,
            elapsedSeconds = elapsed
        });
    }

    [HttpPost("reset")]
    public async Task<IActionResult> Reset()
    {
        if (!ValidateApiKey())
        {
            return Unauthorized(new { error = "Invalid API key" });
        }

        await _timerService.ResetAsync();
        _logger.LogInformation("Timer reset via ESP32");

        return Ok(new
        {
            success = true,
            remainingSeconds = _settings.RoundDurationSeconds
        });
    }

    [HttpGet("status")]
    public async Task<IActionResult> Status()
    {
        if (!ValidateApiKey())
        {
            return Unauthorized(new { error = "Invalid API key" });
        }

        return Ok(new
        {
            status = _timerService.GetStatus().ToString().ToLower(),
            remainingSeconds = await _timerService.GetRemainingSecondsAsync(),
            startedAt = _settings.TimerStartedAt
        });
    }
}
