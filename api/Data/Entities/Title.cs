namespace Maratona.Api.Data.Entities;

public class Title
{
    public int Id { get; set; }
    public int TmdbId { get; set; }
    public MediaType MediaType { get; set; }
    public string? ImdbId { get; set; }
    public string Name { get; set; } = "";
    public string? OriginalName { get; set; }
    public string? Overview { get; set; }
    public string? PosterPath { get; set; }
    public string? BackdropPath { get; set; }
    public DateOnly? ReleaseDate { get; set; }
    public DateOnly? LastAirDate { get; set; }
    public int? Runtime { get; set; }
    public string? Status { get; set; }
    public double Popularity { get; set; }
    public List<string> Genres { get; set; } = [];
    public Credits Credits { get; set; } = new();
    public DateTime AtualizadoEm { get; set; }
    public DateTime? AwardsCheckedAt { get; set; }
    public List<Season> Seasons { get; set; } = [];
    public List<Award> Awards { get; set; } = [];
    public ExternalRating? ExternalRating { get; set; }
}

public class Credits
{
    public List<string> Directors { get; set; } = [];
    public List<string> Creators { get; set; } = [];
    public List<string> Writers { get; set; } = [];
    public List<CastMember> Cast { get; set; } = [];
}

public class CastMember
{
    public string Name { get; set; } = "";
    public string? Character { get; set; }
    public string? ProfilePath { get; set; }
}

public class Season
{
    public int Id { get; set; }
    public int TitleId { get; set; }
    public Title Title { get; set; } = null!;
    public int SeasonNumber { get; set; }
    public string Name { get; set; } = "";
    public string? Overview { get; set; }
    public string? PosterPath { get; set; }
    public DateOnly? AirDate { get; set; }
    public int EpisodeCount { get; set; }
    public DateTime? AtualizadoEm { get; set; }
    public List<Episode> Episodes { get; set; } = [];
}

public class Episode
{
    public int Id { get; set; }
    public int SeasonId { get; set; }
    public Season Season { get; set; } = null!;
    public int EpisodeNumber { get; set; }
    public string Name { get; set; } = "";
    public string? Overview { get; set; }
    public string? StillPath { get; set; }
    public DateOnly? AirDate { get; set; }
    public int? Runtime { get; set; }
}
