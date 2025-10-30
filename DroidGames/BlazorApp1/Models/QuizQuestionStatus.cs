namespace BlazorApp1.Models;

/// <summary>
/// Represents the status of a quiz question for a specific team
/// </summary>
public class QuizQuestionStatus
{
    public QuizQuestion Question { get; set; } = new();
    public QuizQuestionState State { get; set; } = QuizQuestionState.Unanswered;
    public int TotalAttempts { get; set; }
    public int CorrectAttempts { get; set; }
    public int IncorrectAttempts { get; set; }
    public DateTime? LastAttemptAt { get; set; }
}

public enum QuizQuestionState
{
    Unanswered,      // Šedá - ještě nezodpovězeno
    Correct,         // Zelená - zodpovězeno správně
    Incorrect        // Červená - zodpovězeno špatně (ale ne správně)
}
