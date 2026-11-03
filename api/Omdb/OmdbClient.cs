using System.Globalization;
using System.Text.RegularExpressions;

namespace Maratona.Api.Omdb;

public record OmdbResult(
    bool Found,
    bool LimitReached,
    decimal? Imdb,
    int? ImdbVotes,
    int? RottenTomatoes,
    int? Metacritic,
    string? Awards,
    int? OscarWins,
    int? OscarNominations,
    int? EmmyWins,
    int? EmmyNominations);

public partial class OmdbClient(HttpClient http, IConfiguration config)
{
    private readonly string apiKey = config["OMDB_API_KEY"]
        ?? throw new InvalidOperationException("OMDB_API_KEY não configurada.");

    public async Task<OmdbResult> Get(string imdbId, CancellationToken ct)
    {
        using var response = await http.GetAsync($"?apikey={apiKey}&i={Uri.EscapeDataString(imdbId)}", ct);
        var data = await response.Content.ReadFromJsonAsync<OmdbResponse>(ct);
        if (data is null || data.Response != "True")
        {
            var error = data?.Error ?? "";
            if (error.Contains("limit", StringComparison.OrdinalIgnoreCase))
            {
                return Empty(limitReached: true);
            }
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"OMDb respondeu {(int)response.StatusCode}: {error}");
            }
            return Empty(limitReached: false);
        }
        var ratings = data.Ratings ?? [];
        var awards = Clean(data.Awards);
        return new OmdbResult(
            true,
            false,
            decimal.TryParse(Clean(data.ImdbRating), NumberStyles.Number, CultureInfo.InvariantCulture, out var imdb) ? imdb : null,
            int.TryParse(Clean(data.ImdbVotes)?.Replace(",", ""), out var votes) ? votes : null,
            ParseLeadingInt(ratings.FirstOrDefault(r => r.Source == "Rotten Tomatoes")?.Value),
            ParseLeadingInt(Clean(data.Metascore) ?? ratings.FirstOrDefault(r => r.Source == "Metacritic")?.Value),
            awards,
            Match(OscarWinsRegex(), awards),
            Match(OscarNominationsRegex(), awards),
            Match(EmmyWinsRegex(), awards),
            Match(EmmyNominationsRegex(), awards));
    }

    private static OmdbResult Empty(bool limitReached) =>
        new(false, limitReached, null, null, null, null, null, null, null, null, null);

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) || value == "N/A" ? null : value.Trim();

    private static int? ParseLeadingInt(string? value)
    {
        var digits = new string((value ?? "").TakeWhile(char.IsDigit).ToArray());
        return int.TryParse(digits, out var number) ? number : null;
    }

    private static int? Match(Regex regex, string? text)
    {
        var match = regex.Match(text ?? "");
        return match.Success ? int.Parse(match.Groups[1].Value) : null;
    }

    [GeneratedRegex(@"Won (\d+) Oscars?\b")]
    private static partial Regex OscarWinsRegex();

    [GeneratedRegex(@"Nominated for (\d+) Oscars?\b")]
    private static partial Regex OscarNominationsRegex();

    [GeneratedRegex(@"Won (\d+) Primetime Emmys?\b")]
    private static partial Regex EmmyWinsRegex();

    [GeneratedRegex(@"Nominated for (\d+) Primetime Emmys?\b")]
    private static partial Regex EmmyNominationsRegex();

    private record OmdbRating(string Source, string Value);

    private record OmdbResponse(
        string? Response,
        string? Error,
        string? ImdbRating,
        string? ImdbVotes,
        string? Metascore,
        string? Awards,
        List<OmdbRating>? Ratings);
}
