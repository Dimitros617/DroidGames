using BlazorApp1.Data;
using BlazorApp1.Models;
using BlazorApp1.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace BlazorApp1.Services;

/// <summary>
/// Služba pro vyhodnocování achievementů na základě logů rozhodčích.
/// Oddělena od hodnocení pro čistší strukturu kódu.
/// </summary>
public interface IAchievementEvaluationService
{
    /// <summary>
    /// Vyhodnotí achievementy pro dané kolo na základě událostí od rozhodčích
    /// </summary>
    Task<List<Achievement>> EvaluateRoundAchievementsAsync(string teamId, int roundNumber);
    
    /// <summary>
    /// Vyhodnotí specifický typ achievementu
    /// </summary>
    Task<bool> CheckAchievementAsync(string teamId, string achievementId);
}

public class AchievementEvaluationService : IAchievementEvaluationService
{
    private readonly IRepository<Team> _teamRepository;
    private readonly IRepository<Achievement> _achievementRepository;
    private readonly IRepository<TeamAchievement> _teamAchievementRepository;
    private readonly IRepository<MapConfiguration> _mapRepository;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<AchievementEvaluationService> _logger;

    public AchievementEvaluationService(
        IRepository<Team> teamRepository,
        IRepository<Achievement> achievementRepository,
        IRepository<TeamAchievement> teamAchievementRepository,
        IRepository<MapConfiguration> mapRepository,
        IHubContext<NotificationHub> hubContext,
        ILogger<AchievementEvaluationService> logger)
    {
        _teamRepository = teamRepository;
        _achievementRepository = achievementRepository;
        _teamAchievementRepository = teamAchievementRepository;
        _mapRepository = mapRepository;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task<List<Achievement>> EvaluateRoundAchievementsAsync(string teamId, int roundNumber)
    {
        _logger.LogInformation("[AchievementEvaluation] Evaluating achievements for Team: {TeamId}, Round: {Round}", 
            teamId, roundNumber);

        var unlockedAchievements = new List<Achievement>();
        
        var team = await _teamRepository.GetByIdAsync(teamId);
        if (team == null) return unlockedAchievements;

        var round = team.Rides.FirstOrDefault(r => r.RoundNumber == roundNumber);
        if (round == null) return unlockedAchievements;

        // Získat všechny události z hodnocení rozhodčích
        var allEvents = round.RefereeScores.Values
            .Where(s => s.IsApproved)
            .SelectMany(s => s.Events ?? new List<ScoringEventData>())
            .ToList();

        _logger.LogInformation("[AchievementEvaluation] Processing {Count} events", allEvents.Count);

        // Vyhodnotit jednotlivé typy achievementů a sbírat odemčené
        var firstCrystal = await CheckFirstCrystalTouch(teamId, allEvents);
        if (firstCrystal != null) unlockedAchievements.Add(firstCrystal);
        
        var firstSulfur = await CheckFirstSulfurHit(teamId, allEvents);
        if (firstSulfur != null) unlockedAchievements.Add(firstSulfur);
        
        var threeCrystals = await CheckThreeCrystalsInRow(teamId, allEvents);
        if (threeCrystals != null) unlockedAchievements.Add(threeCrystals);
        
        var allCrystals = await CheckAllCrystalsTouched(teamId, round, allEvents);
        if (allCrystals != null) unlockedAchievements.Add(allCrystals);
        
        var allSulfurs = await CheckAllSulfursCleared(teamId, round, allEvents);
        if (allSulfurs != null) unlockedAchievements.Add(allSulfurs);
        
        var perfectRun = await CheckPerfectRun(teamId, allEvents);
        if (perfectRun != null) unlockedAchievements.Add(perfectRun);
        
        var speedDemon = await CheckSpeedDemon(teamId, round, allEvents);
        if (speedDemon != null) unlockedAchievements.Add(speedDemon);
        
        var minimalMoves = await CheckMinimalMoves(teamId, allEvents);
        if (minimalMoves != null) unlockedAchievements.Add(minimalMoves);
        
        var noSulfur = await CheckNoSulfurDamage(teamId, allEvents);
        if (noSulfur != null) unlockedAchievements.Add(noSulfur);
        
        var crystalMaster = await CheckCrystalMaster(teamId, allEvents);
        if (crystalMaster != null) unlockedAchievements.Add(crystalMaster);
        
        _logger.LogInformation("[AchievementEvaluation] Evaluation complete for Team: {TeamId}, Unlocked: {Count}", 
            teamId, unlockedAchievements.Count);
        
        return unlockedAchievements;
    }

    #region Achievement Checks

    /// <summary>
    /// První dotyk krystalu v celé soutěži
    /// </summary>
    private async Task<Achievement?> CheckFirstCrystalTouch(string teamId, List<ScoringEventData> events)
    {
        const string achievementId = "first-crystal-touch";
        
        // Zkontrolovat, jestli achievement už někdo nemá
        var existingAchievements = await _teamAchievementRepository.GetAllAsync();
        if (existingAchievements.Any(a => a.AchievementId == achievementId))
        {
            return null; // Už ho někdo má
        }

        // Zkontrolovat, jestli má tým alespoň jeden dotyk krystalu
        var hasCrystalTouch = events.Any(e => 
            e.Description.Contains("Dotyk krystalu") || 
            e.BlockType == "BlueCrystal");

        if (hasCrystalTouch)
        {
            return await UnlockAchievement(teamId, achievementId, "První dotyk krystalu v soutěži!");
        }
        
        return null;
    }

    /// <summary>
    /// První narušení síry v celé soutěži
    /// </summary>
    private async Task<Achievement?> CheckFirstSulfurHit(string teamId, List<ScoringEventData> events)
    {
        const string achievementId = "first-sulfur-hit";
        
        var existingAchievements = await _teamAchievementRepository.GetAllAsync();
        if (existingAchievements.Any(a => a.AchievementId == achievementId))
        {
            return null;
        }

        var hasSulfurHit = events.Any(e => 
            e.Description.Contains("Narušení síry") || 
            e.BlockType == "YellowSulfur");

        if (hasSulfurHit)
        {
            return await UnlockAchievement(teamId, achievementId, "První narušení síry v soutěži!");
        }
        
        return null;
    }

    /// <summary>
    /// 3 krystaly dotknuté za sebou (bez narušení síry mezi nimi)
    /// </summary>
    private async Task<Achievement?> CheckThreeCrystalsInRow(string teamId, List<ScoringEventData> events)
    {
        const string achievementId = "three-crystals-row";
        
        var orderedEvents = events.OrderBy(e => e.Timestamp).ToList();
        int consecutiveCrystals = 0;
        
        foreach (var evt in orderedEvents)
        {
            if (evt.Description.Contains("Dotyk krystalu") || evt.BlockType == "BlueCrystal")
            {
                consecutiveCrystals++;
                if (consecutiveCrystals >= 3)
                {
                    return await UnlockAchievement(teamId, achievementId, "3 krystaly dotknuté za sebou!");
                }
            }
            else if (evt.Description.Contains("Narušení síry") || evt.BlockType == "YellowSulfur")
            {
                consecutiveCrystals = 0; // Reset při narušení síry
            }
        }
        
        return null;
    }

    /// <summary>
    /// Dotknutí se všech krystalů v dané konfiguraci mapy
    /// </summary>
    private async Task<Achievement?> CheckAllCrystalsTouched(string teamId, RoundParticipation round, List<ScoringEventData> events)
    {
        const string achievementId = "all-crystals-touched";
        
        var map = await _mapRepository.GetByIdAsync(round.MapConfigurationId);
        if (map == null) return null;

        // TODO: Fix after implementing grid access methods on MapConfiguration
        // Spočítat všechny krystaly na mapě
        /* 
        int totalCrystals = 0;
        for (int y = 0; y < map.Height; y++)
        {
            for (int x = 0; x < map.Width; x++)
            {
                var block = map.GetBlock(x, y);
                if (block?.BlockType == MapBlockType.BlueCrystal)
                {
                    totalCrystals++;
                }
            }
        }
        */
        
        // Temporary implementation using Blocks list
        int totalCrystals = map.Blocks.Count(b => b.Type == MapBlockType.BlueCrystal);

        // Spočítat unikátní pozice krystalů, kterých se dotkli
        var touchedCrystals = events
            .Where(e => e.Description.Contains("Dotyk krystalu") || e.BlockType == "BlueCrystal")
            .Select(e => $"{e.X},{e.Y}")
            .Distinct()
            .Count();

        if (touchedCrystals >= totalCrystals && totalCrystals > 0)
        {
            return await UnlockAchievement(teamId, achievementId, $"Dotknutí všech {totalCrystals} krystalů!");
        }
        
        return null;
    }

    /// <summary>
    /// Odsunuti všech sír v dané konfiguraci mapy
    /// </summary>
    private async Task<Achievement?> CheckAllSulfursCleared(string teamId, RoundParticipation round, List<ScoringEventData> events)
    {
        const string achievementId = "all-sulfurs-cleared";
        
        var map = await _mapRepository.GetByIdAsync(round.MapConfigurationId);
        if (map == null) return null;

        // TODO: Fix after implementing grid access methods on MapConfiguration
        // Spočítat všechny síry na mapě
        /*
        int totalSulfurs = 0;
        for (int y = 0; y < map.Height; y++)
        {
            for (int x = 0; x < map.Width; x++)
            {
                var block = map.GetBlock(x, y);
                if (block?.BlockType == MapBlockType.YellowSulfur)
                {
                    totalSulfurs++;
                }
            }
        }
        */
        
        // Temporary implementation using Blocks list
        int totalSulfurs = map.Blocks.Count(b => b.Type == MapBlockType.YellowSulfur);

        // Spočítat unikátní pozice sír, kterých se dotkli
        var touchedSulfurs = events
            .Where(e => e.Description.Contains("Narušení síry") || e.BlockType == "YellowSulfur")
            .Select(e => $"{e.X},{e.Y}")
            .Distinct()
            .Count();

        if (touchedSulfurs >= totalSulfurs && totalSulfurs > 0)
        {
            return await UnlockAchievement(teamId, achievementId, $"Narušení všech {totalSulfurs} sír!");
        }
        
        return null;
    }

    /// <summary>
    /// Perfektní jízda - pouze krystaly, žádná síra
    /// </summary>
    private async Task<Achievement?> CheckPerfectRun(string teamId, List<ScoringEventData> events)
    {
        const string achievementId = "perfect-run";
        
        var hasCrystals = events.Any(e => e.Description.Contains("Dotyk krystalu"));
        var hasSulfur = events.Any(e => e.Description.Contains("Narušení síry"));
        
        if (hasCrystals && !hasSulfur)
        {
            return await UnlockAchievement(teamId, achievementId, "Perfektní jízda - žádná síra!");
        }
        
        return null;
    }

    /// <summary>
    /// Rychlý démon - kolo dokončeno rychle (méně než 10 událostí)
    /// </summary>
    private async Task<Achievement?> CheckSpeedDemon(string teamId, RoundParticipation round, List<ScoringEventData> events)
    {
        const string achievementId = "speed-demon";
        
        if (events.Count <= 10 && round.FinalScore.GetValueOrDefault() > 0)
        {
            return await UnlockAchievement(teamId, achievementId, "Rychlý démon - méně než 10 tahů!");
        }
        
        return null;
    }

    /// <summary>
    /// Minimální pohyby - vysoké skóre s malým počtem tahů
    /// </summary>
    private async Task<Achievement?> CheckMinimalMoves(string teamId, List<ScoringEventData> events)
    {
        const string achievementId = "minimal-moves";
        
        var crystalTouches = events.Count(e => e.Description.Contains("Dotyk krystalu"));
        
        if (crystalTouches >= 5 && events.Count <= 12)
        {
            return await UnlockAchievement(teamId, achievementId, "Minimální pohyby - efektivní hra!");
        }
        
        return null;
    }

    /// <summary>
    /// Bez poškození - žádné narušení síry
    /// </summary>
    private async Task<Achievement?> CheckNoSulfurDamage(string teamId, List<ScoringEventData> events)
    {
        const string achievementId = "no-sulfur-damage";
        
        var hasCrystals = events.Any(e => e.Description.Contains("Dotyk krystalu"));
        var hasSulfur = events.Any(e => e.Description.Contains("Narušení síry"));
        
        if (hasCrystals && !hasSulfur && events.Count >= 5)
        {
            return await UnlockAchievement(teamId, achievementId, "Bez poškození sírou!");
        }
        
        return null;
    }

    /// <summary>
    /// Crystal Master - více než 10 krystalů v jednom kole
    /// </summary>
    private async Task<Achievement?> CheckCrystalMaster(string teamId, List<ScoringEventData> events)
    {
        const string achievementId = "crystal-master";
        
        var crystalTouches = events.Count(e => e.Description.Contains("Dotyk krystalu"));
        
        if (crystalTouches >= 10)
        {
            return await UnlockAchievement(teamId, achievementId, "Crystal Master - více než 10 krystalů!");
        }
        
        return null;
    }

    #endregion

    #region Helper Methods

    private async Task<Achievement?> UnlockAchievement(string teamId, string achievementId, string reason)
    {
        // Zkontrolovat, jestli tým už achievement nemá
        var existingAchievements = await _teamAchievementRepository.GetAllAsync();
        if (existingAchievements.Any(a => a.TeamId == teamId && a.AchievementId == achievementId))
        {
            return null; // Už ho má
        }

        var teamAchievement = new TeamAchievement
        {
            Id = Guid.NewGuid().ToString(),
            TeamId = teamId,
            AchievementId = achievementId,
            UnlockedAt = DateTime.UtcNow
        };

        await _teamAchievementRepository.AddAsync(teamAchievement);
        
        _logger.LogInformation("[AchievementEvaluation] Achievement unlocked! Team: {TeamId}, Achievement: {AchievementId}, Reason: {Reason}", 
            teamId, achievementId, reason);
        
        // Vrátit celý achievement objekt
        var achievement = await _achievementRepository.GetByIdAsync(achievementId);
        
        // Notifikovat tým přes WebSocket (starý mechanismus)
        await _hubContext.Clients.Group($"team_{teamId}")
            .SendAsync("OnAchievementUnlocked", achievementId);
        
        return achievement;
    }

    public async Task<bool> CheckAchievementAsync(string teamId, string achievementId)
    {
        var existingAchievements = await _teamAchievementRepository.GetAllAsync();
        return existingAchievements.Any(a => a.TeamId == teamId && a.AchievementId == achievementId);
    }

    #endregion
}
