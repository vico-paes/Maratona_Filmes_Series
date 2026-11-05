using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Maratona.Api.Auth;
using Maratona.Api.Data;
using Maratona.Api.Data.Entities;
using Maratona.Api.Infrastructure;
using Maratona.Api.Library;
using Maratona.Api.Reviews;
using Maratona.Api.Titles;
using Microsoft.EntityFrameworkCore;

namespace Maratona.Api.Profiles;

public record ProfileRequest(
    [Required, StringLength(60, MinimumLength = 1)] string DisplayName,
    [StringLength(500)] string? Bio,
    [StringLength(500)] string? AvatarUrl,
    [StringLength(500)] string? CoverUrl,
    RatingStyle RatingStyle,
    bool ConvertRatings = false);

public record ConversionExampleDto(string Title, int? SeasonNumber, decimal From, RatingStyle FromStyle, decimal To, RatingStyle ToStyle);

public record ConversionPreviewDto(RatingStyle To, string Rule, int Count, List<ConversionExampleDto> Examples, List<ConversionExampleDto> Samples);

public record FavoriteItem([Range(1, int.MaxValue)] int TitleId, bool Crown);

public record FavoritesRequest([MaxLength(4)] List<FavoriteItem> Movies, [MaxLength(4)] List<FavoriteItem> Series);

public record FavoriteDto(int Slot, bool Crown, int TitleId, TitleCardDto Title);

public record WatchedItemDto(int TitleId, TitleCardDto Title, DateTime? WatchedAt, decimal? Rating, RatingStyle? RatingStyle);

public record StatsDto(int MoviesWatched, int SeriesCompleted, int SeasonsWatched, int HoursWatched, int Reviews, int Lists);

public record ProfileDto(
    string Username,
    string DisplayName,
    string? Bio,
    string? AvatarUrl,
    string? CoverUrl,
    RatingStyle RatingStyle,
    DateTime CreatedAt,
    List<FavoriteDto> FavoriteMovies,
    List<FavoriteDto> FavoriteSeries,
    List<WatchedItemDto> RecentlyWatched,
    List<ReviewDto> RecentReviews,
    StatsDto Stats,
    int Followers,
    int Following,
    bool IsMe,
    bool IsFollowing);

public static class ProfileEndpoints
{
    private const int PageSize = 30;

    public static void MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/users/search", Search);
        app.MapGet("/users/{username}", Profile);
        app.MapGet("/users/{username}/followers", Followers);
        app.MapGet("/users/{username}/following", Following);

        var follow = app.MapGroup("/users/{username}/follow").RequireAuthorization();
        follow.MapPost("", Follow);
        follow.MapDelete("", Unfollow);

