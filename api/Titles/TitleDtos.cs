using Maratona.Api.Data.Entities;

namespace Maratona.Api.Titles;

public record RatingsDto(decimal? Imdb, int? ImdbVotes, int? RottenTomatoes, int? Metacritic, DateTime AtualizadoEm);

public record AwardBadgeDto(AwardKind Kind, string Status);

public record AwardDto(AwardKind Kind, int Year, string Category, string? Person, bool Won, bool IsMajor);

public record TitleCardDto(
    int TmdbId,
    MediaType MediaType,
    string Name,
    int? Year,
    string? PosterPath,
    RatingsDto? Ratings,
    AwardBadgeDto? Award);

public record CastDto(string Name, string? Character, string? ProfilePath);

public record CreditsDto(List<string> Directors, List<string> Creators, List<string> Writers, List<CastDto> Cast);

public record SeasonSummaryDto(int SeasonNumber, string Name, string? PosterPath, DateOnly? AirDate, int EpisodeCount);

public record TitleDetailsDto(
    int Id,
    int TmdbId,
    MediaType MediaType,
    string? ImdbId,
    string Name,
    string? OriginalName,
    string? Overview,
    string? PosterPath,
    string? BackdropPath,
    DateOnly? ReleaseDate,
    DateOnly? LastAirDate,
    int? Year,
    int? Runtime,
    string? Status,
    List<string> Genres,
    CreditsDto Credits,
    List<SeasonSummaryDto> Seasons,
    RatingsDto? Ratings,
    AwardBadgeDto? Award,
    List<AwardDto> Awards,
    DateTime AtualizadoEm);

public record EpisodeDto(int Id, int EpisodeNumber, string Name, string? Overview, string? StillPath, DateOnly? AirDate, int? Runtime);

public record SeasonDetailsDto(
    int Id,
    int SeasonNumber,
    string Name,
    string? Overview,
    string? PosterPath,
    DateOnly? AirDate,
    List<EpisodeDto> Episodes);

public record SearchResultDto(List<TitleCardDto> Movies, List<TitleCardDto> Series);

public record HomeDto(
    List<TitleCardDto> NowPlaying,
    List<TitleCardDto> PopularMovies,
    List<TitleCardDto> OnTheAir,
    List<TitleCardDto> PopularSeries);
