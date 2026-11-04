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

    public static AwardBadgeDto? ToBadge(MediaType type, ExternalRating? rating, IEnumerable<Award> awards)
    {
        var kind = type == MediaType.Movie ? AwardKind.Oscar : AwardKind.Emmy;
        var list = awards.Where(a => a.Kind == kind).ToList();
        var omdbWins = (kind == AwardKind.Oscar ? rating?.OscarWins : rating?.EmmyWins) ?? 0;
        var omdbNominations = (kind == AwardKind.Oscar ? rating?.OscarNominations : rating?.EmmyNominations) ?? 0;
        var wins = Math.Max(list.Count(a => a.Won), omdbWins);
        var nominations = Math.Max(Math.Max(list.Count, omdbNominations), wins);
        if (nominations == 0)
        {
            return null;
        }
        var status = wins == 0 ? "nominated" : list.Any(a => a.Won && a.IsMajor) ? "major" : "won";
        return new AwardBadgeDto(kind, status, wins, nominations);
    }

    public static TitleCardDto ToCard(Title title, IEnumerable<Award> awards) => new(
        title.TmdbId,
        title.MediaType,
        title.Name,
        title.ReleaseDate?.Year,
        title.PosterPath,
        ToRatings(title.ExternalRating),
        ToBadge(title.MediaType, title.ExternalRating, awards));

    public static TitleCardDto ToCard(MediaType type, TmdbListItem item, Title? cached, IEnumerable<Award> awards) => new(
        item.Id,
        type,
        (type == MediaType.Movie ? item.Title : item.Name) ?? "",
        ParseDate(type == MediaType.Movie ? item.ReleaseDate : item.FirstAirDate)?.Year,
        item.PosterPath,
        ToRatings(cached?.ExternalRating),
        ToBadge(type, cached?.ExternalRating, awards));

    public static TitleDetailsDto ToDetails(Title title, List<Award> awards) => new(
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
            .Select(s => new SeasonSummaryDto(s.Id, s.SeasonNumber, s.Name, s.PosterPath, s.AirDate, s.EpisodeCount))
            .ToList(),
        ToRatings(title.ExternalRating),
        ToBadge(title.MediaType, title.ExternalRating, awards),
        awards
            .Where(a => a.Kind == (title.MediaType == MediaType.Movie ? AwardKind.Oscar : AwardKind.Emmy))
            .OrderByDescending(a => a.Won)
            .ThenByDescending(a => a.Year)
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
