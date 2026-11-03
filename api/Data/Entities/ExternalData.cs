namespace Maratona.Api.Data.Entities;

public class ExternalRating
{
    public int TitleId { get; set; }
    public Title Title { get; set; } = null!;
    public decimal? Imdb { get; set; }
    public int? ImdbVotes { get; set; }
    public int? RottenTomatoes { get; set; }
    public int? Metacritic { get; set; }
    public bool NotFound { get; set; }
    public DateTime AtualizadoEm { get; set; }
    public DateTime NextRefreshAt { get; set; }
}

public class OmdbQueueItem
{
    public int TitleId { get; set; }
    public Title Title { get; set; } = null!;
    public int RequestCount { get; set; }
    public DateTime LastRequestedAt { get; set; }
}

public class ApiUsage
{
    public DateOnly Date { get; set; }
    public string Bucket { get; set; } = "";
    public int Calls { get; set; }
}

public class JobRun
{
    public string Name { get; set; } = "";
    public DateTime LastRunAt { get; set; }
}

public class Award
{
    public int Id { get; set; }
    public int TitleId { get; set; }
    public Title Title { get; set; } = null!;
    public AwardKind Kind { get; set; }
    public int Year { get; set; }
    public string Category { get; set; } = "";
    public string? Person { get; set; }
    public bool Won { get; set; }
    public bool IsMajor { get; set; }
}
