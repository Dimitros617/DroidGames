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
