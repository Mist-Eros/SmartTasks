namespace SmartTasks.Shared;

public class TaskDto
{
    public string Title { get; set; } = "";
    public string? Date { get; set; }
    public string? Time { get; set; }
    public string Priority { get; set; } = "medium";
    public List<string> Tags { get; set; } = new();
}
