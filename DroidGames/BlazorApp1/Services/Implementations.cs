using System;
using BlazorApp1.Data;
using BlazorApp1.Models;
using Microsoft.AspNetCore.SignalR;
using BlazorApp1.Hubs;

namespace BlazorApp1.Services;

// Service implementations

public class TeamService : ITeamService
{
    private readonly IRepository<Team> _repository;

    public TeamService(IRepository<Team> repository)
    {
        Console.WriteLine("[DEBUG] TeamService created");
        _repository = repository;
    }

    public async Task<List<Team>> GetAllTeamsAsync() => await _repository.GetAllAsync();
    
    public async Task<Team?> GetTeamByIdAsync(string id) => await _repository.GetByIdAsync(id);
    
    public async Task<Team?> GetTeamByPinAsync(string pin)
    {
        var teams = await _repository.GetAllAsync();
        return teams.FirstOrDefault(t => t.PinCode == pin);
    }
    
    public async Task<Team> AddTeamAsync(Team team) => await _repository.AddAsync(team);
    
    public async Task<Team> UpdateTeamAsync(Team team) => await _repository.UpdateAsync(team);
    
    public async Task<bool> DeleteTeamAsync(string id) => await _repository.DeleteAsync(id);
    
    public async Task<List<Team>> GetLeaderboardAsync()
    {
        var teams = await _repository.GetAllAsync();
        return teams.OrderByDescending(t => t.TotalScore).ToList();
    }
    
    public async Task UpdateTeamScoresAsync()
    {
        var teams = await _repository.GetAllAsync();
        foreach (var team in teams)
        {
            team.TotalScore = team.Rides
                .Where(r => r.IsApproved && r.FinalScore.HasValue)
                .Sum(r => r.FinalScore!.Value);
        }
        
        var sorted = teams.OrderByDescending(t => t.TotalScore).ToList();
        for (int i = 0; i < sorted.Count; i++)
        {
            sorted[i].CurrentPosition = i + 1;
            await _repository.UpdateAsync(sorted[i]);
        }
    }
}

public class ScoreService : IScoreService
{
    private readonly IRepository<Team> _teamRepository;

    public ScoreService(IRepository<Team> teamRepository)
    {
        Console.WriteLine("[DEBUG] ScoreService created");
        _teamRepository = teamRepository;
    }

    public async Task SubmitRefereeScoreAsync(string teamId, int roundNumber, RefereeScore score)
    {
        var team = await _teamRepository.GetByIdAsync(teamId);
        if (team == null) return;

        var round = team.Rides.FirstOrDefault(r => r.RoundNumber == roundNumber);
        if (round == null)
        {
            // Vytvoříme nové kolo, pokud neexistuje
            round = new RoundParticipation
            {
                RoundNumber = roundNumber,
                RefereeScores = new Dictionary<string, RefereeScore>()
            };
            team.Rides.Add(round);
        }
        
        round.RefereeScores[score.RefereeId] = score;
        await _teamRepository.UpdateAsync(team);
    }

    public async Task ApproveScoreAsync(string teamId, int roundNumber, int finalScore)
    {
        var team = await _teamRepository.GetByIdAsync(teamId);
        if (team == null) return;

        var round = team.Rides.FirstOrDefault(r => r.RoundNumber == roundNumber);
        if (round != null)
        {
            round.FinalScore = finalScore;
            round.IsApproved = true;
            await _teamRepository.UpdateAsync(team);
        }
    }
}

public class MapService : IMapService
{
    private readonly IRepository<MapConfiguration> _repository;

    public MapService(IRepository<MapConfiguration> repository)
    {
        Console.WriteLine("[DEBUG] MapService created");
        _repository = repository;
    }

    public async Task<List<MapConfiguration>> GetAllMapsAsync() => await _repository.GetAllAsync();
    
    public async Task<MapConfiguration?> GetMapByIdAsync(string id) => await _repository.GetByIdAsync(id);
    
    public async Task<MapConfiguration> SaveMapAsync(MapConfiguration map)
    {
        var existing = await _repository.GetByIdAsync(map.Id);
        return existing != null 
            ? await _repository.UpdateAsync(map) 
            : await _repository.AddAsync(map);
    }
}

