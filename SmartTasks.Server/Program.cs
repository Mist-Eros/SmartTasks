using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using SmartTasks.Shared;
using SmartTasks.Server.Data;
using SmartTasks.Server.Endpoints;
using SmartTasks.Server.Services;

static int GetUserId(HttpContext ctx)
{
    var idClaim = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    return int.TryParse(idClaim, out var id) ? id : throw new UnauthorizedAccessException("No user id in claims");
}

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddHttpClient();
builder.Services.AddScoped<GeminiService>();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "SmartTasks.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = context =>
        {
            // Return 401 for API requests instead of redirecting to a login page
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<PasswordHasher<User>>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

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
}).RequireAuthorization();

app.MapPost("/api/tasks", async (TaskDto dto, AppDbContext db, HttpContext ctx) =>
{
    var userId = GetUserId(ctx);
    var entity = TaskMapper.ToEntity(dto, userId);
    db.Tasks.Add(entity);
    await db.SaveChangesAsync();
    return Results.Ok(TaskMapper.ToDto(entity));
}).RequireAuthorization();

app.MapGet("/api/tasks", async (AppDbContext db, HttpContext ctx) =>
{
    var userId = GetUserId(ctx);
    var tasks = await db.Tasks
        .Where(t => t.UserId == userId)
        .OrderByDescending(t => t.CreatedAt)
        .ToListAsync();

    return Results.Ok(tasks.Select(TaskMapper.ToDto));
}).RequireAuthorization();

app.MapPut("/api/tasks/{id:int}", async (int id, TaskDto dto, AppDbContext db, HttpContext ctx) =>
{
    var userId = GetUserId(ctx);
    var existing = await db.Tasks
        .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

    if (existing is null)
    {
        return Results.NotFound();
    }

    TaskMapper.ApplyUpdate(existing, dto);
    await db.SaveChangesAsync();

    return Results.Ok(TaskMapper.ToDto(existing));
}).RequireAuthorization();

app.MapDelete("/api/tasks/{id:int}", async (int id, AppDbContext db, HttpContext ctx) =>
{
    var userId = GetUserId(ctx);
    var existing = await db.Tasks
        .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

    if (existing is null)
    {
        return Results.NotFound();
    }

    db.Tasks.Remove(existing);
    await db.SaveChangesAsync();

    return Results.NoContent();
}).RequireAuthorization();

app.MapPost("/api/transcribe", async (HttpRequest request, GeminiService gemini) =>
{
    if (!request.HasFormContentType)
        return Results.BadRequest("Expected multipart form data");

    var form = await request.ReadFormAsync();
    var file = form.Files["audio"];
    if (file == null || file.Length == 0)
        return Results.BadRequest("No audio file provided");

    using var ms = new MemoryStream();
    await file.CopyToAsync(ms);
    try
    {
        var text = await gemini.TranscribeAudioAsync(ms.ToArray(), file.ContentType ?? "audio/webm");
        return Results.Ok(new { text });
    }
    catch (InvalidOperationException ex)
    {
        return Results.Problem(ex.Message);
    }
}).RequireAuthorization();

app.MapAuthEndpoints();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.MapFallbackToFile("index.html");

app.Run();
