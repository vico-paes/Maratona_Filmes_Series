using Maratona.Api.Awards;
using Maratona.Api.Data;
using Maratona.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Maratona.Api.Jobs;

public class OscarImportJob(IServiceScopeFactory scopes, ILogger<OscarImportJob> logger) : DailyJob(scopes, logger)
{
    public const string Source = "oscar_data";
    private const string DatasetUrl = "https://raw.githubusercontent.com/DLu/oscar_data/main/oscars.csv";
    private const int MinimumRows = 5000;

    protected override string Name => "oscar-import";

    protected override TimeSpan Interval => TimeSpan.FromDays(7);

    protected override async Task Run(IServiceProvider services, CancellationToken ct)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var http = services.GetRequiredService<IHttpClientFactory>().CreateClient("datasets");
        var content = await http.GetStringAsync(DatasetUrl, ct);
        var awards = Parse(content).ToList();
        if (awards.Count < MinimumRows)
        {
            logger.LogWarning("Dataset do Oscar com apenas {Count} linhas, importação ignorada", awards.Count);
            return;
        }
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.Awards.Where(a => a.Source == Source).ExecuteDeleteAsync(ct);
        db.Awards.AddRange(awards);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        logger.LogInformation("Oscar: {Count} indicações importadas", awards.Count);
    }

    private static IEnumerable<Award> Parse(string content)
    {
        foreach (var line in content.Split('\n').Skip(1))
        {
            var columns = line.TrimEnd('\r').Replace("\\\"", "\"").Split('\t');
            if (columns.Length < 7 || !columns[6].StartsWith("tt", StringComparison.Ordinal))
            {
                continue;
            }
            var (category, isMajor) = AwardCategories.FromOscar(columns[3], columns[4]);
            var person = columns.Length > 7 ? columns[7] : "";
            foreach (var imdbId in columns[6].Split('|').Distinct())
            {
                yield return new Award
                {
                    ImdbId = imdbId,
                    Source = Source,
                    Kind = AwardKind.Oscar,
                    Year = CeremonyYear(columns[1]),
                    Category = category,
                    Person = string.IsNullOrWhiteSpace(person) ? null : person.Length > 200 ? person[..200] : person,
                    Won = columns.Length > 10 && columns[10] == "True",
                    IsMajor = isMajor
                };
            }
        }
    }

    private static int? CeremonyYear(string filmYear)
    {
        if (!int.TryParse(filmYear.AsSpan(0, Math.Min(4, filmYear.Length)), out var year))
        {
            return null;
        }
        return filmYear.Contains('/') ? year + 2 : year + 1;
    }
}
