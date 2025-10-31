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
    Task<List<User>> GetAllUsersAsync();
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

public interface IFinalScoreService
{
    Task<List<FinalRoundScore>> GetAllScoresAsync();
    Task<List<FinalRoundScore>> GetTeamScoresAsync(string teamId);
    Task<FinalRoundScore?> GetTeamRoundScoreAsync(string teamId, int roundNumber);
    Task<FinalRoundScore> SaveFinalScoreAsync(FinalRoundScore score);
    Task<Dictionary<string, int>> GetLeaderboardAsync();
    Task<List<LeaderboardEntry>> GetDetailedLeaderboardAsync();
}

public interface ICompetitionNotificationService
{
    Task NotifyRoundChangedAsync(int newRound);
    Task NotifyCompetitionStatusChangedAsync(CompetitionStatus newStatus);
    Task NotifyCurrentTeamsChangedAsync(string? teamAId, string? teamBId);
    Task NotifyNextTeamsChangedAsync(string? teamAId, string? teamBId);
    Task NotifyRoundOrderChangedAsync(int roundNumber);
    Task NotifyLeaderboardUpdatedAsync();
    Task NotifyYourTurnAsync(string teamId, string teamName, int position);
    
    // Events for Blazor components to subscribe to
    event Func<int, Task>? OnRoundChanged;
    event Func<CompetitionStatus, Task>? OnCompetitionStatusChanged;
    event Func<string?, string?, Task>? OnCurrentTeamsChanged;
    event Func<string?, string?, Task>? OnNextTeamsChanged;
    event Func<int, Task>? OnRoundOrderChanged;
    event Func<Task>? OnLeaderboardUpdated;
    event Func<string, string, int, Task>? OnYourTurn;
}
