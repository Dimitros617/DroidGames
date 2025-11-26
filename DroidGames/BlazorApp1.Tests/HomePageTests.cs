using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BlazorApp1.Components.Pages;
using BlazorApp1.Data;
using BlazorApp1.Models;
using BlazorApp1.Services;
using Bunit;
using AngleSharp.Dom;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.JSInterop;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BlazorApp1.Tests;

public class HomePageTests : TestContext
{
    private readonly FakeFinalScoreService _finalScores = new();
    private readonly FakeGameStatusService _gameStatus = new();
    private readonly FakeCompetitionNotificationService _competitionNotifications = new();
    private readonly FakeTimerService _timer = new();
    private readonly FakeRepository<CompetitionSettings> _settingsRepo;
    private readonly UserSession _userSession;

    public HomePageTests()
    {
        Services.AddLogging();
        Services.AddDataProtection();

        _settingsRepo = new FakeRepository<CompetitionSettings>(new[]
        {
            new CompetitionSettings
            {
                CurrentRound = 2,
                TotalRounds = 7,
                Status = CompetitionStatus.InProgress,
                GameStatus = GameStatus.Preparation,
                TimerRemainingSeconds = 90
            }
        });

        Services.AddSingleton<IRepository<CompetitionSettings>>(_settingsRepo);
        Services.AddSingleton<IRepository<RoundOrder>>(new FakeRepository<RoundOrder>());
        Services.AddSingleton<ITeamService>(new FakeTeamService());
        Services.AddSingleton<IFinalScoreService>(_finalScores);
        Services.AddSingleton<IScoreNotificationService>(new FakeScoreNotificationService());
        Services.AddSingleton<ITimerService>(_timer);
        Services.AddSingleton<IGameStatusService>(_gameStatus);
        Services.AddSingleton<ICompetitionNotificationService>(_competitionNotifications);
        Services.AddSingleton<NavigationManager>(new TestNavigationManager());

        // ProtectedSessionStorage requires JS interop calls; set them up so UserSession can initialize.
        JSInterop.Setup<ProtectedBrowserStorageResult<User>>("Blazor._internal.protectedBrowserStorage.get", _ => true)
            .SetResult(default);
        JSInterop.SetupVoid("Blazor._internal.protectedBrowserStorage.set", _ => true);
        JSInterop.SetupVoid("Blazor._internal.protectedBrowserStorage.delete", _ => true);
        JSInterop.SetupVoid("sessionStorage.setItem", _ => true);
        JSInterop.SetupVoid("sessionStorage.removeItem", _ => true);
        JSInterop.SetupVoid("sessionStorage.getItem", _ => true);
        Services.AddSingleton<UserSession>(sp => new UserSession(
            new ProtectedSessionStorage(
                sp.GetRequiredService<IJSRuntime>(),
                sp.GetRequiredService<IDataProtectionProvider>())));
        _userSession = Services.GetRequiredService<UserSession>();
    }

