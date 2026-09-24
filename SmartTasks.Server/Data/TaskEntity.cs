namespace SmartTasks.Server.Data;

public class TaskEntity
{
    public int Id { get; set; }
    public string UserId { get; set; } = "default-user";
    public string Title { get; set; } = "";
    public string? Date { get; set; }
    public string? Time { get; set; }
    public string Priority { get; set; } = "medium";
    public string TagsCsv { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