public class AchievementService : IAchievementService
{
    private readonly IRepository<Achievement> _achievementRepository;
    private readonly IRepository<Team> _teamRepository;
    private readonly IRepository<TeamAchievement> _teamAchievementRepository;

    public AchievementService(
        IRepository<Achievement> achievementRepository,
        IRepository<Team> teamRepository,
        IRepository<TeamAchievement> teamAchievementRepository)
    {
        Console.WriteLine("[DEBUG] AchievementService created");
        _achievementRepository = achievementRepository;
        _teamRepository = teamRepository;
        _teamAchievementRepository = teamAchievementRepository;
    }

    public async Task<List<Achievement>> GetAllAchievementsAsync() => 
        await _achievementRepository.GetAllAsync();

    public async Task<List<Achievement>> GetUnlockedForTeamAsync(string teamId)
    {
        var teamAchievements = await GetTeamAchievementsAsync(teamId);
        var allAchievements = await _achievementRepository.GetAllAsync();
        
        var unlockedIds = teamAchievements.Select(ta => ta.AchievementId).ToHashSet();
        return allAchievements.Where(a => unlockedIds.Contains(a.Id)).ToList();
    }

    public async Task<List<TeamAchievement>> GetTeamAchievementsAsync(string teamId)
    {
        var all = await _teamAchievementRepository.GetAllAsync();
        return all.Where(ta => ta.TeamId == teamId).ToList();
    }

    public async Task CheckAndUnlockAchievementsAsync(string teamId)
    {
        var team = await _teamRepository.GetByIdAsync(teamId);
        if (team == null) return;

        var allAchievements = await _achievementRepository.GetAllAsync();
        var unlockedAchievements = await GetTeamAchievementsAsync(teamId);
        var unlockedIds = unlockedAchievements.Select(ta => ta.AchievementId).ToHashSet();
        
        foreach (var achievement in allAchievements)
        {
            if (unlockedIds.Contains(achievement.Id)) continue;

            bool shouldUnlock = achievement.Condition.Type switch
            {
                AchievementConditionType.FirstPoints => 
                    team.Rides.Any(r => r.FinalScore > 0),
                _ => false
            };

            if (shouldUnlock)
            {
                await UnlockAchievementAsync(teamId, achievement.Id);
            }
        }
    }

    public async Task<List<Achievement>> CheckQuizAchievementsAsync(string teamId, QuizProgress progress, QuizAttempt lastAttempt)
    {
        var allAchievements = await _achievementRepository.GetAllAsync();
        var unlockedAchievements = await GetTeamAchievementsAsync(teamId);
        var unlockedIds = unlockedAchievements.Select(ta => ta.AchievementId).ToHashSet();
        var newlyUnlocked = new List<Achievement>();

        foreach (var achievement in allAchievements)
        {
            if (unlockedIds.Contains(achievement.Id)) continue;

            bool shouldUnlock = await CheckQuizCondition(achievement, progress, lastAttempt);

            if (shouldUnlock)
            {
                await UnlockAchievementAsync(teamId, achievement.Id, new Dictionary<string, object>
                {
                    ["unlockedVia"] = "quiz",
                    ["questionId"] = lastAttempt.QuestionId,
                    ["timestamp"] = lastAttempt.AttemptedAt
                });
                newlyUnlocked.Add(achievement);
            }
        }

        return newlyUnlocked;
    }