    [Fact]
    public void Home_shows_round_status_and_leaderboard_from_services()
    {
        // Ověří, že se po načtení zobrazí údaje o kole/stavu a data z leaderboardu
        _finalScores.SetLeaderboard(new List<LeaderboardEntry>
        {
            new()
            {
                TeamId = "team-1",
                TeamName = "Team Alpha",
                Position = 1,
                TotalScore = 120,
                CompletedRounds = 3
            }
        });

        var cut = RenderComponent<Home>();

        cut.WaitForAssertion(() =>
        {
            var settings = GetPrivate<CompetitionSettings?>(cut.Instance, "_settings");
            Assert.NotNull(settings);
            Assert.Equal(2, settings!.CurrentRound);
            Assert.Equal(CompetitionStatus.InProgress, settings.Status);
        }, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task Home_updates_competition_status_when_event_fires()
    {
        // Ověří, že se UI přepne na nový stav soutěže při přijetí notifikace (bez refresh)
        _finalScores.SetLeaderboard(new List<LeaderboardEntry>());
        var cut = RenderComponent<Home>();

        await _competitionNotifications.RaiseCompetitionStatusChanged(CompetitionStatus.Paused);

        cut.WaitForAssertion(() =>
        {
            var settings = GetPrivate<CompetitionSettings?>(cut.Instance, "_settings");
            Assert.Equal(CompetitionStatus.Paused, settings!.Status);
        }, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Home_shows_timer_value_from_service()
    {
        // Ověří, že se na titulní stránce zobrazí časovač podle hodnot ze služby (když jsou k dispozici aktuální týmy)
        _timer.SetRemaining(45);
        _timer.SetStatus(TimerStatus.Running);
        _finalScores.SetLeaderboard(new List<LeaderboardEntry>());

        var fakeTeams = (FakeTeamService)Services.GetRequiredService<ITeamService>();
        fakeTeams.AddSeed(new Team { Id = "team-a", Name = "Alpha", School = "Test" });
        fakeTeams.AddSeed(new Team { Id = "team-b", Name = "Beta", School = "Test" });
        _settingsRepo.Items[0].CurrentTeamAId = "team-a";
        _settingsRepo.Items[0].CurrentTeamBId = "team-b";

        var cut = RenderComponent<Home>();

        cut.WaitForAssertion(() =>
        {
            var remaining = GetPrivate<int>(cut.Instance, "_timerRemaining");
            Assert.Equal(45, remaining);
        }, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task Home_shows_your_turn_modal_for_matching_team()
    {
        // Ověří, že se modal „JSTE NA ŘADĚ“ zobrazí jen pro přihlášený tým
        SetUserDirectly(_userSession, new User
        {
            Id = "team-1",
            TeamId = "team-1",
            Username = "TeamUser",
            Role = UserRole.Team
        });

        _finalScores.SetLeaderboard(new List<LeaderboardEntry>());
        var cut = RenderComponent<Home>();

        await _competitionNotifications.NotifyYourTurnAsync("team-1", "Team One", 1);

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("JSTE NA", cut.Markup);
            Assert.Contains("Team One", cut.Markup);
        });

        // Nezobrazí se pro jiný tým
        await _competitionNotifications.NotifyYourTurnAsync("team-99", "Other", 2);
        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("Other", cut.Markup);
        });
    }

    [Fact]
    public async Task Home_updates_round_number_on_notification()
    {
        // Ověří, že změna kola přes notifikaci se propíše bez refresh
        _finalScores.SetLeaderboard(new List<LeaderboardEntry>());
        var cut = RenderComponent<Home>();

        await _competitionNotifications.RaiseRoundChanged(3);

        cut.WaitForAssertion(() =>
        {
            var settings = GetPrivate<CompetitionSettings?>(cut.Instance, "_settings");
            Assert.Equal(3, settings!.CurrentRound);
        }, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task Home_updates_current_and_next_teams_on_notifications()
    {
        // Ověří, že změna current/next týmů se projeví v UI
        var teams = (FakeTeamService)Services.GetRequiredService<ITeamService>();
        teams.AddSeed(new Team { Id = "ta", Name = "Team A", School = "School" });
        teams.AddSeed(new Team { Id = "tb", Name = "Team B", School = "School" });
        teams.AddSeed(new Team { Id = "tc", Name = "Team C", School = "School" });
        teams.AddSeed(new Team { Id = "td", Name = "Team D", School = "School" });
        _settingsRepo.Items[0].CurrentTeamAId = "ta";
        _settingsRepo.Items[0].CurrentTeamBId = "tb";
        _settingsRepo.Items[0].NextTeamAId = "tc";
        _settingsRepo.Items[0].NextTeamBId = "td";

        var cut = RenderComponent<Home>();

        await _competitionNotifications.RaiseCurrentTeamsChanged("tb", "tc");
        cut.WaitForAssertion(() =>
        {
            var currentA = GetPrivate<Team?>(cut.Instance, "_currentTeamA");
            var currentB = GetPrivate<Team?>(cut.Instance, "_currentTeamB");
            Assert.Equal("tb", currentA?.Id);
            Assert.Equal("tc", currentB?.Id);
        }, TimeSpan.FromSeconds(2));

        await _competitionNotifications.RaiseNextTeamsChanged("ta", "td");
        cut.WaitForAssertion(() =>
        {
            var nextA = GetPrivate<Team?>(cut.Instance, "_nextTeamA");
            var nextB = GetPrivate<Team?>(cut.Instance, "_nextTeamB");
            Assert.Equal("ta", nextA?.Id);
            Assert.Equal("td", nextB?.Id);
        }, TimeSpan.FromSeconds(2));
    }

    private static void SetUserDirectly(UserSession session, User user)
    {
        typeof(UserSession).GetField("_currentUser", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(session, user);
        typeof(UserSession).GetField("_isInitialized", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(session, true);
    }

    private static T? GetPrivate<T>(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        return (T?)field?.GetValue(instance);
    }
}

internal class FakeFinalScoreService : IFinalScoreService
{
    private List<LeaderboardEntry> _leaderboard = new();

    public void SetLeaderboard(List<LeaderboardEntry> entries) => _leaderboard = entries;

    public Task<List<LeaderboardEntry>> GetDetailedLeaderboardAsync() => Task.FromResult(_leaderboard);

    // Unused in these tests
    public Task<List<FinalRoundScore>> GetAllScoresAsync() => Task.FromResult(new List<FinalRoundScore>());
    public Task<List<FinalRoundScore>> GetTeamScoresAsync(string teamId) => Task.FromResult(new List<FinalRoundScore>());
    public Task<FinalRoundScore?> GetTeamRoundScoreAsync(string teamId, int roundNumber) => Task.FromResult<FinalRoundScore?>(null);
    public Task<FinalRoundScore> SaveFinalScoreAsync(FinalRoundScore score) => Task.FromResult(score);
    public Task<Dictionary<string, int>> GetLeaderboardAsync() => Task.FromResult(new Dictionary<string, int>());
}

internal class FakeTeamService : ITeamService
{
    private readonly List<Team> _teams = new();

    public void AddSeed(Team team) => _teams.Add(team);

    public Task<List<Team>> GetAllTeamsAsync() => Task.FromResult(_teams.ToList());
    public Task<Team?> GetTeamByIdAsync(string id) => Task.FromResult(_teams.FirstOrDefault(t => t.Id == id));
    public Task<Team?> GetTeamByPinAsync(string pin) => Task.FromResult(_teams.FirstOrDefault(t => t.PinCode == pin));
    public Task<Team> AddTeamAsync(Team team)
    {
        _teams.Add(team);
        return Task.FromResult(team);
    }
    public Task<Team> UpdateTeamAsync(Team team)
    {
        var idx = _teams.FindIndex(t => t.Id == team.Id);
        if (idx >= 0) _teams[idx] = team; else _teams.Add(team);
        return Task.FromResult(team);
    }
    public Task<bool> DeleteTeamAsync(string id)
    {
        var removed = _teams.RemoveAll(t => t.Id == id) > 0;
        return Task.FromResult(removed);
    }
    public Task<List<Team>> GetLeaderboardAsync() => Task.FromResult(_teams.OrderByDescending(t => t.TotalScore).ToList());
    public Task UpdateTeamScoresAsync() => Task.CompletedTask;
}

internal class FakeScoreNotificationService : IScoreNotificationService
{
    public event Func<string, int, string, Task>? OnRefereeScoreUpdated;
    public event Func<string, string, Task>? OnScoreApprovalChanged;
    public event Func<string, RoundCompletedNotification, Task>? OnRoundCompleted;
    public event Func<string, AchievementUnlockedNotification, Task>? OnAchievementUnlocked;

    public Task NotifyAchievementUnlocked(string teamId, AchievementUnlockedNotification notification)
        => OnAchievementUnlocked?.Invoke(teamId, notification) ?? Task.CompletedTask;

    public Task NotifyRefereeScoreUpdated(string teamId, int roundNumber, string refereeId)
        => OnRefereeScoreUpdated?.Invoke(teamId, roundNumber, refereeId) ?? Task.CompletedTask;

    public Task NotifyRoundCompleted(string teamId, RoundCompletedNotification notification)
        => OnRoundCompleted?.Invoke(teamId, notification) ?? Task.CompletedTask;

    public Task NotifyScoreApprovalChanged(string teamId, string refereeId)
        => OnScoreApprovalChanged?.Invoke(teamId, refereeId) ?? Task.CompletedTask;
}

internal class FakeTimerService : ITimerService
{
    private int _remainingSeconds = 90;
    private TimerStatus _status = TimerStatus.Stopped;

    public Task StartAsync()
    {
        _status = TimerStatus.Running;
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        _status = TimerStatus.Stopped;
        return Task.CompletedTask;
    }

    public Task ResetAsync()
    {
        _remainingSeconds = 90;
        _status = TimerStatus.Stopped;
        return Task.CompletedTask;
    }

    public Task<int> GetRemainingSecondsAsync() => Task.FromResult(_remainingSeconds);

    public Task NotifyTickAsync(int remainingSeconds)
    {
        _remainingSeconds = remainingSeconds;
        return Task.CompletedTask;
    }

    public TimerStatus GetStatus() => _status;

    public void SetRemaining(int seconds) => _remainingSeconds = seconds;
    public void SetStatus(TimerStatus status) => _status = status;
}

internal class FakeGameStatusService : IGameStatusService
{
    private GameStatus _status = GameStatus.Preparation;
    public event Func<GameStatus, Task>? OnGameStatusChanged;

    public GameStatus GetCurrentStatus() => _status;

    public Task NotifyGameStatusChanged(GameStatus newStatus)
    {
        return OnGameStatusChanged?.Invoke(newStatus) ?? Task.CompletedTask;
    }

    public async Task SetGameStatusAsync(GameStatus status)
    {
        _status = status;
        if (OnGameStatusChanged != null)
        {
            await OnGameStatusChanged.Invoke(status);
        }
    }
}

internal class FakeCompetitionNotificationService : ICompetitionNotificationService
{
    public event Func<int, Task>? OnRoundChanged;
    public event Func<CompetitionStatus, Task>? OnCompetitionStatusChanged;
    public event Func<string?, string?, Task>? OnCurrentTeamsChanged;
    public event Func<string?, string?, Task>? OnNextTeamsChanged;
    public event Func<int, Task>? OnRoundOrderChanged;
    public event Func<Task>? OnLeaderboardUpdated;
    public event Func<string, string, int, Task>? OnYourTurn;

    public Task NotifyCompetitionStatusChangedAsync(CompetitionStatus newStatus)
        => OnCompetitionStatusChanged?.Invoke(newStatus) ?? Task.CompletedTask;
    public Task NotifyCurrentTeamsChangedAsync(string? teamAId, string? teamBId)
        => OnCurrentTeamsChanged?.Invoke(teamAId, teamBId) ?? Task.CompletedTask;
    public Task NotifyLeaderboardUpdatedAsync()
        => OnLeaderboardUpdated?.Invoke() ?? Task.CompletedTask;
    public Task NotifyNextTeamsChangedAsync(string? teamAId, string? teamBId)
        => OnNextTeamsChanged?.Invoke(teamAId, teamBId) ?? Task.CompletedTask;
    public Task NotifyRoundChangedAsync(int newRound)
        => OnRoundChanged?.Invoke(newRound) ?? Task.CompletedTask;
    public Task NotifyRoundOrderChangedAsync(int roundNumber)
        => OnRoundOrderChanged?.Invoke(roundNumber) ?? Task.CompletedTask;
    public Task NotifyYourTurnAsync(string teamId, string teamName, int position)
        => OnYourTurn?.Invoke(teamId, teamName, position) ?? Task.CompletedTask;

    public Task RaiseCompetitionStatusChanged(CompetitionStatus newStatus) => NotifyCompetitionStatusChangedAsync(newStatus);
    public Task RaiseRoundChanged(int round) => NotifyRoundChangedAsync(round);
    public Task RaiseCurrentTeamsChanged(string? a, string? b) => NotifyCurrentTeamsChangedAsync(a, b);
    public Task RaiseNextTeamsChanged(string? a, string? b) => NotifyNextTeamsChangedAsync(a, b);
}

internal class FakeRepository<T> : IRepository<T> where T : class
{
    private readonly List<T> _items;
    public FakeRepository(IEnumerable<T>? seed = null)
    {
        _items = seed?.ToList() ?? new List<T>();
    }

    public List<T> Items => _items;

    public Task<T> AddAsync(T entity)
    {
        _items.Add(entity);
        return Task.FromResult(entity);
    }

    public Task<bool> DeleteAsync(string id)
    {
        var removed = _items.RemoveAll(e => GetId(e) == id) > 0;
        return Task.FromResult(removed);
    }

    public Task<List<T>> GetAllAsync() => Task.FromResult(_items.ToList());

    public Task<T?> GetByIdAsync(string id)
    {
        var entity = _items.FirstOrDefault(e => GetId(e) == id);
        return Task.FromResult(entity);
    }

    public Task SaveAsync() => Task.CompletedTask;

    public Task<T> UpdateAsync(T entity)
    {
        var id = GetId(entity);
        var index = _items.FindIndex(e => GetId(e) == id);
        if (index >= 0)
        {
            _items[index] = entity;
        }
        else
        {
            _items.Add(entity);
        }
        return Task.FromResult(entity);
    }

    public Task UpdateSingletonAsync(T entity)
    {
        _items.Clear();
        _items.Add(entity);
        return Task.CompletedTask;
    }

    private static string? GetId(T entity)
    {
        var prop = typeof(T).GetProperty("Id");
        return prop?.GetValue(entity)?.ToString();
    }
}

internal class TestNavigationManager : NavigationManager
{
    public List<string> Navigations { get; } = new();

    public TestNavigationManager()
    {
        Initialize("http://localhost/", "http://localhost/");
    }

    protected override void NavigateToCore(string uri, bool forceLoad)
    {
        Navigations.Add(uri);
    }
}
