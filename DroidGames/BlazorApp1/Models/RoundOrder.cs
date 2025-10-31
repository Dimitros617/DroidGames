namespace BlazorApp1.Models;

/// <summary>
/// Pořadí týmu v konkrétním kole soutěže
/// </summary>
public class RoundOrder
{
    /// <summary>
    /// Unikátní ID záznamu
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Číslo kola (1, 2, 3, ...)
    /// </summary>
    public int RoundNumber { get; set; }
    
    /// <summary>
    /// ID týmu
    /// </summary>
    public string TeamId { get; set; } = string.Empty;
    
    /// <summary>
    /// Pořadí týmu v daném kole (1 = první jede, 2 = druhý, atd.)
    /// </summary>
    public int Position { get; set; }
    
    /// <summary>
    /// Zda je pořadí schválené a zamčené (nelze měnit)
    /// </summary>
    public bool IsConfirmed { get; set; }
    
    /// <summary>
    /// Zda je pořadí veřejně viditelné
    /// </summary>
    public bool IsPublic { get; set; }
    
    /// <summary>
    /// Datum a čas vytvoření
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    /// <summary>
    /// Datum a čas posledního míchání
    /// </summary>
    public DateTime? LastShuffledAt { get; set; }
    
    /// <summary>
    /// Datum a čas schválení
    /// </summary>
    public DateTime? ConfirmedAt { get; set; }
}
