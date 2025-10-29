namespace BlazorApp1.Models;

public class QuizQuestion
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Question { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public int CorrectAnswerIndex { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public QuizDifficulty Difficulty { get; set; }
    public List<string> Tags { get; set; } = new();
}
