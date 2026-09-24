using Microsoft.EntityFrameworkCore;
using SmartTasks.Shared;
using SmartTasks.Server.Data;
using SmartTasks.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddHttpClient();
builder.Services.AddScoped<GeminiService>();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/api/ping", () => new { pong = true });

app.MapPost("/api/parse", async (ParseRequest request, GeminiService gemini) =>
{
    try
    {
        var task = await gemini.ParseTaskAsync(request.Text);
        return Results.Ok(task);
    }
    catch (InvalidOperationException ex)
    {
        return Results.Problem(ex.Message);
    }
});

app.MapPost("/api/tasks", async (TaskDto dto, AppDbContext db) =>
{
    var entity = TaskMapper.ToEntity(dto);
    db.Tasks.Add(entity);
    await db.SaveChangesAsync();
    return Results.Ok(TaskMapper.ToDto(entity));
});

app.MapGet("/api/tasks", async (AppDbContext db) =>
{
    var tasks = await db.Tasks
        .Where(t => t.UserId == "default-user")
        .OrderByDescending(t => t.CreatedAt)
        .ToListAsync();

    return Results.Ok(tasks.Select(TaskMapper.ToDto));
});

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.MapFallbackToFile("index.html");

app.Run();
