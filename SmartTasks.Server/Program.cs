using SmartTasks.Shared;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/api/ping", () => new { pong = true });

app.MapPost("/api/parse", (ParseRequest request) =>
    Results.Ok(new TaskDto
    {
        Title = "Sample task",
        Date = "2026-10-01",
        Time = "14:00",
        Priority = "high",
        Tags = new List<string> { "sample", "hardcoded" }
    }));

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.MapFallbackToFile("index.html");

app.Run();
