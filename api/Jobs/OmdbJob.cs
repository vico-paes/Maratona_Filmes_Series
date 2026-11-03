using Maratona.Api.Data;
using Maratona.Api.Data.Entities;
using Maratona.Api.Omdb;
using Microsoft.EntityFrameworkCore;

namespace Maratona.Api.Jobs;

public class OmdbJob(IServiceScopeFactory scopes, ILogger<OmdbJob> logger) : DailyJob(scopes, logger)
{
    protected override string Name => "omdb";

    protected override async Task Run(IServiceProvider services, CancellationToken ct)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var omdb = services.GetRequiredService<OmdbService>();
        if (!omdb.Enabled)
        {
            return;
        }
        var limit = omdb.Limit(OmdbService.JobBucket);
        var now = DateTime.UtcNow;

        var queued = await db.OmdbQueue
            .OrderByDescending(q => q.RequestCount)
            .ThenBy(q => q.LastRequestedAt)
            .Select(q => q.TitleId)
            .Take(limit)
            .ToListAsync(ct);

        var renewals = await db.ExternalRatings
            .Where(r => r.NextRefreshAt <= now)
            .OrderBy(r => r.AtualizadoEm)
            .Select(r => r.TitleId)
            .Take(limit)
            .ToListAsync(ct);

        var missing = await db.Titles
            .Where(t => t.ImdbId != null && t.ExternalRating == null)
            .OrderByDescending(t => t.Popularity)
            .Select(t => t.Id)
            .Take(limit)
            .ToListAsync(ct);

        var saved = 0;
        foreach (var titleId in queued.Concat(renewals).Concat(missing).Distinct())
        {
            var title = await db.Titles.Include(t => t.ExternalRating).FirstOrDefaultAsync(t => t.Id == titleId, ct);
            if (title?.ImdbId is null)
            {
                await db.OmdbQueue.Where(q => q.TitleId == titleId).ExecuteDeleteAsync(ct);
                continue;
            }
            if (!await omdb.TryReserve(OmdbService.JobBucket, ct))
            {
                break;
            }
            try
            {
                if (await omdb.Fetch(title, ct) == FetchOutcome.LimitReached)
                {
                    break;
                }
                saved++;
            }
            catch (Exception ex) when (ex is HttpRequestException or DbUpdateException)
            {
                logger.LogWarning(ex, "Falha ao buscar notas de {ImdbId}", title.ImdbId);
            }
            db.ChangeTracker.Clear();
        }
        logger.LogInformation("OMDb: {Saved} títulos atualizados ({Queued} na fila, {Renewals} para renovar, {Missing} sem nota)",
            saved, queued.Count, renewals.Count, missing.Count);
    }
}
