namespace BlazorApp1.Models;

public class Reminder
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Text { get; set; } = string.Empty;
    public int IntervalMinutes { get; set; }
    public DateTime? LastTriggered { get; set; }
    public DateTime? NextDue { get; set; }
    public bool IsActive { get; set; } = true;
    public ReminderPriority Priority { get; set; }
}
