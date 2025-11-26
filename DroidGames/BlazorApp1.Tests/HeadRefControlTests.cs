using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using BlazorApp1.Components.Pages.HeadReferee;
using BlazorApp1.Data;
using BlazorApp1.Models;
using BlazorApp1.Services;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System;
using Xunit;

namespace BlazorApp1.Tests;

public class HeadRefControlTests : TestContext
{
    private readonly CaptureCompetitionNotifications _notifications = new();
    private readonly FakeRepo<CompetitionSettings> _settingsRepo;
    private readonly FakeRepo<Team> _teamsRepo;

    public HeadRefControlTests()
    {
        _settingsRepo = new FakeRepo<CompetitionSettings>(new[]
        {
            new CompetitionSettings
            {
                CurrentRound = 1,
                TotalRounds = 5,
                Status = CompetitionStatus.NotStarted
            }
        });
        _teamsRepo = new FakeRepo<Team>(new List<Team>());

        Services.AddSingleton<IRepository<CompetitionSettings>>(_settingsRepo);
        Services.AddSingleton<IRepository<Team>>(_teamsRepo);
        Services.AddSingleton<ICompetitionNotificationService>(_notifications);
        Services.AddSingleton<NavigationManager>(new TestNavigationManager());
    }

    [Fact]
    public async Task Control_sends_status_notification_on_change()
    {
        // Ověří, že změna stavu v /headref/control vyvolá notifikaci
        var cut = RenderComponent<Control>();

        var settings = GetPrivate<CompetitionSettings?>(cut.Instance, "_settings");
        Assert.NotNull(settings);
        settings!.Status = CompetitionStatus.InProgress;

        InvokePrivate(cut.Instance, "OnStatusChanged");

        Assert.Equal(CompetitionStatus.InProgress, _notifications.LastStatus);
    }

    [Fact]
    public async Task Control_sends_round_notification_on_change()
    {
        // Ověří, že změna čísla kola v /headref/control vyvolá notifikaci
        var cut = RenderComponent<Control>();

        var settings = GetPrivate<CompetitionSettings?>(cut.Instance, "_settings");
        Assert.NotNull(settings);
        settings!.CurrentRound = 4;

        InvokePrivate(cut.Instance, "OnRoundChanged");

        Assert.Equal(4, _notifications.LastRound);
    }

    private static T? GetPrivate<T>(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        return (T?)field?.GetValue(instance);
    }

    private static void InvokePrivate(object instance, string methodName)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        method?.Invoke(instance, null);
    }
}

internal class CaptureCompetitionNotifications : ICompetitionNotificationService
{
    public event Func<int, Task>? OnRoundChanged { add { } remove { } }
    public event Func<CompetitionStatus, Task>? OnCompetitionStatusChanged { add { } remove { } }
    public event Func<string?, string?, Task>? OnCurrentTeamsChanged { add { } remove { } }
    public event Func<string?, string?, Task>? OnNextTeamsChanged { add { } remove { } }
    public event Func<int, Task>? OnRoundOrderChanged { add { } remove { } }
    public event Func<Task>? OnLeaderboardUpdated { add { } remove { } }
    public event Func<string, string, int, Task>? OnYourTurn { add { } remove { } }

    public int? LastRound { get; private set; }
    public CompetitionStatus? LastStatus { get; private set; }

    public Task NotifyRoundChangedAsync(int newRound)
    {
        LastRound = newRound;
        return Task.CompletedTask;
    }

    public Task NotifyCompetitionStatusChangedAsync(CompetitionStatus newStatus)
    {
        LastStatus = newStatus;
        return Task.CompletedTask;
    }

    public Task NotifyCurrentTeamsChangedAsync(string? teamAId, string? teamBId)
        => Task.CompletedTask;

    public Task NotifyNextTeamsChangedAsync(string? teamAId, string? teamBId)
        => Task.CompletedTask;

    public Task NotifyRoundOrderChangedAsync(int roundNumber)
        => Task.CompletedTask;

    public Task NotifyLeaderboardUpdatedAsync()
        => Task.CompletedTask;

    public Task NotifyYourTurnAsync(string teamId, string teamName, int position)
        => Task.CompletedTask;
}

internal class FakeRepo<T> : IRepository<T> where T : class
{
    private readonly List<T> _items;

    public FakeRepo(IEnumerable<T>? seed = null)
    {
        _items = seed?.ToList() ?? new List<T>();
    }

    public Task<List<T>> GetAllAsync() => Task.FromResult(_items.ToList());

    public Task<T?> GetByIdAsync(string id)
    {
        var prop = typeof(T).GetProperty("Id");
        var match = _items.FirstOrDefault(x => prop?.GetValue(x)?.ToString() == id);
        return Task.FromResult(match);
    }

    public Task<T> AddAsync(T entity)
    {
        _items.Add(entity);
        return Task.FromResult(entity);
    }

    public Task<T> UpdateAsync(T entity)
    {
        var prop = typeof(T).GetProperty("Id");
        var id = prop?.GetValue(entity)?.ToString();
        var idx = _items.FindIndex(x => prop?.GetValue(x)?.ToString() == id);
        if (idx >= 0) _items[idx] = entity; else _items.Add(entity);
        return Task.FromResult(entity);
    }

    public Task<bool> DeleteAsync(string id)
    {
        var prop = typeof(T).GetProperty("Id");
        var removed = _items.RemoveAll(x => prop?.GetValue(x)?.ToString() == id) > 0;
        return Task.FromResult(removed);
    }

    public Task SaveAsync() => Task.CompletedTask;
    public Task UpdateSingletonAsync(T entity)
    {
        _items.Clear();
        _items.Add(entity);
        return Task.CompletedTask;
    }
}