    private Task<bool> CheckQuizCondition(Achievement achievement, QuizProgress progress, QuizAttempt lastAttempt)
    {
        var condition = achievement.Condition;
        
        var result = condition.Type switch
        {
            AchievementConditionType.QuizFirstCorrect => 
                lastAttempt.IsCorrect && progress.CorrectAnswers == 1,
            
            AchievementConditionType.QuizCorrectAnswers => 
                condition.Parameters.TryGetValue("count", out var countObj) && 
                progress.CorrectAnswers >= Convert.ToInt32(countObj),
            
            AchievementConditionType.QuizConsecutiveCorrect => 
                condition.Parameters.TryGetValue("streak", out var streakObj) && 
                GetCurrentStreak(progress) >= Convert.ToInt32(streakObj),
            
            AchievementConditionType.QuizCompleteAll => 
                progress.AnsweredQuestionIds.Count >= 20 && 
                progress.AnsweredQuestionIds.Count == progress.CorrectAnswers,
            
            AchievementConditionType.QuizNoMistakes => 
                progress.IncorrectAnswers == 0 && 
                progress.AnsweredQuestionIds.Count >= 20,
            
            AchievementConditionType.QuizSpeedDemon => 
                lastAttempt.IsCorrect &&
                condition.Parameters.TryGetValue("maxSeconds", out var maxSecsObj) && 
                lastAttempt.TimeToAnswerMs <= Convert.ToInt32(maxSecsObj) * 1000,
            
            AchievementConditionType.QuizMastermind => 
                condition.Parameters.TryGetValue("successRate", out var rateObj) && 
                progress.TotalAttempts >= 10 &&
                progress.SuccessRate >= Convert.ToDouble(rateObj),
            
            AchievementConditionType.QuizNightOwl => 
                lastAttempt.IsCorrect &&
                lastAttempt.AttemptedAt.Hour >= 23 || lastAttempt.AttemptedAt.Hour < 5,
            
            AchievementConditionType.QuizEarlyBird => 
                lastAttempt.IsCorrect &&
                lastAttempt.AttemptedAt.Hour >= 5 && lastAttempt.AttemptedAt.Hour < 7,
            
            _ => false
        };
        
        return Task.FromResult(result);
    }

    private int GetCurrentStreak(QuizProgress progress)
    {
        // Najdeme nejdelší aktuální sérii správných odpovědí
        var allAttempts = progress.Attempts
            .OrderByDescending(a => a.AttemptedAt)
            .ToList();

        int currentStreak = 0;
        foreach (var attempt in allAttempts)
        {
            if (attempt.IsCorrect)
                currentStreak++;
            else
                break;
        }

        return currentStreak;
    }

    private async Task UnlockAchievementAsync(string teamId, string achievementId, Dictionary<string, object>? unlockData = null)
    {
        var teamAchievement = new TeamAchievement
        {
            TeamId = teamId,
            AchievementId = achievementId,
            UnlockedAt = DateTime.UtcNow,
            UnlockData = unlockData ?? new Dictionary<string, object>()
        };

        await _teamAchievementRepository.AddAsync(teamAchievement);
    }
}

public class QuizService : IQuizService
{
    private readonly IRepository<QuizQuestion> _questionRepository;
    private readonly IRepository<QuizProgress> _progressRepository;

    public QuizService(IRepository<QuizQuestion> questionRepository, IRepository<QuizProgress> progressRepository)
    {
        Console.WriteLine("[DEBUG] QuizService created");
        _questionRepository = questionRepository;
        _progressRepository = progressRepository;
    }

    public async Task<List<QuizQuestion>> GetAllQuestionsAsync()
    {
        return await _questionRepository.GetAllAsync();
    }

    public async Task<QuizProgress> GetTeamProgressAsync(string teamId)
    {
        var allProgress = await _progressRepository.GetAllAsync();
        var progress = allProgress.FirstOrDefault(p => p.TeamId == teamId);
        
        if (progress == null)
        {
            progress = new QuizProgress { TeamId = teamId };
            progress = await _progressRepository.AddAsync(progress);
        }
        
        return progress;
    }

    public async Task<QuizAttempt> SubmitAttemptAsync(string teamId, string questionId, int selectedAnswerIndex, int timeToAnswerMs)
    {
        var question = await _questionRepository.GetByIdAsync(questionId);
        if (question == null)
            throw new ArgumentException($"Question {questionId} not found");
        
        var progress = await GetTeamProgressAsync(teamId);
        
        var attempt = new QuizAttempt
        {
            QuestionId = questionId,
            SelectedAnswerIndex = selectedAnswerIndex,
            IsCorrect = selectedAnswerIndex == question.CorrectAnswerIndex,
            TimeToAnswerMs = timeToAnswerMs,
            AttemptedAt = DateTime.Now
        };
        
        progress.Attempts.Add(attempt);
        progress.LastAttemptAt = DateTime.Now;
        
        await _progressRepository.UpdateAsync(progress);
        
        return attempt;
    }

