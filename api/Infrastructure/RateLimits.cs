using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Maratona.Api.Infrastructure;

public static class RateLimits
{
    public const string AuthPolicy = "auth";
    private const int PerIpPerMinute = 300;
    private const int PerUserPerMinute = 120;
    private const int AuthPerIpPerMinute = 10;

    public static void Configure(RateLimiterOptions options)
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.GlobalLimiter = PartitionedRateLimiter.CreateChained(
            PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(Ip(context), _ => Window(PerIpPerMinute))),
            PartitionedRateLimiter.Create<HttpContext, string>(context =>
                context.User.UserIdOrNull() is { } userId
                    ? RateLimitPartition.GetFixedWindowLimiter($"user:{userId}", _ => Window(PerUserPerMinute))
                    : RateLimitPartition.GetNoLimiter("anonymous")));
        options.AddPolicy(AuthPolicy, context =>
            RateLimitPartition.GetFixedWindowLimiter(Ip(context), _ => Window(AuthPerIpPerMinute)));
    }

    private static string Ip(HttpContext context) => context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private static FixedWindowRateLimiterOptions Window(int permits) => new()
    {
        PermitLimit = permits,
        Window = TimeSpan.FromMinutes(1),
        QueueLimit = 0
    };
}
