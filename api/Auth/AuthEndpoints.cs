using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Maratona.Api.Data;
using Maratona.Api.Data.Entities;
using Maratona.Api.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Maratona.Api.Auth;

public record RegisterRequest(
    [Required, RegularExpression("^[a-zA-Z0-9_.]{3,30}$", ErrorMessage = "Use de 3 a 30 letras, números, ponto ou _.")] string Username,
    [Required, EmailAddress, StringLength(254)] string Email,
    [Required, StringLength(100, MinimumLength = 8)] string Password,
    [StringLength(60)] string? DisplayName);

public record LoginRequest(
    [Required, StringLength(254)] string Login,
    [Required, StringLength(100)] string Password);

public record PasswordRequest(
    [Required, StringLength(100)] string CurrentPassword,
    [Required, StringLength(100, MinimumLength = 8)] string NewPassword);

public record DeleteAccountRequest([Required, StringLength(100)] string Password);

public record MeDto(
    int Id,
    string Username,
    string Email,
    string DisplayName,
    string? Bio,
    string? AvatarUrl,
    string? CoverUrl,
    RatingStyle RatingStyle,
    DateTime CreatedAt);

public record AuthResponse(string Token, MeDto User);

public static class AuthEndpoints
{
    private static readonly PasswordHasher<User> Hasher = new();
    private static readonly string DummyHash = Hasher.HashPassword(new User(), Guid.NewGuid().ToString());
    private static readonly HashSet<string> ReservedUsernames = ["search", "me", "admin"];

    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/auth").RequireRateLimiting(RateLimits.AuthPolicy);
        auth.MapPost("/register", Register);
        auth.MapPost("/login", Login);

        var me = app.MapGroup("/me").RequireAuthorization();
        me.MapGet("", Me);
        me.MapPut("/password", ChangePassword).RequireRateLimiting(RateLimits.AuthPolicy);
        me.MapPost("/delete", DeleteAccount).RequireRateLimiting(RateLimits.AuthPolicy);
    }

    public static MeDto ToMe(User user) => new(
        user.Id,
        user.Username,
        user.Email,
        user.DisplayName,
        user.Bio,
        user.AvatarUrl,
        user.CoverUrl,
        user.RatingStyle,
        user.CreatedAt);

    private static async Task<IResult> Register(RegisterRequest request, AppDbContext db, JwtTokens tokens, CancellationToken ct)
    {
        var username = request.Username.Trim().ToLowerInvariant();
        var email = request.Email.Trim().ToLowerInvariant();
        if (ReservedUsernames.Contains(username) || await db.Users.AnyAsync(u => u.Username == username, ct))
        {
            return Common.Invalid("username", "Esse nome de usuário já está em uso.");
        }
        if (await db.Users.AnyAsync(u => u.Email == email, ct))
        {
            return Common.Invalid("email", "Esse e-mail já está cadastrado.");
        }
        var displayName = string.IsNullOrWhiteSpace(request.DisplayName) ? request.Username.Trim() : request.DisplayName.Trim();
        var user = new User { Username = username, Email = email, DisplayName = displayName };
        user.PasswordHash = Hasher.HashPassword(user, request.Password);
        db.Users.Add(user);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return Common.Invalid("username", "Nome de usuário ou e-mail já cadastrado.");
        }
        return Results.Ok(new AuthResponse(tokens.Create(user), ToMe(user)));
    }

    private static async Task<IResult> Login(LoginRequest request, AppDbContext db, JwtTokens tokens, CancellationToken ct)
    {
        var login = request.Login.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == login || u.Email == login, ct);
        if (user is null)
        {
            Hasher.VerifyHashedPassword(new User(), DummyHash, request.Password);
            return Results.Unauthorized();
        }
        var result = Hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            return Results.Unauthorized();
        }
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = Hasher.HashPassword(user, request.Password);
            await db.SaveChangesAsync(ct);
        }
        return Results.Ok(new AuthResponse(tokens.Create(user), ToMe(user)));
    }

    private static async Task<IResult> Me(ClaimsPrincipal principal, AppDbContext db, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([principal.UserId()], ct);
        return user is null ? Results.Unauthorized() : Results.Ok(ToMe(user));
    }

    private static async Task<IResult> ChangePassword(PasswordRequest request, ClaimsPrincipal principal, AppDbContext db, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([principal.UserId()], ct);
        if (user is null)
        {
            return Results.Unauthorized();
        }
        if (Hasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword) == PasswordVerificationResult.Failed)
        {
            return Common.Invalid("currentPassword", "Senha atual incorreta.");
        }
        user.PasswordHash = Hasher.HashPassword(user, request.NewPassword);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteAccount(DeleteAccountRequest request, ClaimsPrincipal principal, AppDbContext db, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([principal.UserId()], ct);
        if (user is null)
        {
            return Results.Unauthorized();
        }
        if (Hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
        {
            return Common.Invalid("password", "Senha incorreta.");
        }
        db.Users.Remove(user);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}
