using Maratona.Api.Data;
using Maratona.Api.Data.Entities;
using Maratona.Api.Tmdb;
using Microsoft.EntityFrameworkCore;

namespace Maratona.Api.Titles;

public class TitleService(AppDbContext db, TmdbClient tmdb, ILogger<TitleService> logger)
{
    public static readonly TimeSpan CacheAge = TimeSpan.FromDays(7);

    public async Task<Title?> Get(MediaType type, int tmdbId, CancellationToken ct)
    {
        var title = await FullQuery().FirstOrDefaultAsync(t => t.MediaType == type && t.TmdbId == tmdbId, ct);
        if (title is not null && title.AtualizadoEm > DateTime.UtcNow - CacheAge)
        {
            return title;
        }
        try
        {
            return await Sync(type, tmdbId, title, ct);
        }
        catch (HttpRequestException ex) when (title is not null)
        {
            logger.LogWarning(ex, "Falha ao atualizar {Type} {TmdbId} no TMDB, usando cache", type, tmdbId);
            return title;
        }
    }

    public async Task Refresh(MediaType type, int tmdbId, CancellationToken ct)
    {
        var title = await db.Titles
            .Include(t => t.Seasons)
            .FirstOrDefaultAsync(t => t.MediaType == type && t.TmdbId == tmdbId, ct);
        await Sync(type, tmdbId, title, ct);
    }

    public async Task<Season?> GetSeason(int tmdbId, int number, CancellationToken ct)
    {
        var title = await Get(MediaType.Tv, tmdbId, ct);
        if (title is null)
        {
            return null;
        }
        var season = await SeasonQuery(title.Id, number, ct);
        if (season is null || season.AtualizadoEm > DateTime.UtcNow - CacheAge)
        {
            return season;
        }
        try
        {
            var data = await tmdb.GetSeason(tmdbId, number, ct);
            if (data is null)
            {
                return season;
            }
            season.Name = data.Name;
            season.Overview = data.Overview;
            season.PosterPath = data.PosterPath;
            season.AirDate = TitleMapper.ParseDate(data.AirDate);
            foreach (var item in data.Episodes ?? [])
            {
                var episode = season.Episodes.FirstOrDefault(e => e.EpisodeNumber == item.EpisodeNumber);
                if (episode is null)
                {
                    episode = new Episode { EpisodeNumber = item.EpisodeNumber };
                    season.Episodes.Add(episode);
                }
                episode.Name = item.Name;
                episode.Overview = item.Overview;
                episode.StillPath = item.StillPath;
                episode.AirDate = TitleMapper.ParseDate(item.AirDate);
                episode.Runtime = item.Runtime is > 0 ? item.Runtime : null;
            }
            season.AtualizadoEm = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
        catch (HttpRequestException ex) when (season.Episodes.Count > 0)
        {
            logger.LogWarning(ex, "Falha ao atualizar temporada {Number} de {TmdbId}, usando cache", number, tmdbId);
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            season = await SeasonQuery(title.Id, number, ct);
        }
        return season;
    }

    public async Task<List<TitleCardDto>> ToCards(MediaType type, IEnumerable<TmdbListItem> items, CancellationToken ct)
    {
        var list = items.ToList();
        var ids = list.Select(i => i.Id).ToList();
        var cached = await db.Titles
            .AsNoTracking()
            .Include(t => t.ExternalRating)
            .Include(t => t.Awards)
            .Where(t => t.MediaType == type && ids.Contains(t.TmdbId))
            .ToDictionaryAsync(t => t.TmdbId, ct);
        return list.Select(i => TitleMapper.ToCard(type, i, cached.GetValueOrDefault(i.Id))).ToList();
    }

    private IQueryable<Title> FullQuery() => db.Titles
        .Include(t => t.Seasons)
        .Include(t => t.ExternalRating)
        .Include(t => t.Awards)
        .AsSplitQuery();

    private Task<Season?> SeasonQuery(int titleId, int number, CancellationToken ct) => db.Seasons
        .Include(s => s.Episodes)
        .FirstOrDefaultAsync(s => s.TitleId == titleId && s.SeasonNumber == number, ct);

    private async Task<Title?> Sync(MediaType type, int tmdbId, Title? title, CancellationToken ct)
    {
        var isNew = title is null;
        title ??= new Title { TmdbId = tmdbId, MediaType = type };
        var found = type == MediaType.Movie ? await FillMovie(title, ct) : await FillTv(title, ct);
        if (!found && isNew)
        {
            return null;
        }
        if (found && string.IsNullOrWhiteSpace(title.Overview))
        {
            title.Overview = await tmdb.GetEnglishOverview(type, tmdbId, ct);
        }
        title.AtualizadoEm = DateTime.UtcNow;
        if (isNew)
        {
            db.Titles.Add(title);
        }
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException) when (isNew)
        {
            db.ChangeTracker.Clear();
            return await FullQuery().FirstOrDefaultAsync(t => t.MediaType == type && t.TmdbId == tmdbId, ct);
        }
        return title;
    }

