using System.Text.Json;

namespace Maratona.Api.Awards;

public record WikidataAward(string ImdbId, string Label, int? Year, string? Person, bool Won);

public class WikidataClient(HttpClient http)
{
    public async Task<List<WikidataAward>> GetAwards(IReadOnlyCollection<string> imdbIds, CancellationToken ct)
    {
        var values = string.Join(" ", imdbIds.Select(id => $"\"{id}\""));
        var query = $$"""
            SELECT ?imdb ?how ?awardLabel ?time ?personLabel WHERE {
              VALUES ?imdb { {{values}} }
              ?work wdt:P345 ?imdb .
              { ?work p:P166 ?st . ?st ps:P166 ?award . BIND("won" AS ?how) OPTIONAL { ?st pq:P1346 ?person } }
              UNION { ?work p:P1411 ?st . ?st ps:P1411 ?award . BIND("nom" AS ?how) OPTIONAL { ?st pq:P1346 ?person } }
              UNION { ?person p:P166 ?st . ?st ps:P166 ?award ; pq:P1686 ?work . BIND("won" AS ?how) }
              UNION { ?person p:P1411 ?st . ?st ps:P1411 ?award ; pq:P1686 ?work . BIND("nom" AS ?how) }
              OPTIONAL { ?st pq:P585 ?time }
              SERVICE wikibase:label { bd:serviceParam wikibase:language "en". }
            }
            """;
        using var response = await http.GetAsync($"sparql?format=json&query={Uri.EscapeDataString(query)}", ct);
        response.EnsureSuccessStatusCode();
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var result = new List<WikidataAward>();
        foreach (var row in json.RootElement.GetProperty("results").GetProperty("bindings").EnumerateArray())
        {
            var time = Value(row, "time");
            result.Add(new WikidataAward(
                Value(row, "imdb") ?? "",
                Value(row, "awardLabel") ?? "",
                time is { Length: >= 4 } && int.TryParse(time.AsSpan(0, 4), out var year) ? year : null,
                Value(row, "personLabel"),
                Value(row, "how") == "won"));
        }
        return result;
    }

    private static string? Value(JsonElement row, string name) =>
        row.TryGetProperty(name, out var field) ? field.GetProperty("value").GetString() : null;
}
