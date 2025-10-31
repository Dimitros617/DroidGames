namespace BlazorApp1.Models;

/// <summary>
/// Finální schválené skóre týmu za kolo
/// </summary>
public class FinalRoundScore
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// ID týmu
    /// </summary>
    public string TeamId { get; set; } = string.Empty;
    
    /// <summary>
    /// Číslo kola
    /// </summary>
    public int RoundNumber { get; set; }
    
    /// <summary>
    /// Počet dotyků krystalů
    /// </summary>
    public int CrystalTouches { get; set; }
    
    /// <summary>
    /// Body za krystaly
    /// </summary>
    public int CrystalPoints { get; set; }
    
    /// <summary>
    /// Počet narušení síry
    /// </summary>
    public int SulfurHits { get; set; }
    
    /// <summary>
    /// Ztráta bodů za síru (záporné číslo)
    /// </summary>
    public int SulfurPenalty { get; set; }
    
    /// <summary>
    /// Validní přesuny
    /// </summary>
    public int ValidMoves { get; set; }
    
    /// <summary>
    /// Body za bonusy (první dotyk, shromaždiště, atd.)
    /// </summary>
    public int BonusPoints { get; set; }
    
    /// <summary>
    /// Celkové skóre za kolo
    /// </summary>
    public int TotalScore { get; set; }
    
    /// <summary>
    /// Kdy bylo schváleno
    /// </summary>
    public DateTime ApprovedAt { get; set; }
    
    /// <summary>
    /// Kdo schválil (HeadReferee ID)
    /// </summary>
    public string ApprovedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// ID mapy použité v kole
    /// </summary>
    public string MapConfigurationId { get; set; } = string.Empty;
    
    /// <summary>
    /// Detaily bonusů (např. "První dotyk krystalu: +5", "Shromaždiště: +10")
    /// </summary>
    public Dictionary<string, int> BonusBreakdown { get; set; } = new();
    
    /// <summary>
    /// Kompletní události ze schváleného hodnocení (pro zobrazení detailů)
    /// </summary>
    public List<ScoringEventData> Events { get; set; } = new();
}
