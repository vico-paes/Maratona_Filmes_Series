using Maratona.Api.Data;
using Maratona.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Maratona.Api.Omdb;

public enum FetchOutcome
{
    Saved,
    LimitReached
}

public class OmdbService(AppDbContext db, IServiceProvider services, IConfiguration config, ILogger<OmdbService> logger)
{
    public const string DemandBucket = "omdb-demand";
    public const string JobBucket = "omdb-job";
    private static readonly TimeSpan DemandTimeout = TimeSpan.FromSeconds(5);

    public bool Enabled =>
        !string.Equals(config["OMDB_ENABLED"], "false", StringComparison.OrdinalIgnoreCase)
        && !string.IsNullOrWhiteSpace(config["OMDB_API_KEY"]);

    public int Limit(string bucket) => bucket == DemandBucket
        ? config.GetValue("OMDB_DAILY_LIMIT_DEMAND", 700)
        : config.GetValue("OMDB_DAILY_LIMIT_JOB", 300);

    public async Task OnTitleOpened(Title title, CancellationToken ct)
    {
        if (!Enabled || title.ImdbId is null || title.ExternalRating is not null)
        {
            return;
        }
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO omdb_queue (title_id, request_count, last_requested_at)
            VALUES ({title.Id}, 1, {DateTime.UtcNow})
            ON CONFLICT (title_id) DO UPDATE
            SET request_count = omdb_queue.request_count + 1, last_requested_at = excluded.last_requested_at
            """, ct);
        if (!await TryReserve(DemandBucket, ct))
        {
            return;
        }
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(DemandTimeout);
        try
        {
            await Fetch(title, timeout.Token);
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or DbUpdateException && !ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Falha ao buscar notas de {ImdbId} sob demanda", title.ImdbId);
        }
    }

    public async Task<bool> TryReserve(string bucket, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO api_usage (date, bucket, calls) VALUES ({today}, {bucket}, 0) ON CONFLICT DO NOTHING", ct);
        var reserved = await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE api_usage SET calls = calls + 1 WHERE date = {today} AND bucket = {bucket} AND calls < {Limit(bucket)}", ct);
        return reserved == 1;
    }

    public async Task<FetchOutcome> Fetch(Title title, CancellationToken ct)
    {
        var result = await services.GetRequiredService<OmdbClient>().Get(title.ImdbId!, ct);
        if (result.LimitReached)
        {
            await ExhaustToday(ct);
            return FetchOutcome.LimitReached;
        }
        var now = DateTime.UtcNow;
        var rating = title.ExternalRating ?? await db.ExternalRatings.FindAsync([title.Id], ct);
        if (rating is null)
        {
            rating = new ExternalRating { TitleId = title.Id };
            db.ExternalRatings.Add(rating);
        }
        rating.NotFound = !result.Found;
        rating.Imdb = result.Imdb;
        rating.ImdbVotes = result.ImdbVotes;
        rating.RottenTomatoes = result.RottenTomatoes;
        rating.Metacritic = result.Metacritic;
        rating.AwardsText = result.Awards?.Length > 300 ? result.Awards[..300] : result.Awards;
        rating.OscarWins = result.OscarWins;
        rating.OscarNominations = result.OscarNominations;
        rating.EmmyWins = result.EmmyWins;
        rating.EmmyNominations = result.EmmyNominations;
        rating.AtualizadoEm = now;
        rating.NextRefreshAt = now + (result.Found ? RefreshSchedule.Next(title, now) : RefreshSchedule.NotFound);
        title.ExternalRating = rating;
        await db.SaveChangesAsync(ct);
        await db.OmdbQueue.Where(q => q.TitleId == title.Id).ExecuteDeleteAsync(ct);
        return FetchOutcome.Saved;
    }

    private async Task ExhaustToday(CancellationToken ct)
    {
        logger.LogWarning("OMDb informou que a cota do dia acabou");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        foreach (var bucket in new[] { DemandBucket, JobBucket })
        {
            var limit = Limit(bucket);
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO api_usage (date, bucket, calls) VALUES ({today}, {bucket}, {limit})
                ON CONFLICT (date, bucket) DO UPDATE SET calls = GREATEST(api_usage.calls, {limit})
                """, ct);
        }
    }
}
