using System.Text.RegularExpressions;
using Maratona.Api.Awards;
using Maratona.Api.Data;
using Maratona.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Maratona.Api.Jobs;

public partial class EmmyJob(IServiceScopeFactory scopes, ILogger<EmmyJob> logger) : DailyJob(scopes, logger)
{
    public const string Source = "wikidata";
    private const int BatchSize = 10;
    private const int MaxTitles = 200;
    private static readonly TimeSpan CheckAge = TimeSpan.FromDays(30);

    protected override string Name => "emmy-wikidata";

    protected override async Task Run(IServiceProvider services, CancellationToken ct)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var wikidata = services.GetRequiredService<WikidataClient>();
        var limit = DateTime.UtcNow - CheckAge;
        var titles = await db.Titles
            .Where(t => t.MediaType == MediaType.Tv && t.ImdbId != null && (t.AwardsCheckedAt == null || t.AwardsCheckedAt < limit))
            .OrderByDescending(t => t.Popularity)
            .Take(MaxTitles)
            .ToListAsync(ct);

        var imported = 0;
        foreach (var batch in titles.Chunk(BatchSize))
        {
            var ids = batch.Select(t => t.ImdbId!).ToList();
            try
            {
                var rows = await wikidata.GetAwards(ids, ct);
                var awards = ToAwards(rows).ToList();
                await db.Awards.Where(a => a.Source == Source && ids.Contains(a.ImdbId)).ExecuteDeleteAsync(ct);
                db.Awards.AddRange(awards);
                foreach (var title in batch)
                {
                    title.AwardsCheckedAt = DateTime.UtcNow;
                }
                await db.SaveChangesAsync(ct);
                imported += awards.Count;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Wikidata falhou para o lote {Ids}", string.Join(",", ids));
            }
            await Task.Delay(TimeSpan.FromSeconds(1), ct);
        }
        logger.LogInformation("Emmy: {Count} prêmios de {Titles} séries", imported, titles.Count);
    }

    private static IEnumerable<Award> ToAwards(IEnumerable<WikidataAward> rows) => rows
        .Where(r => AwardCategories.IsEmmy(r.Label))
        .GroupBy(r => (r.ImdbId, r.Label, r.Year))
        .Select(g =>
        {
            var (category, isMajor) = AwardCategories.FromEmmy(g.Key.Label);
            var people = string.Join(", ", g
                .Select(r => r.Person)
                .Where(p => !string.IsNullOrWhiteSpace(p) && !EntityIdRegex().IsMatch(p))
                .Distinct());
            return new Award
            {
                ImdbId = g.Key.ImdbId,
                Source = Source,
                Kind = AwardKind.Emmy,
                Year = g.Key.Year,
                Category = category.Length > 200 ? category[..200] : category,
                Person = people.Length == 0 ? null : people.Length > 200 ? people[..200] : people,
                Won = g.Any(r => r.Won),
                IsMajor = isMajor
            };
        });

    [GeneratedRegex(@"^Q\d+$")]
    private static partial Regex EntityIdRegex();
}
