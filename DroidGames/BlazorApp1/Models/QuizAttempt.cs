namespace BlazorApp1.Models;

/// <summary>
/// Represents a single attempt at answering a quiz question
/// Multiple attempts per question are allowed
/// </summary>
public class QuizAttempt
{
    public string QuestionId { get; set; } = string.Empty;
    public int SelectedAnswerIndex { get; set; }
    public bool IsCorrect { get; set; }
    public int TimeToAnswerMs { get; set; }
    public DateTime AttemptedAt { get; set; } = DateTime.Now;
}
