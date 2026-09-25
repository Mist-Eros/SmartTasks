using SmartTasks.Shared;

namespace SmartTasks.Server.Data;

public static class TaskMapper
{
    public static TaskEntity ToEntity(TaskDto dto, int userId)
    {
        return new TaskEntity
        {
            UserId = userId,
            Title = dto.Title,
            Date = dto.Date,
            Time = dto.Time,
            Priority = dto.Priority,
            TagsCsv = string.Join(",", dto.Tags ?? new List<string>()),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static void ApplyUpdate(TaskEntity existing, TaskDto dto)
    {
        existing.Title = dto.Title;
        existing.Date = dto.Date;
        existing.Time = dto.Time;
        existing.Priority = dto.Priority;
        existing.TagsCsv = string.Join(",", dto.Tags ?? new List<string>());
        existing.UpdatedAt = DateTime.UtcNow;
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
