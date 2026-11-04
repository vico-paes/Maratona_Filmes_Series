using System.Security.Claims;
using Maratona.Api.Data.Entities;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Maratona.Api.Infrastructure;

public record PageDto<T>(List<T> Items, int Page, int TotalPages, int Total);

public record UserSummaryDto(string Username, string DisplayName, string? AvatarUrl);

public static class Common
{
    public static int UserId(this ClaimsPrincipal user) =>
        int.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    public static int? UserIdOrNull(this ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id) ? id : null;

    public static int TotalPages(int total, int pageSize) => (total + pageSize - 1) / pageSize;

    public static bool IsHttpsUrl(string? value) =>
        string.IsNullOrEmpty(value) || (Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps);

    public static IResult Invalid(string field, string message) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { [field] = [message] });

    public static UserSummaryDto ToSummary(User user) => new(user.Username, user.DisplayName, user.AvatarUrl);
}
