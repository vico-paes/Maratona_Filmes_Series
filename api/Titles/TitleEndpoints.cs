using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Maratona.Api.Data.Entities;
using Maratona.Api.Infrastructure;
using Maratona.Api.Omdb;
using Maratona.Api.Tmdb;
using Microsoft.Extensions.Caching.Memory;

namespace Maratona.Api.Titles;

public static class TitleEndpoints
{
    private record HomeLists(
        List<TmdbListItem> NowPlaying,
        List<TmdbListItem> PopularMovies,
        List<TmdbListItem> OnTheAir,
        List<TmdbListItem> PopularSeries);

    public static void MapTitleEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/home", Home);
        app.MapGet("/home/{list}", HomeList);

        var titles = app.MapGroup("/titles");
        titles.MapGet("/search", Search);
        titles.MapGet("/movie/{tmdbId:int}", Movie);
        titles.MapGet("/tv/{tmdbId:int}", Tv);
        titles.MapGet("/tv/{tmdbId:int}/seasons/{number:int}", Season);
    }

    private static async Task<IResult> Search(
        [Required, StringLength(100, MinimumLength = 1)] string? q,
        TmdbClient tmdb,
        TitleService service,
        CancellationToken ct,
        [Range(1, 20)] int page = 1)
    {
        var query = q?.Trim() ?? "";
        if (query.Length == 0)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["q"] = ["Informe o que deseja buscar."] });
        }
        var movies = tmdb.Search(MediaType.Movie, query, page, ct);
        var series = tmdb.Search(MediaType.Tv, query, page, ct);
        await Task.WhenAll(movies, series);
        return Results.Ok(new SearchResultDto(
            await service.ToCards(MediaType.Movie, (await movies)?.Results ?? [], ct),
            await service.ToCards(MediaType.Tv, (await series)?.Results ?? [], ct)));
    }

    private static Task<IResult> Movie(
        [Range(1, int.MaxValue)] int tmdbId,
        ClaimsPrincipal user,
        TitleService service,
        OmdbService omdb,
        CancellationToken ct) => Details(MediaType.Movie, tmdbId, user, service, omdb, ct);

    private static Task<IResult> Tv(
        [Range(1, int.MaxValue)] int tmdbId,
        ClaimsPrincipal user,
        TitleService service,
        OmdbService omdb,
        CancellationToken ct) => Details(MediaType.Tv, tmdbId, user, service, omdb, ct);

    private static async Task<IResult> Details(
        MediaType type,
        int tmdbId,
        ClaimsPrincipal user,
        TitleService service,
        OmdbService omdb,
        CancellationToken ct)
    {
        var title = await service.Get(type, tmdbId, ct);
        if (title is null)
        {
            return Results.NotFound();
        }
        if (user.Identity?.IsAuthenticated == true)
        {
            await omdb.OnTitleOpened(title, ct);
        }
        return Results.Ok(TitleMapper.ToDetails(title, await service.AwardsFor(title, ct)));
    }

    private static async Task<IResult> Season(
        [Range(1, int.MaxValue)] int tmdbId,
        [Range(0, 500)] int number,
        TitleService service,
        CancellationToken ct)
    {
        var season = await service.GetSeason(tmdbId, number, ct);
        return season is null ? Results.NotFound() : Results.Ok(TitleMapper.ToDetails(season));
    }

    private const int MaxListPages = 20;

    private static readonly Dictionary<string, (string Path, MediaType Type)> Lists = new()
    {
        ["now-playing"] = ("movie/now_playing", MediaType.Movie),
        ["popular-movies"] = ("movie/popular", MediaType.Movie),
        ["on-the-air"] = ("tv/on_the_air", MediaType.Tv),
        ["popular-series"] = ("tv/popular", MediaType.Tv),
    };

    private static async Task<IResult> HomeList(
        string list,
        TmdbClient tmdb,
        TitleService service,
        IMemoryCache cache,
        CancellationToken ct,
        [Range(1, MaxListPages)] int page = 1)
    {
        if (!Lists.TryGetValue(list, out var source))
        {
            return Results.NotFound();
        }
        var result = await cache.GetOrCreateAsync($"home-list:{list}:{page}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            return await tmdb.GetList(source.Path, page, ct);
        });
        var cards = await service.ToCards(source.Type, result?.Results ?? [], ct);
        var totalPages = Math.Min(result?.TotalPages ?? 0, MaxListPages);
        return Results.Ok(new PageDto<TitleCardDto>(cards, page, totalPages, Math.Min(result?.TotalResults ?? 0, totalPages * 20)));
    }

    private static async Task<IResult> Home(TmdbClient tmdb, TitleService service, IMemoryCache cache, CancellationToken ct)
    {
        var lists = await cache.GetOrCreateAsync("home-lists", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            var nowPlaying = tmdb.GetList("movie/now_playing", 1, ct);
            var popularMovies = tmdb.GetList("movie/popular", 1, ct);
            var onTheAir = tmdb.GetList("tv/on_the_air", 1, ct);
            var popularSeries = tmdb.GetList("tv/popular", 1, ct);
            await Task.WhenAll(nowPlaying, popularMovies, onTheAir, popularSeries);
            return new HomeLists(
                (await nowPlaying)?.Results ?? [],
                (await popularMovies)?.Results ?? [],
                (await onTheAir)?.Results ?? [],
                (await popularSeries)?.Results ?? []);
        });
        return Results.Ok(new HomeDto(
            await service.ToCards(MediaType.Movie, lists!.NowPlaying, ct),
            await service.ToCards(MediaType.Movie, lists.PopularMovies, ct),
            await service.ToCards(MediaType.Tv, lists.OnTheAir, ct),
            await service.ToCards(MediaType.Tv, lists.PopularSeries, ct)));
    }
}
