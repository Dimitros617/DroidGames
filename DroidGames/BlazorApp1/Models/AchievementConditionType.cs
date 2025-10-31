namespace BlazorApp1.Models;

public enum AchievementConditionType
{
    // Map-based achievements
    FirstPoints,
    TouchAllCrystals,
    NoSulfurDisruption,
    PerfectRound,
    WinAllRounds,
    CooperativeBonus,
    ConsistentScores,
    PositionImprovement,
    FastCompletion,
    ExactScore,
    MinimalMoves,
    
    // Evaluation-based achievements (from referee scoring)
    FirstCrystalTouch,          // První dotyk krystalu v celé soutěži
    FirstSulfurHit,             // První narušení síry v celé soutěži
    ThreeCrystalsInRow,         // 3 krystaly dotknuté za sebou
    AllCrystalsTouched,         // Dotknutí všech krystalů v kole
    AllSulfursCleared,          // Odsunuti všech sír v kole
    PerfectRun,                 // Perfektní jízda - pouze krystaly
    SpeedDemon,                 // Rychlé dokončení kola
    MinimalMovesEfficient,      // Vysoké skóre s malým počtem tahů
    NoSulfurDamage,             // Žádné narušení síry v kole
    CrystalMaster,              // Více než 10 krystalů v jednom kole
    
    // Quiz-based achievements
    QuizFirstCorrect,           // První správná odpověď
    QuizCorrectAnswers,         // X správných odpovědí celkem
    QuizConsecutiveCorrect,     // X správných odpovědí v řadě
    QuizPerfectStreak,          // Perfektní série bez chyby
    QuizSpeedDemon,             // Odpověz do X sekund
    QuizNightOwl,               // Zodpověz otázku pozdě v noci
    QuizEarlyBird,              // Zodpověz otázku časně ráno
    QuizCompleteAll,            // Zodpověz všechny otázky správně
    QuizNoMistakes,             // Projdi kvíz bez jediné chyby
    QuizMastermind,             // Dosáhni X% úspěšnosti
    
    Custom
}
