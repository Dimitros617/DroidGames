using BlazorApp1.Data;
using BlazorApp1.Models;
using BlazorApp1.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace BlazorApp1.Services;

/// <summary>
/// Služba pro finalizaci schválených hodnocení rozhodčích.
/// Oddělena od procesu zadávání a úprav hodnocení.
/// </summary>
public interface IScoreFinalizationService
{
    /// <summary>
    /// Zpracuje schválené hodnocení - vypočítá finální skóre, aktualizuje tým, notifikuje
    /// </summary>
    Task FinalizeApprovedScoreAsync(string teamId, int roundNumber, string approvedByRefereeId);
    
    /// <summary>
    /// Vypočítá finální skóre z hodnocení všech rozhodčích
    /// </summary>
    Task<int> CalculateFinalScoreAsync(string teamId, int roundNumber);
}

public class ScoreFinalizationService : IScoreFinalizationService
{
    private readonly IRepository<Team> _teamRepository;
    private readonly IHubContext<ScoreboardHub> _hubContext;
    private readonly IAchievementEvaluationService _achievementEvaluationService;
    private readonly ILogger<ScoreFinalizationService> _logger;

    public ScoreFinalizationService(
        IRepository<Team> teamRepository,
        IHubContext<ScoreboardHub> hubContext,
        IAchievementEvaluationService achievementEvaluationService,
        ILogger<ScoreFinalizationService> logger)
    {
        _teamRepository = teamRepository;
        _hubContext = hubContext;
        _achievementEvaluationService = achievementEvaluationService;
        _logger = logger;
    }

    public async Task FinalizeApprovedScoreAsync(string teamId, int roundNumber, string approvedByRefereeId)
    {
        _logger.LogInformation("[ScoreFinalization] Starting finalization for Team: {TeamId}, Round: {Round}", 
            teamId, roundNumber);

        var team = await _teamRepository.GetByIdAsync(teamId);
        if (team == null)
        {
            _logger.LogWarning("[ScoreFinalization] Team not found: {TeamId}", teamId);
            return;
        }

        var round = team.Rides.FirstOrDefault(r => r.RoundNumber == roundNumber);
        if (round == null)
        {
            _logger.LogWarning("[ScoreFinalization] Round not found: {Round}", roundNumber);
            return;
        }

        // Zkontrolovat, jestli jsou schválena všechna hodnocení rozhodčích
        var allApproved = round.RefereeScores.Values.All(s => s.IsApproved);
        
        if (allApproved)
        {
            _logger.LogInformation("[ScoreFinalization] All referee scores approved - calculating final score");
            
            // Vypočítat finální skóre
            var finalScore = await CalculateFinalScoreAsync(teamId, roundNumber);
            
            // Uložit finální skóre a označit kolo jako schválené
            round.FinalScore = finalScore;
            round.IsApproved = true;
            // TODO: Add ApprovedAt and ApprovedByRefereeId to RoundParticipation model
            // round.ApprovedAt = DateTime.UtcNow;
            // round.ApprovedByRefereeId = approvedByRefereeId;
            
            // Aktualizovat celkové skóre týmu
            team.TotalScore = team.Rides
                .Where(r => r.IsApproved && r.FinalScore.HasValue)
                .Sum(r => r.FinalScore!.Value);
            
            await _teamRepository.UpdateAsync(team);
            
            _logger.LogInformation("[ScoreFinalization] Final score: {Score}, Total score: {Total}", 
                finalScore, team.TotalScore);
            
            // Vyhodnotit achievementy na základě událostí v tomto kole
            await _achievementEvaluationService.EvaluateRoundAchievementsAsync(teamId, roundNumber);
            
            // Notifikovat tým o schváleném výsledku přes WebSocket
            await _hubContext.Clients.Group($"team_{teamId}")
                .SendAsync("OnRoundScoreApproved", roundNumber, finalScore, team.TotalScore);
            
            // Aktualizovat leaderboard pro všechny
            await _hubContext.Clients.All
                .SendAsync("OnLeaderboardUpdated");
            
            _logger.LogInformation("[ScoreFinalization] Finalization complete for Team: {TeamId}", teamId);
        }
        else
        {
            _logger.LogInformation("[ScoreFinalization] Not all referee scores approved yet - waiting");
        }
    }

    public async Task<int> CalculateFinalScoreAsync(string teamId, int roundNumber)
    {
        var team = await _teamRepository.GetByIdAsync(teamId);
        if (team == null) return 0;

        var round = team.Rides.FirstOrDefault(r => r.RoundNumber == roundNumber);
        if (round == null) return 0;

        var approvedScores = round.RefereeScores.Values
            .Where(s => s.IsApproved)
            .Select(s => s.TotalScore)
            .ToList();

        if (approvedScores.Count == 0) return 0;

        // Strategie: průměr z hodnocení všech rozhodčích
        // Lze změnit na median, minimum, maximum atd.
        var finalScore = (int)Math.Round(approvedScores.Average());
        
        _logger.LogInformation("[ScoreFinalization] Calculated final score: {Score} from {Count} referee scores", 
            finalScore, approvedScores.Count);

        return finalScore;
    }
}