    public async Task<List<QuizQuestionStatus>> GetAllQuestionsWithStatusAsync(string teamId)
    {
        var questions = await GetAllQuestionsAsync();
        var progress = await GetTeamProgressAsync(teamId);
        
        var statuses = new List<QuizQuestionStatus>();
        
        foreach (var question in questions)
        {
            var attempts = progress.Attempts.Where(a => a.QuestionId == question.Id).ToList();
            var hasCorrect = attempts.Any(a => a.IsCorrect);
            
            var status = new QuizQuestionStatus
            {
                Question = question,
                State = hasCorrect ? QuizQuestionState.Correct : 
                       (attempts.Any() ? QuizQuestionState.Incorrect : QuizQuestionState.Unanswered),
                TotalAttempts = attempts.Count,
                CorrectAttempts = attempts.Count(a => a.IsCorrect),
                IncorrectAttempts = attempts.Count(a => !a.IsCorrect),
                LastAttemptAt = attempts.OrderByDescending(a => a.AttemptedAt).FirstOrDefault()?.AttemptedAt
            };
            
            statuses.Add(status);
        }
        
        return statuses;
    }

    public async Task<QuizQuestion?> GetRandomUnansweredQuestionAsync(string teamId)
    {
        var statuses = await GetAllQuestionsWithStatusAsync(teamId);
        var unanswered = statuses.Where(s => s.State == QuizQuestionState.Unanswered).ToList();
        
        if (!unanswered.Any())
            return null;
        
        var random = unanswered[Random.Shared.Next(unanswered.Count)];
        return random.Question;
    }
}

public class ReminderService : IReminderService
{
    private readonly IRepository<Reminder> _repository;

    public ReminderService(IRepository<Reminder> repository)
    {
        Console.WriteLine("[DEBUG] ReminderService created");
        _repository = repository;
    }

    public async Task<List<Reminder>> GetActiveRemindersAsync()
    {
        var all = await _repository.GetAllAsync();
        return all.Where(r => r.IsActive).ToList();
    }

    public async Task<Reminder> AddReminderAsync(Reminder reminder)
    {
        reminder.NextDue = DateTime.UtcNow.AddMinutes(reminder.IntervalMinutes);
        return await _repository.AddAsync(reminder);
    }

    public async Task AcknowledgeReminderAsync(string id)
    {
        var reminder = await _repository.GetByIdAsync(id);
        if (reminder != null)
        {
            reminder.LastTriggered = DateTime.UtcNow;
            reminder.NextDue = DateTime.UtcNow.AddMinutes(reminder.IntervalMinutes);
            await _repository.UpdateAsync(reminder);
        }
    }
}

public class FunFactService : IFunFactService
{
    private readonly IRepository<FunFact> _repository;

    public FunFactService(IRepository<FunFact> repository)
    {
        Console.WriteLine("[DEBUG] FunFactService created");
        _repository = repository;
    }

    public async Task<List<FunFact>> GetAllFactsAsync() => await _repository.GetAllAsync();

    public async Task<FunFact?> GetRandomUnusedFactAsync()
    {
        var facts = await _repository.GetAllAsync();
        var unused = facts.Where(f => !f.IsUsed).ToList();
        return unused.Any() ? unused[Random.Shared.Next(unused.Count)] : null;
    }

    public async Task MarkAsUsedAsync(string id)
    {
        var fact = await _repository.GetByIdAsync(id);
        if (fact != null)
        {
            fact.IsUsed = true;
            fact.UsedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(fact);
        }
    }
}

public class AuthService : IAuthService
{
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Team> _teamRepository;

    public AuthService(IRepository<User> userRepository, IRepository<Team> teamRepository)
    {
        Console.WriteLine("[DEBUG] AuthService created");
        _userRepository = userRepository;
        _teamRepository = teamRepository;
    }

    public async Task<User?> LoginAsync(string username, string password)
    {
        var users = await _userRepository.GetAllAsync();
        var user = users.FirstOrDefault(u => u.Username == username);
        
        if (user != null && VerifyPassword(password, user.PasswordHash))
        {
            return user;
        }
        return null;
    }

    public async Task<User?> LoginWithPinAsync(string pin)
    {
        var teams = await _teamRepository.GetAllAsync();
        var team = teams.FirstOrDefault(t => t.PinCode == pin);
        
        if (team != null)
        {
            return new User
            {
                Id = team.Id,
                Username = team.Name,
                Role = UserRole.Team,
                TeamId = team.Id
            };
        }
        return null;
    }

    public async Task<User> RegisterAsync(string username, string password, UserRole role)
    {
        var user = new User
        {
            Username = username,
            PasswordHash = HashPassword(password),
            Role = role
        };
        return await _userRepository.AddAsync(user);
    }
    
