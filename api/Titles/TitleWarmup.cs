using System.Threading.Channels;
using Maratona.Api.Data;
using Maratona.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Maratona.Api.Titles;

public class TitleWarmupQueue
{
    private readonly Channel<(MediaType Type, int TmdbId)> channel =
        Channel.CreateBounded<(MediaType, int)>(new BoundedChannelOptions(500) { FullMode = BoundedChannelFullMode.DropWrite });

    public void Enqueue(MediaType type, IEnumerable<int> tmdbIds)
    {
        foreach (var id in tmdbIds)
        {
            channel.Writer.TryWrite((type, id));
        }
    }

    public IAsyncEnumerable<(MediaType Type, int TmdbId)> ReadAll(CancellationToken ct) => channel.Reader.ReadAllAsync(ct);
}

public class TitleWarmupService(TitleWarmupQueue queue, IServiceScopeFactory scopes, ILogger<TitleWarmupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await foreach (var (type, tmdbId) in queue.ReadAll(ct))
        {
            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                if (await db.Titles.AnyAsync(t => t.MediaType == type && t.TmdbId == tmdbId, ct))
                {
                    continue;
                }
                await scope.ServiceProvider.GetRequiredService<TitleService>().Refresh(type, tmdbId, ct);
            }
            catch (Exception ex) when (ex is HttpRequestException or DbUpdateException)
            {
                logger.LogWarning(ex, "Falha ao pré-carregar {Type} {TmdbId}", type, tmdbId);
            }
        }
    }
}