    private async Task<bool> FillMovie(Title title, CancellationToken ct)
    {
        var movie = await tmdb.GetMovie(title.TmdbId, ct);
        if (movie is null)
        {
            return false;
        }
        var crew = movie.Credits?.Crew ?? [];
        title.Name = movie.Title;
        title.OriginalName = movie.OriginalTitle;
        title.Overview = movie.Overview;
        title.PosterPath = movie.PosterPath;
        title.BackdropPath = movie.BackdropPath;
        title.ReleaseDate = TitleMapper.ParseDate(movie.ReleaseDate);
        title.Runtime = movie.Runtime is > 0 ? movie.Runtime : null;
        title.Status = movie.Status;
        title.Popularity = movie.Popularity;
        title.Genres = (movie.Genres ?? []).Select(g => g.Name).ToList();
        title.ImdbId = string.IsNullOrWhiteSpace(movie.ImdbId) ? null : movie.ImdbId;
        title.Credits.Directors = crew.Where(c => c.Job == "Director").Select(c => c.Name).Distinct().Take(3).ToList();
        title.Credits.Writers = crew.Where(c => c.Department == "Writing").Select(c => c.Name).Distinct().Take(4).ToList();
        title.Credits.Creators = [];
        title.Credits.Cast = ToCast(movie.Credits);
        return true;
    }

    private async Task<bool> FillTv(Title title, CancellationToken ct)
    {
        var tv = await tmdb.GetTv(title.TmdbId, ct);
        if (tv is null)
        {
            return false;
        }
        title.Name = tv.Name;
        title.OriginalName = tv.OriginalName;
        title.Overview = tv.Overview;
        title.PosterPath = tv.PosterPath;
        title.BackdropPath = tv.BackdropPath;
        title.ReleaseDate = TitleMapper.ParseDate(tv.FirstAirDate);
        title.LastAirDate = TitleMapper.ParseDate(tv.LastAirDate);
        var runtime = tv.EpisodeRunTime?.FirstOrDefault(r => r > 0);
        title.Runtime = runtime > 0 ? runtime : null;
        title.Status = tv.Status;
        title.Popularity = tv.Popularity;
        title.Genres = (tv.Genres ?? []).Select(g => g.Name).ToList();
        title.ImdbId = string.IsNullOrWhiteSpace(tv.ExternalIds?.ImdbId) ? null : tv.ExternalIds.ImdbId;
        title.Credits.Directors = [];
        title.Credits.Writers = [];
        title.Credits.Creators = (tv.CreatedBy ?? []).Select(c => c.Name).Distinct().ToList();
        title.Credits.Cast = ToCast(tv.Credits);
        foreach (var item in tv.Seasons ?? [])
        {
            var season = title.Seasons.FirstOrDefault(s => s.SeasonNumber == item.SeasonNumber);
            if (season is null)
            {
                season = new Season { SeasonNumber = item.SeasonNumber };
                title.Seasons.Add(season);
            }
            else if (season.EpisodeCount != item.EpisodeCount)
            {
                season.AtualizadoEm = null;
            }
            season.Name = item.Name;
            season.Overview = item.Overview;
            season.PosterPath = item.PosterPath;
            season.AirDate = TitleMapper.ParseDate(item.AirDate);
            season.EpisodeCount = item.EpisodeCount;
        }
        return true;
    }

    private static List<CastMember> ToCast(TmdbCredits? credits) => (credits?.Cast ?? [])
        .OrderBy(c => c.Order)
        .Take(5)
        .Select(c => new CastMember { Name = c.Name, Character = c.Character, ProfilePath = c.ProfilePath })
        .ToList();
}
