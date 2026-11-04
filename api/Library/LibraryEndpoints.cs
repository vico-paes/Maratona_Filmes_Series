using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Maratona.Api.Data;
using Maratona.Api.Data.Entities;
using Maratona.Api.Infrastructure;
using Maratona.Api.Reviews;
using Maratona.Api.Titles;
using Microsoft.EntityFrameworkCore;

namespace Maratona.Api.Library;

public record FlagRequest(bool Value);

public record RatingRequest(decimal? Rating, RatingStyle? Style);

public record EntryDto(int TitleId, bool InWatchlist, bool Watched, DateTime? WatchedAt, decimal? Rating, RatingStyle? RatingStyle);

public record SeasonEntryDto(int SeasonId, int SeasonNumber, bool Watched, DateTime? WatchedAt, decimal? Rating, RatingStyle? RatingStyle);

public record SeasonChangeDto(SeasonEntryDto Season, bool TitleWatched);

public record LibraryItemDto(
    int TitleId,
    TitleCardDto Title,
    bool InWatchlist,
    bool Watched,
    DateTime? WatchedAt,
    decimal? Rating,
    RatingStyle? RatingStyle,
    DateTime AddedAt,
    int NewSeasons);

public record NewSeasonDto(int TitleId, TitleCardDto Title, int SeasonId, int SeasonNumber, string Name, DateOnly? AirDate, bool Released);

public record MyTitleStateDto(EntryDto? Entry, List<SeasonEntryDto> Seasons, List<ReviewDto> Reviews, List<NoteDto> Notes);

public static class LibraryEndpoints
{
    private const int PageSize = 40;

    public static void MapLibraryEndpoints(this IEndpointRouteBuilder app)
    {
        var me = app.MapGroup("/me").RequireAuthorization();
        me.MapGet("/library", GetLibrary);
        me.MapGet("/new-seasons", GetNewSeasons);
        me.MapGet("/titles/{titleId:int}", GetState);
        me.MapPut("/titles/{titleId:int}/watchlist", SetWatchlist);
        me.MapPut("/titles/{titleId:int}/watched", SetWatched);
        me.MapPut("/titles/{titleId:int}/rating", SetRating);
        me.MapPut("/seasons/{seasonId:int}/watched", SetSeasonWatched);
        me.MapPut("/seasons/{seasonId:int}/rating", SetSeasonRating);
    }

    private static async Task<IResult> GetLibrary(
        ClaimsPrincipal user,
        AppDbContext db,
        TitleService titles,
        CancellationToken ct,
        [AllowedValues("all", "movie", "tv")] string type = "all",
        [AllowedValues("all", "watchlist", "watched")] string status = "all",
        [AllowedValues("unwatched", "watched", "added", "recent", "rating_desc", "rating_asc", "imdb", "year", "az")] string sort = "unwatched",
        [Range(1, 1000)] int page = 1)
    {
        var userId = user.UserId();
        var query = db.LibraryEntries.Where(e => e.UserId == userId);
        query = type switch
        {
            "movie" => query.Where(e => e.Title.MediaType == MediaType.Movie),
            "tv" => query.Where(e => e.Title.MediaType == MediaType.Tv),
            _ => query
        };
        query = status switch
        {
            "watchlist" => query.Where(e => e.InWatchlist && !e.Watched),
            "watched" => query.Where(e => e.Watched),
            _ => query.Where(e => e.InWatchlist || e.Watched)
        };
        var ordered = sort switch
        {
            "watched" => query.OrderByDescending(e => e.Watched).ThenByDescending(e => e.WatchedAt),
            "added" => query.OrderByDescending(e => e.AddedAt),
            "recent" => query.OrderBy(e => e.WatchedAt == null).ThenByDescending(e => e.WatchedAt),
            "rating_desc" => query.OrderBy(e => e.Rating == null)
                .ThenByDescending(e => e.RatingStyle == RatingStyle.Stars ? e.Rating * 2 : e.Rating),
            "rating_asc" => query.OrderBy(e => e.Rating == null)
                .ThenBy(e => e.RatingStyle == RatingStyle.Stars ? e.Rating * 2 : e.Rating),
            "imdb" => query.OrderBy(e => e.Title.ExternalRating == null || e.Title.ExternalRating.Imdb == null)
                .ThenByDescending(e => e.Title.ExternalRating!.Imdb),
            "year" => query.OrderBy(e => e.Title.ReleaseDate == null).ThenByDescending(e => e.Title.ReleaseDate),
            "az" => query.OrderBy(e => e.Title.Name),
            _ => query.OrderBy(e => e.Watched).ThenByDescending(e => e.AddedAt)
        };
        var total = await query.CountAsync(ct);
        var entries = await ordered
            .ThenBy(e => e.TitleId)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync(ct);
        var ids = entries.Select(e => e.TitleId).ToList();
        var cards = await titles.Cards(ids, ct);
        var newSeasons = (await Watching.NewSeasons(db, userId, ids, ct)).ToLookup(s => s.TitleId);
        var items = entries
            .Select(e => new LibraryItemDto(
                e.TitleId,
                cards[e.TitleId],
                e.InWatchlist,
                e.Watched,
                e.WatchedAt,
                e.Rating,
                e.RatingStyle,
                e.AddedAt,
                newSeasons[e.TitleId].Count()))
            .ToList();
        return Results.Ok(new PageDto<LibraryItemDto>(items, page, Common.TotalPages(total, PageSize), total));
    }

