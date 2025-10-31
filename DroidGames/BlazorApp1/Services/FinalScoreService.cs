using BlazorApp1.Data;
using BlazorApp1.Models;

namespace BlazorApp1.Services;

public class FinalScoreService : IFinalScoreService
{
    private readonly IRepository<FinalRoundScore> _repository;
    private readonly IRepository<Team> _teamRepository;
    private readonly ILogger<FinalScoreService> _logger;

    public FinalScoreService(
        IRepository<FinalRoundScore> repository,
        IRepository<Team> teamRepository,
        ILogger<FinalScoreService> logger)
    {
        _repository = repository;
        _teamRepository = teamRepository;
        _logger = logger;
    }

    public async Task<List<FinalRoundScore>> GetAllScoresAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<List<FinalRoundScore>> GetTeamScoresAsync(string teamId)
    {
        var all = await _repository.GetAllAsync();
        return all.Where(s => s.TeamId == teamId)
                  .OrderBy(s => s.RoundNumber)
                  .ToList();
    }

    public async Task<FinalRoundScore?> GetTeamRoundScoreAsync(string teamId, int roundNumber)
    {
        var all = await _repository.GetAllAsync();
        return all.FirstOrDefault(s => s.TeamId == teamId && s.RoundNumber == roundNumber);
    }

    public async Task<FinalRoundScore> SaveFinalScoreAsync(FinalRoundScore score)
    {
        _logger.LogInformation(
            "[FinalScoreService] Saving final score: Team={TeamId}, Round={Round}, Total={Total}",
            score.TeamId, score.RoundNumber, score.TotalScore);

        // Check if already exists
        var existing = await GetTeamRoundScoreAsync(score.TeamId, score.RoundNumber);
        
        if (existing != null)
        {
            // Update existing
            _logger.LogWarning(
                "[FinalScoreService] Overwriting existing score for Team={TeamId}, Round={Round}",
                score.TeamId, score.RoundNumber);
            
            await _repository.DeleteAsync(existing.Id);
        }

        return await _repository.AddAsync(score);
    }

    public async Task<Dictionary<string, int>> GetLeaderboardAsync()
    {
        var allScores = await GetAllScoresAsync();
        
        return allScores
            .GroupBy(s => s.TeamId)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(s => s.TotalScore)
            );
    }

    public async Task<List<LeaderboardEntry>> GetDetailedLeaderboardAsync()
    {
        var allScores = await GetAllScoresAsync();
        var teams = await _teamRepository.GetAllAsync();

        var leaderboard = allScores
            .GroupBy(s => s.TeamId)
            .Select(g =>
            {
                var team = teams.FirstOrDefault(t => t.Id == g.Key);
                return new LeaderboardEntry
                {
                    TeamId = g.Key,
                    TeamName = team?.Name ?? "Neznámý tým",
                    TotalScore = g.Sum(s => s.TotalScore),
                    CompletedRounds = g.Count(),
                    RoundScores = g.OrderBy(s => s.RoundNumber).ToList()
                };
            })
            .OrderByDescending(e => e.TotalScore)
            .ThenBy(e => e.TeamName)
            .ToList();

        // Přiřadit pozice
        for (int i = 0; i < leaderboard.Count; i++)
        {
            leaderboard[i].Position = i + 1;
            leaderboard[i].PreviousPosition = i + 1; // Při načtení je stejná
        }

        return leaderboard;
    }
}