        var me = app.MapGroup("/me").RequireAuthorization();
        me.MapPut("/profile", UpdateProfile);
        me.MapGet("/rating-conversion", ConversionPreview);
        me.MapPut("/favorites", UpdateFavorites);
    }

    private static async Task<IResult> Search(
        [Required, StringLength(50, MinimumLength = 1)] string? q,
        AppDbContext db,
        CancellationToken ct)
    {
        var term = q?.Trim().ToLowerInvariant() ?? "";
        if (term.Length == 0)
        {
            return Common.Invalid("q", "Informe o que deseja buscar.");
        }
        var users = await db.Users
            .Where(u => u.Username.Contains(term) || u.DisplayName.ToLower().Contains(term))
            .OrderBy(u => u.Username)
            .Take(PageSize)
            .Select(u => new UserSummaryDto(u.Username, u.DisplayName, u.AvatarUrl))
            .ToListAsync(ct);
        return Results.Ok(users);
    }

    private static async Task<IResult> Profile(string username, ClaimsPrincipal principal, AppDbContext db, TitleService titles, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username.ToLower(), ct);
        if (user is null)
        {
            return Results.NotFound();
        }
        var viewerId = principal.UserIdOrNull();
        var favorites = await db.Favorites.Where(f => f.UserId == user.Id).OrderBy(f => f.Slot).ToListAsync(ct);
        var recent = await db.LibraryEntries
            .Where(e => e.UserId == user.Id && e.Watched)
            .OrderByDescending(e => e.WatchedAt)
            .Take(12)
            .Select(e => new { e.TitleId, e.WatchedAt, e.Rating, e.RatingStyle })
            .ToListAsync(ct);
        var cards = await titles.Cards(favorites.Select(f => f.TitleId).Concat(recent.Select(e => e.TitleId)), ct);
        var reviews = await ReviewQueries.Load(
            db,
            titles,
            db.Reviews.Where(r => r.UserId == user.Id).OrderByDescending(r => r.CreatedAt).Take(5),
            viewerId,
            includeTitle: true,
            ct);
        return Results.Ok(new ProfileDto(
            user.Username,
            user.DisplayName,
            user.Bio,
            user.AvatarUrl,
            user.CoverUrl,
            user.RatingStyle,
            user.CreatedAt,
            Favorites(favorites, MediaType.Movie, cards),
            Favorites(favorites, MediaType.Tv, cards),
            recent.Select(e => new WatchedItemDto(e.TitleId, cards[e.TitleId], e.WatchedAt, e.Rating, e.RatingStyle)).ToList(),
            reviews,
            await Stats(db, user.Id, ct),
            await db.Follows.CountAsync(f => f.FollowedId == user.Id, ct),
            await db.Follows.CountAsync(f => f.FollowerId == user.Id, ct),
            viewerId == user.Id,
            viewerId is not null && await db.Follows.AnyAsync(f => f.FollowerId == viewerId && f.FollowedId == user.Id, ct)));
    }

    private static List<FavoriteDto> Favorites(List<Favorite> favorites, MediaType type, Dictionary<int, TitleCardDto> cards) => favorites
        .Where(f => f.MediaType == type)
        .Select(f => new FavoriteDto(f.Slot, f.Crown, f.TitleId, cards[f.TitleId]))
        .ToList();

    private static async Task<StatsDto> Stats(AppDbContext db, int userId, CancellationToken ct)
    {
        var watched = db.LibraryEntries.Where(e => e.UserId == userId && e.Watched);
        var seasons = db.SeasonEntries.Where(e => e.UserId == userId && e.Watched);
        var movieMinutes = await watched
            .Where(e => e.Title.MediaType == MediaType.Movie)
            .SumAsync(e => e.Title.Runtime ?? 0, ct);
        var seriesMinutes = await seasons
            .SumAsync(e => e.Season.EpisodeCount * (e.Season.Title.Runtime ?? 0), ct);
        return new StatsDto(
            await watched.CountAsync(e => e.Title.MediaType == MediaType.Movie, ct),
            await watched.CountAsync(e => e.Title.MediaType == MediaType.Tv, ct),
            await seasons.CountAsync(e => e.Season.SeasonNumber > 0, ct),
            (movieMinutes + seriesMinutes) / 60,
            await db.Reviews.CountAsync(r => r.UserId == userId, ct),
            await db.Lists.CountAsync(l => l.UserId == userId && l.IsPublic, ct));
    }

    private static Task<IResult> Followers(string username, AppDbContext db, CancellationToken ct, [Range(1, 1000)] int page = 1) =>
        People(db, username, page, (u, follows) => follows.Where(f => f.FollowedId == u).OrderByDescending(f => f.CreatedAt).Select(f => f.Follower), ct);

    private static Task<IResult> Following(string username, AppDbContext db, CancellationToken ct, [Range(1, 1000)] int page = 1) =>
        People(db, username, page, (u, follows) => follows.Where(f => f.FollowerId == u).OrderByDescending(f => f.CreatedAt).Select(f => f.Followed), ct);

    private static async Task<IResult> People(
        AppDbContext db,
        string username,
        int page,
        Func<int, IQueryable<Follow>, IQueryable<User>> select,
        CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username.ToLower(), ct);
        if (user is null)
        {
            return Results.NotFound();
        }
        var query = select(user.Id, db.Follows);
        var total = await query.CountAsync(ct);
        var people = await query
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(u => new UserSummaryDto(u.Username, u.DisplayName, u.AvatarUrl))
            .ToListAsync(ct);
        return Results.Ok(new PageDto<UserSummaryDto>(people, page, Common.TotalPages(total, PageSize), total));
    }

    private static async Task<IResult> Follow(string username, ClaimsPrincipal principal, AppDbContext db, CancellationToken ct)
    {
        var target = await db.Users.FirstOrDefaultAsync(u => u.Username == username.ToLower(), ct);
        if (target is null)
        {
            return Results.NotFound();
        }
        var userId = principal.UserId();
        if (target.Id == userId)
        {
            return Common.Invalid("username", "Você não pode seguir a si mesmo.");
        }
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO follows (follower_id, followed_id, created_at) VALUES ({userId}, {target.Id}, {DateTime.UtcNow})
            ON CONFLICT DO NOTHING
            """, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> Unfollow(string username, ClaimsPrincipal principal, AppDbContext db, CancellationToken ct)
    {
        var userId = principal.UserId();
        var name = username.ToLower();
        await db.Follows.Where(f => f.FollowerId == userId && f.Followed.Username == name).ExecuteDeleteAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> UpdateProfile(ProfileRequest request, ClaimsPrincipal principal, AppDbContext db, CancellationToken ct)
    {
        if (!Common.IsHttpsUrl(request.AvatarUrl))
        {
            return Common.Invalid("avatarUrl", "Use um link https válido.");
        }
        if (!Common.IsHttpsUrl(request.CoverUrl))
        {
            return Common.Invalid("coverUrl", "Use um link https válido.");
        }
        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return Common.Invalid("displayName", "Informe um nome.");
        }
        var user = await db.Users.FindAsync([principal.UserId()], ct);
        if (user is null)
        {
            return Results.Unauthorized();
        }
        user.DisplayName = request.DisplayName.Trim();
        user.Bio = string.IsNullOrWhiteSpace(request.Bio) ? null : request.Bio.Trim();
        user.AvatarUrl = string.IsNullOrWhiteSpace(request.AvatarUrl) ? null : request.AvatarUrl.Trim();
        user.CoverUrl = string.IsNullOrWhiteSpace(request.CoverUrl) ? null : request.CoverUrl.Trim();
        user.RatingStyle = request.RatingStyle;
        if (request.ConvertRatings)
        {
            var to = request.RatingStyle;
            var entries = await db.LibraryEntries
                .Where(e => e.UserId == user.Id && e.Rating != null && e.RatingStyle != to)
                .ToListAsync(ct);
            foreach (var entry in entries)
            {
                entry.Rating = RatingScale.Convert(entry.Rating!.Value, entry.RatingStyle!.Value, to);
                entry.RatingStyle = to;
            }
            var seasons = await db.SeasonEntries
                .Where(e => e.UserId == user.Id && e.Rating != null && e.RatingStyle != to)
                .ToListAsync(ct);
            foreach (var entry in seasons)
            {
                entry.Rating = RatingScale.Convert(entry.Rating!.Value, entry.RatingStyle!.Value, to);
                entry.RatingStyle = to;
            }
        }
        await db.SaveChangesAsync(ct);
        return Results.Ok(AuthEndpoints.ToMe(user));
    }

    private static async Task<IResult> ConversionPreview(
        [Required, AllowedValues("stars", "numeric")] string? to,
        ClaimsPrincipal principal,
        AppDbContext db,
        CancellationToken ct)
    {
        var target = to == "numeric" ? RatingStyle.Numeric : RatingStyle.Stars;
        var from = target == RatingStyle.Stars ? RatingStyle.Numeric : RatingStyle.Stars;
        var userId = principal.UserId();
        var titleRatings = db.LibraryEntries
            .Where(e => e.UserId == userId && e.Rating != null && e.RatingStyle == from)
            .OrderByDescending(e => e.UpdatedAt)
            .Select(e => new { e.Title.Name, SeasonNumber = (int?)null, Rating = e.Rating!.Value });
        var seasonRatings = db.SeasonEntries
            .Where(e => e.UserId == userId && e.Rating != null && e.RatingStyle == from)
            .OrderByDescending(e => e.UpdatedAt)
            .Select(e => new { e.Season.Title.Name, SeasonNumber = (int?)e.Season.SeasonNumber, Rating = e.Rating!.Value });
        var count = await titleRatings.CountAsync(ct) + await seasonRatings.CountAsync(ct);
        var examples = (await titleRatings.Take(3).ToListAsync(ct))
            .Concat(await seasonRatings.Take(3).ToListAsync(ct))
            .Take(3)
            .Select(r => new ConversionExampleDto(r.Name, r.SeasonNumber, r.Rating, from, RatingScale.Convert(r.Rating, from, target), target))
            .ToList();
        decimal[] sampleValues = from == RatingStyle.Numeric ? [7, 8.5m, 0] : [3.5m, 5, 0.5m];
        var samples = sampleValues
            .Select(v => new ConversionExampleDto("Exemplo", null, v, from, RatingScale.Convert(v, from, target), target))
            .ToList();
        var rule = target == RatingStyle.Stars
            ? "A nota é dividida por 2 e arredondada para a meia estrela mais próxima. Por exemplo, 7 vira 3,5 estrelas, 8,5 vira 4,5 estrelas e 0 vira meia estrela, que é o mínimo."
            : "A quantidade de estrelas é multiplicada por 2. Por exemplo, 3,5 estrelas vira 7 e 5 estrelas vira 10.";
        return Results.Ok(new ConversionPreviewDto(target, rule, count, examples, samples));
    }

    private static async Task<IResult> UpdateFavorites(FavoritesRequest request, ClaimsPrincipal principal, AppDbContext db, CancellationToken ct)
    {
        var groups = new[] { (Type: MediaType.Movie, Field: "movies", Items: request.Movies ?? []), (Type: MediaType.Tv, Field: "series", Items: request.Series ?? []) };
        foreach (var group in groups)
        {
            if (group.Items.Count > 4)
            {
                return Common.Invalid(group.Field, "Escolha no máximo 4.");
            }
            if (group.Items.Select(i => i.TitleId).Distinct().Count() != group.Items.Count)
            {
                return Common.Invalid(group.Field, "Não repita títulos.");
            }
            if (group.Items.Count(i => i.Crown) > 1)
            {
                return Common.Invalid(group.Field, "Só um favorito pode ter coroa.");
            }
            var ids = group.Items.Select(i => i.TitleId).ToList();
            var found = await db.Titles.CountAsync(t => ids.Contains(t.Id) && t.MediaType == group.Type, ct);
            if (found != ids.Count)
            {
                return Common.Invalid(group.Field, group.Type == MediaType.Movie ? "Escolha apenas filmes." : "Escolha apenas séries.");
            }
        }
        var userId = principal.UserId();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.Favorites.Where(f => f.UserId == userId).ExecuteDeleteAsync(ct);
        foreach (var group in groups)
        {
            db.Favorites.AddRange(group.Items.Select((item, index) => new Favorite
            {
                UserId = userId,
                MediaType = group.Type,
                Slot = index + 1,
                TitleId = item.TitleId,
                Crown = item.Crown
            }));
        }
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Results.NoContent();
    }
}
