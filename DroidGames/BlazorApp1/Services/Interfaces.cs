using BlazorApp1.Models;

namespace BlazorApp1.Services;

// Simplified service interfaces for initial implementation

public interface IScoreService
{
    Task SubmitRefereeScoreAsync(string teamId, int roundNumber, RefereeScore score);
    Task ApproveScoreAsync(string teamId, int roundNumber, int finalScore);
}

public interface IMapService
{
    Task<List<MapConfiguration>> GetAllMapsAsync();
    Task<MapConfiguration?> GetMapByIdAsync(string id);
    Task<MapConfiguration> SaveMapAsync(MapConfiguration map);
}

public interface IAchievementService
{
    Task<List<Achievement>> GetAllAchievementsAsync();
    Task<List<Achievement>> GetUnlockedForTeamAsync(string teamId);
    Task CheckAndUnlockAchievementsAsync(string teamId);
}

public interface IQuizService
{
    Task<List<QuizQuestion>> GetQuestionsAsync(int count);
    Task<QuizSession> StartSessionAsync(string userId);
    Task<QuizSession> SubmitAnswerAsync(string sessionId, QuizAnswer answer);
}

public interface IReminderService
{
    Task<List<Reminder>> GetActiveRemindersAsync();
    Task<Reminder> AddReminderAsync(Reminder reminder);
    Task AcknowledgeReminderAsync(string id);
}

public interface IFunFactService
{
    Task<List<FunFact>> GetAllFactsAsync();
    Task<FunFact?> GetRandomUnusedFactAsync();
    Task MarkAsUsedAsync(string id);
}

public interface IAuthService
{
    Task<User?> LoginAsync(string username, string password);
    Task<User?> LoginWithPinAsync(string pin);
    Task<User> RegisterAsync(string username, string password, UserRole role);
}

public interface ITimerService
{
    Task StartAsync();
    Task StopAsync();
    Task ResetAsync();
    Task<int> GetRemainingSecondsAsync();
    TimerStatus GetStatus();
}