    public async Task<List<User>> GetAllUsersAsync()
    {
        return await _userRepository.GetAllAsync();
    }

    private string HashPassword(string password) => 
        Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password));

    private bool VerifyPassword(string password, string hash) =>
        HashPassword(password) == hash;
}

public class TimerService : ITimerService
{
    private readonly CompetitionSettings _settings;
    private readonly IGameStatusService _gameStatusService;
    private readonly IHubContext<TimerHub> _timerHub;
    private DateTime? _startTime;
    private int _startRemainingSeconds;

    public TimerService(CompetitionSettings settings, IGameStatusService gameStatusService, IHubContext<TimerHub> timerHub)
    {
        Console.WriteLine("[DEBUG] TimerService created");
        _settings = settings;
        _gameStatusService = gameStatusService;
        _timerHub = timerHub;
    }

    public async Task StartAsync()
    {
        var remaining = _settings.TimerRemainingSeconds <= 0
            ? _settings.RoundDurationSeconds
            : _settings.TimerRemainingSeconds;

        _startRemainingSeconds = remaining;
        _startTime = DateTime.UtcNow;
        _settings.TimerStartedAt = _startTime;
        _settings.TimerStatus = TimerStatus.Running;
        _settings.TimerRemainingSeconds = remaining;

        await _gameStatusService.SetGameStatusAsync(GameStatus.RoundInProgress);
        await _timerHub.Clients.All.SendAsync("OnTimerStarted", remaining);
    }

    public async Task StopAsync()
    {
        var remaining = await GetRemainingSecondsAsync();
        _settings.TimerStatus = TimerStatus.Stopped;
        _settings.TimerRemainingSeconds = remaining;
        _settings.TimerStartedAt = null;
        _startTime = null;
        _startRemainingSeconds = remaining;
        await _gameStatusService.SetGameStatusAsync(GameStatus.WaitingForScoring);
        await _timerHub.Clients.All.SendAsync("OnTimerStopped", remaining);
    }

    public async Task ResetAsync()
    {
        _startTime = null;
        _startRemainingSeconds = _settings.RoundDurationSeconds;
        _settings.TimerStatus = TimerStatus.Stopped;
        _settings.TimerRemainingSeconds = _startRemainingSeconds;
        _settings.TimerStartedAt = null;
        await _gameStatusService.SetGameStatusAsync(GameStatus.Preparation);
        await _timerHub.Clients.All.SendAsync("OnTimerReset", _settings.TimerRemainingSeconds);
    }

    public Task<int> GetRemainingSecondsAsync()
    {
        if (_settings.TimerStatus != TimerStatus.Running || !_startTime.HasValue)
        {
            return Task.FromResult(_settings.TimerRemainingSeconds);
        }

        var elapsed = (int)(DateTime.UtcNow - _startTime.Value).TotalSeconds;
        var remaining = Math.Max(0, _startRemainingSeconds - elapsed);
        _settings.TimerRemainingSeconds = remaining;
        return Task.FromResult(remaining);
    }

    public async Task NotifyTickAsync(int remainingSeconds)
    {
        await _timerHub.Clients.All.SendAsync("OnTimerTick", remainingSeconds);
    }

    public TimerStatus GetStatus() => _settings.TimerStatus;
}

public class TimerBackgroundService : BackgroundService
{
    private readonly ITimerService _timerService;
    private readonly ILogger<TimerBackgroundService> _logger;

    public TimerBackgroundService(ITimerService timerService, ILogger<TimerBackgroundService> logger)
    {
        Console.WriteLine("[DEBUG] TimerBackgroundService created");
        _timerService = timerService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_timerService.GetStatus() == TimerStatus.Running)
                {
                    var remaining = await _timerService.GetRemainingSecondsAsync();
                    await _timerService.NotifyTickAsync(remaining);
                    if (remaining <= 0)
                    {
                        await _timerService.StopAsync();
                    }
                }
                await Task.Delay(1000, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in timer background service");
            }
        }
    }
}

public class CompetitionNotificationService : ICompetitionNotificationService
{
    private readonly IHubContext<Hubs.CompetitionHub> _hubContext;
    private readonly ILogger<CompetitionNotificationService> _logger;

