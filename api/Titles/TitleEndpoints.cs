using System.ComponentModel.DataAnnotations;
using Maratona.Api.Data.Entities;
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

    private static async Task<IResult> Movie([Range(1, int.MaxValue)] int tmdbId, TitleService service, CancellationToken ct)
    {
        var title = await service.Get(MediaType.Movie, tmdbId, ct);
        return title is null ? Results.NotFound() : Results.Ok(TitleMapper.ToDetails(title));
    }

    private static async Task<IResult> Tv([Range(1, int.MaxValue)] int tmdbId, TitleService service, CancellationToken ct)
    {
        var title = await service.Get(MediaType.Tv, tmdbId, ct);
        return title is null ? Results.NotFound() : Results.Ok(TitleMapper.ToDetails(title));
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
