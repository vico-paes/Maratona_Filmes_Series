using Maratona.Api.Data;
using Maratona.Api.Data.Entities;
using Maratona.Api.Tmdb;
using Maratona.Api.Titles;
using Microsoft.EntityFrameworkCore;

namespace Maratona.Api.Jobs;

public class TmdbSyncJob(IServiceScopeFactory scopes, ILogger<TmdbSyncJob> logger) : DailyJob(scopes, logger)
{
    private const int ListPages = 2;
    private const int MaxChangePages = 50;
    private const int StaleBatch = 500;
    private static readonly TimeSpan StaleAge = TimeSpan.FromDays(90);

    private static readonly (string Path, MediaType Type)[] Lists =
    [
        ("movie/now_playing", MediaType.Movie),
        ("movie/upcoming", MediaType.Movie),
        ("movie/popular", MediaType.Movie),
        ("tv/on_the_air", MediaType.Tv),
        ("tv/airing_today", MediaType.Tv),
        ("tv/popular", MediaType.Tv)
    ];

    protected override string Name => "tmdb-sync";

    protected override async Task Run(IServiceProvider services, CancellationToken ct)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var tmdb = services.GetRequiredService<TmdbClient>();
        var titles = services.GetRequiredService<TitleService>();
        var targets = new HashSet<(MediaType Type, int Id)>();

        foreach (var (path, type) in Lists)
        {
            for (var page = 1; page <= ListPages; page++)
            {
                var result = await tmdb.GetList(path, page, ct);
                foreach (var item in result?.Results ?? [])
                {
                    targets.Add((type, item.Id));
                }
            }
        }

        var since = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2));
        foreach (var type in new[] { MediaType.Movie, MediaType.Tv })
        {
            for (var page = 1; page <= MaxChangePages; page++)
            {
                var result = await tmdb.GetChanges(type, since, page, ct);
                var ids = (result?.Results ?? []).Select(c => c.Id).ToList();
                if (ids.Count > 0)
                {
                    var known = await db.Titles
                        .Where(t => t.MediaType == type && ids.Contains(t.TmdbId))
                        .Select(t => t.TmdbId)
                        .ToListAsync(ct);
                    targets.UnionWith(known.Select(id => (type, id)));
                }
                if (result is null || page >= result.TotalPages)
                {
                    break;
                }
            }
        }

        var limit = DateTime.UtcNow - StaleAge;
        var stale = await db.Titles
            .Where(t => t.AtualizadoEm < limit)
            .OrderBy(t => t.AtualizadoEm)
            .Take(StaleBatch)
            .Select(t => new { t.MediaType, t.TmdbId })
            .ToListAsync(ct);
        targets.UnionWith(stale.Select(t => (t.MediaType, t.TmdbId)));

        var updated = 0;
        foreach (var (type, id) in targets)
        {
            try
            {
                await titles.Refresh(type, id, ct);
                updated++;
            }
            catch (Exception ex) when (ex is HttpRequestException or DbUpdateException)
            {
                logger.LogWarning(ex, "Falha ao sincronizar {Type} {TmdbId}", type, id);
            }
            db.ChangeTracker.Clear();
        }
        logger.LogInformation("TMDB: {Updated} de {Total} títulos atualizados", updated, targets.Count);
    }
}
