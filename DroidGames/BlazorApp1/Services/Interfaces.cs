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
    Task<List<TeamAchievement>> GetTeamAchievementsAsync(string teamId);
    Task CheckAndUnlockAchievementsAsync(string teamId);
    Task<List<Achievement>> CheckQuizAchievementsAsync(string teamId, QuizProgress progress, QuizAttempt lastAttempt);
}

public interface IQuizService
{
    Task<List<QuizQuestion>> GetAllQuestionsAsync();
    Task<QuizProgress> GetTeamProgressAsync(string teamId);
    Task<QuizAttempt> SubmitAttemptAsync(string teamId, string questionId, int selectedAnswerIndex, int timeToAnswerMs);
    Task<List<QuizQuestionStatus>> GetAllQuestionsWithStatusAsync(string teamId);
    Task<QuizQuestion?> GetRandomUnansweredQuestionAsync(string teamId);
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

public interface IDiplomaService
{
    Task<byte[]> GenerateDiplomaAsync(string teamId, string memberName, int position);
    Task<byte[]> GenerateBulkDiplomasAsync(string teamId);
    Task<byte[]> GenerateAllTeamsDiplomasAsync();
}
