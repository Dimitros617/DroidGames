namespace BlazorApp1.Models;

/// <summary>
/// Tracks a team's progress through the quiz system
/// Stores all attempts for each question
/// </summary>
public class QuizProgress
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string TeamId { get; set; } = string.Empty;
    
    /// <summary>
    /// All attempts across all questions (multiple attempts per question allowed)
    /// </summary>
    public List<QuizAttempt> Attempts { get; set; } = new();
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? LastAttemptAt { get; set; }
    
    // Computed properties
    public int TotalAttempts => Attempts.Count;
    public int CorrectAnswers => Attempts.Count(a => a.IsCorrect);
    public int IncorrectAnswers => Attempts.Count(a => !a.IsCorrect);
    
    /// <summary>
    /// Gets the list of question IDs that have been answered correctly (first correct answer only)
    /// </summary>
    public List<string> AnsweredQuestionIds => 
        Attempts
            .Where(a => a.IsCorrect)
            .GroupBy(a => a.QuestionId)
            .Select(g => g.First().QuestionId)
            .ToList();
    
    /// <summary>
    /// Success rate (correct attempts / total attempts)
    /// </summary>
    public double SuccessRate => TotalAttempts > 0 ? (double)CorrectAnswers / TotalAttempts * 100 : 0;
}
