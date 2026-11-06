namespace Maratona.Api.Tmdb;

public record TmdbPage<T>(int Page, int TotalPages, int TotalResults, List<T>? Results);

public record TmdbListItem(int Id, string? Title, string? Name, string? ReleaseDate, string? FirstAirDate, string? PosterPath, double Popularity);

public record TmdbGenre(int Id, string Name);

public record TmdbCast(string Name, string? Character, string? ProfilePath, int Order);

public record TmdbCrew(string Name, string? Job, string? Department);

public record TmdbCredits(List<TmdbCast>? Cast, List<TmdbCrew>? Crew);

public record TmdbMovie(
    int Id,
    string Title,
    string? OriginalTitle,
    string? Overview,
    string? PosterPath,
    string? BackdropPath,
    string? ReleaseDate,
    int? Runtime,
    string? Status,
    double Popularity,
    List<TmdbGenre>? Genres,
    string? ImdbId,
    TmdbCredits? Credits);

public record TmdbPerson(string Name);

public record TmdbSeasonSummary(int SeasonNumber, string Name, string? Overview, string? PosterPath, string? AirDate, int EpisodeCount);

public record TmdbExternalIds(string? ImdbId);

public record TmdbTv(
    int Id,
    string Name,
    string? OriginalName,
    string? Overview,
    string? PosterPath,
    string? BackdropPath,
    string? FirstAirDate,
    string? LastAirDate,
    List<int>? EpisodeRunTime,
    string? Status,
    double Popularity,
    List<TmdbGenre>? Genres,
    List<TmdbPerson>? CreatedBy,
    List<TmdbSeasonSummary>? Seasons,
    TmdbExternalIds? ExternalIds,
    TmdbCredits? Credits);

public record TmdbEpisode(int EpisodeNumber, string Name, string? Overview, string? StillPath, string? AirDate, int? Runtime);

public record TmdbSeason(int SeasonNumber, string Name, string? Overview, string? PosterPath, string? AirDate, List<TmdbEpisode>? Episodes);

public record TmdbChange(int Id);
