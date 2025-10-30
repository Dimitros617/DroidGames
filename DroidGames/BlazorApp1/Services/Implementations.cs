using System;
using BlazorApp1.Data;
using BlazorApp1.Models;

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
            team.TotalScore = team.Rounds
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

        var round = team.Rounds.FirstOrDefault(r => r.RoundNumber == roundNumber);
        if (round != null)
        {
            round.RefereeScores[score.RefereeId] = score;
            await _teamRepository.UpdateAsync(team);
        }
    }

    public async Task ApproveScoreAsync(string teamId, int roundNumber, int finalScore)
    {
        var team = await _teamRepository.GetByIdAsync(teamId);
        if (team == null) return;

        var round = team.Rounds.FirstOrDefault(r => r.RoundNumber == roundNumber);
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

    public AchievementService(
        IRepository<Achievement> achievementRepository,
        IRepository<Team> teamRepository)
    {
        Console.WriteLine("[DEBUG] AchievementService created");
        _achievementRepository = achievementRepository;
        _teamRepository = teamRepository;
    }

    public async Task<List<Achievement>> GetAllAchievementsAsync() => 
        await _achievementRepository.GetAllAsync();

    public async Task<List<Achievement>> GetUnlockedForTeamAsync(string teamId)
    {
        var team = await _teamRepository.GetByIdAsync(teamId);
        if (team == null) return new List<Achievement>();

        var allAchievements = await _achievementRepository.GetAllAsync();
        return allAchievements
            .Where(a => team.UnlockedAchievements.Contains(a.Id))
            .ToList();
    }

    public async Task CheckAndUnlockAchievementsAsync(string teamId)
    {
        var team = await _teamRepository.GetByIdAsync(teamId);
        if (team == null) return;

        var allAchievements = await _achievementRepository.GetAllAsync();
        
        foreach (var achievement in allAchievements)
        {
            if (team.UnlockedAchievements.Contains(achievement.Id)) continue;

            bool shouldUnlock = achievement.Condition.Type switch
            {
                AchievementConditionType.FirstPoints => 
                    team.Rounds.Any(r => r.FinalScore > 0),
                _ => false
            };

            if (shouldUnlock)
            {
                team.UnlockedAchievements.Add(achievement.Id);
                await _teamRepository.UpdateAsync(team);
            }
        }
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

    private string HashPassword(string password) => 
        Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password));

    private bool VerifyPassword(string password, string hash) =>
        HashPassword(password) == hash;
}

public class TimerService : ITimerService
{
    private readonly CompetitionSettings _settings;
    private DateTime? _startTime;

    public TimerService(CompetitionSettings settings)
    {
        Console.WriteLine("[DEBUG] TimerService created");
        _settings = settings;
    }

    public Task StartAsync()
    {
        _settings.TimerStatus = TimerStatus.Running;
        _settings.TimerStartedAt = DateTime.UtcNow;
        _startTime = DateTime.UtcNow;
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        _settings.TimerStatus = TimerStatus.Stopped;
        return Task.CompletedTask;
    }

    public Task ResetAsync()
    {
        _settings.TimerStatus = TimerStatus.Stopped;
        _settings.TimerRemainingSeconds = _settings.RoundDurationSeconds;
        _settings.TimerStartedAt = null;
        _startTime = null;
        return Task.CompletedTask;
    }

    public Task<int> GetRemainingSecondsAsync()
    {
        if (_settings.TimerStatus != TimerStatus.Running || !_startTime.HasValue)
        {
            return Task.FromResult(_settings.TimerRemainingSeconds);
        }

        var elapsed = (int)(DateTime.UtcNow - _startTime.Value).TotalSeconds;
        var remaining = Math.Max(0, _settings.RoundDurationSeconds - elapsed);
        _settings.TimerRemainingSeconds = remaining;
        return Task.FromResult(remaining);
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
