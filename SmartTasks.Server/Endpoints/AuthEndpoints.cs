using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartTasks.Server.Data;

namespace SmartTasks.Server.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/api/auth/register", async (RegisterRequest request, AppDbContext db, PasswordHasher<User> hasher, HttpContext httpContext) =>
        {
            var username = request.Username?.Trim() ?? "";
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(request.Password))
            {
                return Results.BadRequest("Username and password are required");
            }
            if (request.Password.Length < 6)
            {
                return Results.BadRequest("Password must be at least 6 characters");
            }

            var existing = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (existing is not null)
            {
                return Results.Conflict("Username already taken");
            }

            var user = new User { Username = username };
            user.PasswordHash = hasher.HashPassword(user, request.Password);
            db.Users.Add(user);
            await db.SaveChangesAsync();

            await SignInAsync(httpContext, user);

            return Results.Ok(new { id = user.Id, username = user.Username });
        });

        app.MapPost("/api/auth/login", async (LoginRequest request, AppDbContext db, PasswordHasher<User> hasher, HttpContext httpContext) =>
        {
            var username = request.Username?.Trim() ?? "";
            var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var result = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password ?? "");
            if (result == PasswordVerificationResult.Failed)
            {
                return Results.Unauthorized();
            }

            await SignInAsync(httpContext, user);

            return Results.Ok(new { id = user.Id, username = user.Username });
        });

        app.MapPost("/api/auth/logout", async (HttpContext httpContext) =>
        {
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.NoContent();
        });

        app.MapGet("/api/auth/me", (HttpContext httpContext) =>
        {
            if (httpContext.User?.Identity?.IsAuthenticated != true)
            {
                return Results.Unauthorized();
            }

            var id = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var username = httpContext.User.FindFirstValue(ClaimTypes.Name);
            return Results.Ok(new { id = int.Parse(id!), username });
        });
    }

    private static async Task SignInAsync(HttpContext httpContext, User user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    }
}

public record RegisterRequest(string? Username, string? Password);
public record LoginRequest(string? Username, string? Password);
