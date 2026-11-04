using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Maratona.Api.Data;
using Maratona.Api.Infrastructure;
using Maratona.Api.Reviews;
using Maratona.Api.Titles;
using Microsoft.EntityFrameworkCore;

namespace Maratona.Api.Feed;

public static class FeedEndpoints
{
    private const int PageSize = 20;

    public static void MapFeedEndpoints(this IEndpointRouteBuilder app) => app.MapGet("/feed", Feed);

    private static async Task<IResult> Feed(
        ClaimsPrincipal user,
        AppDbContext db,
        TitleService titles,
        CancellationToken ct,
        [AllowedValues("all", "following")] string scope = "all",
        [Range(1, 1000)] int page = 1)
    {
        var viewerId = user.UserIdOrNull();
        var query = db.Reviews.AsQueryable();
        if (scope == "following")
        {
            if (viewerId is null)
            {
                return Results.Unauthorized();
            }
            query = query.Where(r => db.Follows.Any(f => f.FollowerId == viewerId && f.FollowedId == r.UserId));
        }
        var total = await query.CountAsync(ct);
        var reviews = await ReviewQueries.Load(
            db,
            titles,
            query.OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id).Skip((page - 1) * PageSize).Take(PageSize),
            viewerId,
            includeTitle: true,
            ct);
        return Results.Ok(new PageDto<ReviewDto>(reviews, page, Common.TotalPages(total, PageSize), total));
    }
}