    private static async Task<IResult> GetNewSeasons(ClaimsPrincipal user, AppDbContext db, TitleService titles, CancellationToken ct)
    {
        var seasons = await Watching.NewSeasons(db, user.UserId(), null, ct);
        var cards = await titles.Cards(seasons.Select(s => s.TitleId), ct);
        return Results.Ok(seasons
            .Select(s => new NewSeasonDto(s.TitleId, cards[s.TitleId], s.SeasonId, s.SeasonNumber, s.Name, s.AirDate, s.Released))
            .ToList());
    }

    private static async Task<IResult> GetState(int titleId, ClaimsPrincipal user, AppDbContext db, TitleService titles, CancellationToken ct)
    {
        if (!await db.Titles.AnyAsync(t => t.Id == titleId, ct))
        {
            return Results.NotFound();
        }
        var userId = user.UserId();
        var entry = await db.LibraryEntries.FindAsync([userId, titleId], ct);
        var seasons = await db.SeasonEntries
            .Where(e => e.UserId == userId && e.Season.TitleId == titleId)
            .OrderBy(e => e.Season.SeasonNumber)
            .Select(e => new SeasonEntryDto(e.SeasonId, e.Season.SeasonNumber, e.Watched, e.WatchedAt, e.Rating, e.RatingStyle))
            .ToListAsync(ct);
        var reviews = await ReviewQueries.Load(
            db,
            titles,
            db.Reviews.Where(r => r.UserId == userId && r.TitleId == titleId).OrderBy(r => r.SeasonId),
            userId,
            includeTitle: false,
            ct);
        var notes = await db.PrivateNotes
            .Where(n => n.UserId == userId && n.TitleId == titleId)
            .Select(n => new NoteDto(n.Id, n.SeasonId, n.EpisodeId, n.Body, n.UpdatedAt))
            .ToListAsync(ct);
        return Results.Ok(new MyTitleStateDto(entry is null ? null : ToDto(entry), seasons, reviews, notes));
    }

    private static Task<IResult> SetWatchlist(int titleId, FlagRequest request, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        EntryResult(Watching.ChangeEntry(db, user.UserId(), titleId, e => e.InWatchlist = request.Value, ct));

    private static Task<IResult> SetWatched(int titleId, FlagRequest request, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        EntryResult(Watching.ChangeEntry(db, user.UserId(), titleId, e => Watching.Mark(e, request.Value), ct));

    private static async Task<IResult> SetRating(int titleId, RatingRequest request, ClaimsPrincipal user, AppDbContext db, CancellationToken ct)
    {
        var userId = user.UserId();
        var style = request.Style ?? await ProfileStyle(db, userId, ct);
        if (!RatingScale.IsValid(request.Rating, style))
        {
            return Common.Invalid("rating", RatingScale.Rule(style));
        }
        return await EntryResult(Watching.ChangeEntry(db, userId, titleId, e =>
        {
            e.Rating = request.Rating;
            e.RatingStyle = request.Rating is null ? null : style;
            if (request.Rating is not null)
            {
                Watching.Mark(e, true);
            }
        }, ct));
    }

    private static Task<IResult> SetSeasonWatched(int seasonId, FlagRequest request, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        SeasonResult(Watching.ChangeSeason(db, user.UserId(), seasonId, e => Watching.Mark(e, request.Value), ct));

    private static async Task<IResult> SetSeasonRating(int seasonId, RatingRequest request, ClaimsPrincipal user, AppDbContext db, CancellationToken ct)
    {
        var userId = user.UserId();
        var style = request.Style ?? await ProfileStyle(db, userId, ct);
        if (!RatingScale.IsValid(request.Rating, style))
        {
            return Common.Invalid("rating", RatingScale.Rule(style));
        }
        return await SeasonResult(Watching.ChangeSeason(db, userId, seasonId, e =>
        {
            e.Rating = request.Rating;
            e.RatingStyle = request.Rating is null ? null : style;
            if (request.Rating is not null)
            {
                Watching.Mark(e, true);
            }
        }, ct));
    }

    private static Task<RatingStyle> ProfileStyle(AppDbContext db, int userId, CancellationToken ct) =>
        db.Users.Where(u => u.Id == userId).Select(u => u.RatingStyle).FirstAsync(ct);

    private static async Task<IResult> EntryResult(Task<LibraryEntry?> change) =>
        await change is { } entry ? Results.Ok(ToDto(entry)) : Results.NotFound();

    private static async Task<IResult> SeasonResult(Task<(Season Season, SeasonEntry Entry, bool TitleWatched)?> change) =>
        await change is { } result
            ? Results.Ok(new SeasonChangeDto(
                new SeasonEntryDto(
                    result.Season.Id,
                    result.Season.SeasonNumber,
                    result.Entry.Watched,
                    result.Entry.WatchedAt,
                    result.Entry.Rating,
                    result.Entry.RatingStyle),
                result.TitleWatched))
            : Results.NotFound();

    private static EntryDto ToDto(LibraryEntry entry) =>
        new(entry.TitleId, entry.InWatchlist, entry.Watched, entry.WatchedAt, entry.Rating, entry.RatingStyle);
}
