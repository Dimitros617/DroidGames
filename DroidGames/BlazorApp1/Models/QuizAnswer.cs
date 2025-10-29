namespace BlazorApp1.Models;

public class QuizAnswer
{
    public string QuestionId { get; set; } = string.Empty;
    public int SelectedAnswerIndex { get; set; }
    public bool IsCorrect { get; set; }
    public int TimeToAnswerMs { get; set; }
}
