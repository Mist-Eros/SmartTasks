using SmartTasks.Shared;

namespace SmartTasks.Server.Data;

public static class TaskMapper
{
    public static TaskEntity ToEntity(TaskDto dto)
    {
        return new TaskEntity
        {
            Title = dto.Title,
            Date = dto.Date,
            Time = dto.Time,
            Priority = dto.Priority,
            TagsCsv = string.Join(",", dto.Tags ?? new List<string>()),
            UserId = "default-user",
            CreatedAt = DateTime.UtcNow
        };
    }

    public static TaskDto ToDto(TaskEntity entity)
    {
        return new TaskDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Date = entity.Date,
            Time = entity.Time,
            Priority = entity.Priority,
            Tags = string.IsNullOrWhiteSpace(entity.TagsCsv)
                ? new List<string>()
                : entity.TagsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
        };
    }
}
