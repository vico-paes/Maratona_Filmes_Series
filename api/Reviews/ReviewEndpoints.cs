using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Maratona.Api.Data;
using Maratona.Api.Data.Entities;
using Maratona.Api.Infrastructure;
using Maratona.Api.Library;
using Maratona.Api.Titles;
using Microsoft.EntityFrameworkCore;

namespace Maratona.Api.Reviews;

public record ReviewRequest(
    [Range(1, int.MaxValue)] int? SeasonId,
    [Required, StringLength(10000, MinimumLength = 1)] string Body,
    bool Spoiler);

public record NoteRequest(
    [Range(1, int.MaxValue)] int? SeasonId,
    [Range(1, int.MaxValue)] int? EpisodeId,
    [StringLength(20000)] string? Body);

public record NoteDto(int Id, int? SeasonId, int? EpisodeId, string Body, DateTime UpdatedAt);

public static class ReviewEndpoints
{
    private const int PageSize = 20;

    public static void MapReviewEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/titles/{titleId:int}/reviews", TitleReviews);

        var me = app.MapGroup("/me").RequireAuthorization();
        me.MapPut("/titles/{titleId:int}/review", SaveReview);
        me.MapDelete("/reviews/{reviewId:int}", DeleteReview);
        me.MapPut("/titles/{titleId:int}/notes", SaveNote);

        var likes = app.MapGroup("/reviews/{reviewId:int}/like").RequireAuthorization();
        likes.MapPost("", Like);
        likes.MapDelete("", Unlike);
    }

    private static async Task<IResult> TitleReviews(
        int titleId,
        ClaimsPrincipal user,
        AppDbContext db,
        TitleService titles,
        CancellationToken ct,
        [Range(1, int.MaxValue)] int? seasonId = null,
        [Range(1, 1000)] int page = 1)
    {
        var query = db.Reviews.Where(r => r.TitleId == titleId && r.SeasonId == seasonId);
        var total = await query.CountAsync(ct);
        var reviews = await ReviewQueries.Load(
            db,
            titles,
            query.OrderByDescending(r => r.CreatedAt).Skip((page - 1) * PageSize).Take(PageSize),
            user.UserIdOrNull(),
            includeTitle: false,
            ct);
        return Results.Ok(new PageDto<ReviewDto>(reviews, page, Common.TotalPages(total, PageSize), total));
    }

    private static async Task<IResult> SaveReview(
        int titleId,
        ReviewRequest request,
        ClaimsPrincipal user,
        AppDbContext db,
        TitleService titles,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Body))
        {
            return Common.Invalid("body", "Escreva o texto do review.");
        }
        if (!await db.Titles.AnyAsync(t => t.Id == titleId, ct))
        {
            return Results.NotFound();
        }
        if (request.SeasonId is { } seasonId && !await db.Seasons.AnyAsync(s => s.Id == seasonId && s.TitleId == titleId, ct))
        {
            return Common.Invalid("seasonId", "Essa temporada não pertence ao título.");
        }
        var userId = user.UserId();
        var review = await db.Reviews.FirstOrDefaultAsync(r => r.UserId == userId && r.TitleId == titleId && r.SeasonId == request.SeasonId, ct);
        if (review is null)
        {
            review = new Review { UserId = userId, TitleId = titleId, SeasonId = request.SeasonId };
            db.Reviews.Add(review);
        }
        review.Body = request.Body.Trim();
        review.Spoiler = request.Spoiler;
        review.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        if (request.SeasonId is { } watchedSeason)
        {
            await Watching.ChangeSeason(db, userId, watchedSeason, e => Watching.Mark(e, true), ct);
        }
        else
        {
            await Watching.WatchTitle(db, userId, titleId, ct);
        }
        var saved = await ReviewQueries.Load(db, titles, db.Reviews.Where(r => r.Id == review.Id), userId, includeTitle: false, ct);
        return Results.Ok(saved[0]);
    }

    private static async Task<IResult> DeleteReview(int reviewId, ClaimsPrincipal user, AppDbContext db, CancellationToken ct)
    {
        var userId = user.UserId();
        var deleted = await db.Reviews.Where(r => r.Id == reviewId && r.UserId == userId).ExecuteDeleteAsync(ct);
        return deleted == 0 ? Results.NotFound() : Results.NoContent();
    }

    private static async Task<IResult> Like(int reviewId, ClaimsPrincipal user, AppDbContext db, CancellationToken ct)
    {
        if (!await db.Reviews.AnyAsync(r => r.Id == reviewId, ct))
        {
            return Results.NotFound();
        }
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO review_likes (user_id, review_id, created_at) VALUES ({user.UserId()}, {reviewId}, {DateTime.UtcNow})
            ON CONFLICT DO NOTHING
            """, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> Unlike(int reviewId, ClaimsPrincipal user, AppDbContext db, CancellationToken ct)
    {
        var userId = user.UserId();
        await db.ReviewLikes.Where(l => l.ReviewId == reviewId && l.UserId == userId).ExecuteDeleteAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> SaveNote(int titleId, NoteRequest request, ClaimsPrincipal user, AppDbContext db, CancellationToken ct)
    {
        if (!await db.Titles.AnyAsync(t => t.Id == titleId, ct))
        {
            return Results.NotFound();
        }
        var seasonId = request.SeasonId;
        if (request.EpisodeId is { } episodeId)
        {
            var episode = await db.Episodes
                .Where(e => e.Id == episodeId && e.Season.TitleId == titleId)
                .Select(e => new { e.SeasonId })
                .FirstOrDefaultAsync(ct);
            if (episode is null || (seasonId is not null && seasonId != episode.SeasonId))
            {
                return Common.Invalid("episodeId", "Esse episódio não pertence ao título.");
            }
            seasonId = episode.SeasonId;
        }
        else if (seasonId is { } id && !await db.Seasons.AnyAsync(s => s.Id == id && s.TitleId == titleId, ct))
        {
            return Common.Invalid("seasonId", "Essa temporada não pertence ao título.");
        }
        var userId = user.UserId();
        var note = await db.PrivateNotes.FirstOrDefaultAsync(n =>
            n.UserId == userId && n.TitleId == titleId && n.SeasonId == seasonId && n.EpisodeId == request.EpisodeId, ct);
        if (string.IsNullOrWhiteSpace(request.Body))
        {
            if (note is not null)
            {
                db.PrivateNotes.Remove(note);
                await db.SaveChangesAsync(ct);
            }
            return Results.NoContent();
        }
        if (note is null)
        {
            note = new PrivateNote { UserId = userId, TitleId = titleId, SeasonId = seasonId, EpisodeId = request.EpisodeId };
            db.PrivateNotes.Add(note);
        }
        note.Body = request.Body;
        note.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new NoteDto(note.Id, note.SeasonId, note.EpisodeId, note.Body, note.UpdatedAt));
    }
}
