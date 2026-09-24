using Microsoft.EntityFrameworkCore;

namespace SmartTasks.Server.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<TaskEntity> Tasks => Set<TaskEntity>();
}
