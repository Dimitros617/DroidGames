namespace BlazorApp1.Models;

/// <summary>
/// Globální stav celé hry/soutěže
/// </summary>
public enum GameStatus
{
    /// <summary>
    /// Příprava před začátkem soutěže
    /// </summary>
    Preparation,
    
    /// <summary>
    /// Právě probíhá jízda (čas běží)
    /// </summary>
    RoundInProgress,
    
    /// <summary>
    /// Čeká se na hodnocení rozhodčích
    /// </summary>
    WaitingForScoring,
    
    /// <summary>
    /// Příprava další jízdy (po schválení výsledků)
    /// </summary>
    PreparingNextRound,
    
    /// <summary>
    /// Přestávka
    /// </summary>
    Break,
    
    /// <summary>
    /// Soutěž ukončena
    /// </summary>
    Finished
}