    public CompetitionNotificationService(
        IHubContext<Hubs.CompetitionHub> hubContext,
        ILogger<CompetitionNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public event Func<int, Task>? OnRoundChanged;
    public event Func<CompetitionStatus, Task>? OnCompetitionStatusChanged;
    public event Func<string?, string?, Task>? OnCurrentTeamsChanged;
    public event Func<string?, string?, Task>? OnNextTeamsChanged;
    public event Func<int, Task>? OnRoundOrderChanged;
    public event Func<Task>? OnLeaderboardUpdated;
    public event Func<string, string, int, Task>? OnYourTurn;

    public async Task NotifyRoundChangedAsync(int newRound)
    {
        _logger.LogInformation("[CompetitionNotificationService] Round changed to {Round}", newRound);
        
        // Notify all clients via SignalR Hub
        await _hubContext.Clients.All.SendAsync("OnRoundChanged", newRound);
        
        // Trigger local event for Blazor components
        if (OnRoundChanged != null)
        {
            await OnRoundChanged.Invoke(newRound);
        }
    }

    public async Task NotifyCompetitionStatusChangedAsync(CompetitionStatus newStatus)
    {
        _logger.LogInformation("[CompetitionNotificationService] Status changed to {Status}", newStatus);
        
        // Notify all clients via SignalR Hub
        await _hubContext.Clients.All.SendAsync("OnCompetitionStatusChanged", newStatus);
        
        // Trigger local event for Blazor components
        if (OnCompetitionStatusChanged != null)
        {
            await OnCompetitionStatusChanged.Invoke(newStatus);
        }
    }

    public async Task NotifyCurrentTeamsChangedAsync(string? teamAId, string? teamBId)
    {
        _logger.LogInformation("[CompetitionNotificationService] Current teams changed: A={TeamA}, B={TeamB}", teamAId, teamBId);
        
        // Notify all clients via SignalR Hub
        await _hubContext.Clients.All.SendAsync("OnCurrentTeamsChanged", teamAId, teamBId);
        
        // Trigger local event for Blazor components
        if (OnCurrentTeamsChanged != null)
        {
            await OnCurrentTeamsChanged.Invoke(teamAId, teamBId);
        }
    }

    public async Task NotifyNextTeamsChangedAsync(string? teamAId, string? teamBId)
    {
        _logger.LogInformation("[CompetitionNotificationService] Next teams changed: A={TeamA}, B={TeamB}", teamAId, teamBId);
        
        // Notify all clients via SignalR Hub
        await _hubContext.Clients.All.SendAsync("OnNextTeamsChanged", teamAId, teamBId);
        
        // Trigger local event for Blazor components
        if (OnNextTeamsChanged != null)
        {
            await OnNextTeamsChanged.Invoke(teamAId, teamBId);
        }
    }

    public async Task NotifyRoundOrderChangedAsync(int roundNumber)
    {
        _logger.LogInformation("[CompetitionNotificationService] Round order changed for round {Round}", roundNumber);
        
        // Notify all clients via SignalR Hub
        await _hubContext.Clients.All.SendAsync("OnRoundOrderChanged", roundNumber);
        
        // Trigger local event for Blazor components
        if (OnRoundOrderChanged != null)
        {
            await OnRoundOrderChanged.Invoke(roundNumber);
        }
    }

    public async Task NotifyLeaderboardUpdatedAsync()
    {
        _logger.LogInformation("[CompetitionNotificationService] Leaderboard updated");
        
        // Notify all clients via SignalR Hub
        await _hubContext.Clients.All.SendAsync("OnLeaderboardUpdated");
        
        // Trigger local event for Blazor components
        if (OnLeaderboardUpdated != null)
        {
            await OnLeaderboardUpdated.Invoke();
        }
    }

    public async Task NotifyYourTurnAsync(string teamId, string teamName, int position)
    {
        _logger.LogInformation("[CompetitionNotificationService] Your turn: {TeamName} (teamId={TeamId}), position={Position}", 
            teamName, teamId, position);
        
        // Notify specific team via SignalR Hub (group by teamId)
        await _hubContext.Clients.Group($"team_{teamId}").SendAsync("OnYourTurn", teamName, position);
        
        // Trigger local event for Blazor components
        if (OnYourTurn != null)
        {
            await OnYourTurn.Invoke(teamId, teamName, position);
        }
    }
}
