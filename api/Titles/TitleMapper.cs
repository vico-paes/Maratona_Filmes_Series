using System.Globalization;
using Maratona.Api.Data.Entities;
using Maratona.Api.Tmdb;

namespace Maratona.Api.Titles;

public static class TitleMapper
{
    public static DateOnly? ParseDate(string? value) =>
        DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : null;

    public static RatingsDto? ToRatings(ExternalRating? rating) =>
        rating is null || rating.NotFound
            ? null
            : new RatingsDto(rating.Imdb, rating.ImdbVotes, rating.RottenTomatoes, rating.Metacritic, rating.AtualizadoEm);

    public static AwardBadgeDto? ToBadge(MediaType type, IEnumerable<Award> awards)
    {
        var kind = type == MediaType.Movie ? AwardKind.Oscar : AwardKind.Emmy;
        var list = awards.Where(a => a.Kind == kind).ToList();
        if (list.Count == 0)
        {
            return null;
        }
        var status = list.Any(a => a.Won && a.IsMajor) ? "major" : list.Any(a => a.Won) ? "won" : "nominated";
        return new AwardBadgeDto(kind, status);
    }

    public static TitleCardDto ToCard(Title title) => new(
        title.TmdbId,
        title.MediaType,
        title.Name,
        title.ReleaseDate?.Year,
        title.PosterPath,
        ToRatings(title.ExternalRating),
        ToBadge(title.MediaType, title.Awards));

    public static TitleCardDto ToCard(MediaType type, TmdbListItem item, Title? cached) => new(
        item.Id,
        type,
        (type == MediaType.Movie ? item.Title : item.Name) ?? "",
        ParseDate(type == MediaType.Movie ? item.ReleaseDate : item.FirstAirDate)?.Year,
        item.PosterPath,
        ToRatings(cached?.ExternalRating),
        cached is null ? null : ToBadge(type, cached.Awards));

    public static TitleDetailsDto ToDetails(Title title) => new(
        title.Id,
        title.TmdbId,
        title.MediaType,
        title.ImdbId,
        title.Name,
        title.OriginalName,
        title.Overview,
        title.PosterPath,
        title.BackdropPath,
        title.ReleaseDate,
        title.LastAirDate,
        title.ReleaseDate?.Year,
        title.Runtime,
        title.Status,
        title.Genres,
        new CreditsDto(
            title.Credits.Directors,
            title.Credits.Creators,
            title.Credits.Writers,
            title.Credits.Cast.Select(c => new CastDto(c.Name, c.Character, c.ProfilePath)).ToList()),
        title.Seasons
            .OrderBy(s => s.SeasonNumber)
            .Select(s => new SeasonSummaryDto(s.SeasonNumber, s.Name, s.PosterPath, s.AirDate, s.EpisodeCount))
            .ToList(),
        ToRatings(title.ExternalRating),
        ToBadge(title.MediaType, title.Awards),
        title.Awards
            .OrderByDescending(a => a.Year)
            .ThenByDescending(a => a.IsMajor)
            .Select(a => new AwardDto(a.Kind, a.Year, a.Category, a.Person, a.Won, a.IsMajor))
            .ToList(),
        title.AtualizadoEm);

    public static SeasonDetailsDto ToDetails(Season season) => new(
        season.Id,
        season.SeasonNumber,
        season.Name,
        season.Overview,
        season.PosterPath,
        season.AirDate,
        season.Episodes
            .OrderBy(e => e.EpisodeNumber)
            .Select(e => new EpisodeDto(e.Id, e.EpisodeNumber, e.Name, e.Overview, e.StillPath, e.AirDate, e.Runtime))
            .ToList());
}
