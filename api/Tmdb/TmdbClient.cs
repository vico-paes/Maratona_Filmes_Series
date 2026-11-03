using System.Net;
using System.Text.Json;
using Maratona.Api.Data.Entities;

namespace Maratona.Api.Tmdb;

public class TmdbClient(HttpClient http, IConfiguration config)
{
    private const string Language = "pt-BR";
    private const string Region = "BR";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private readonly string apiKey = config["TMDB_API_KEY"]
        ?? throw new InvalidOperationException("TMDB_API_KEY não configurada.");

    public Task<TmdbMovie?> GetMovie(int id, CancellationToken ct) =>
        Get<TmdbMovie>($"movie/{id}", "&append_to_response=credits", ct);

    public Task<TmdbTv?> GetTv(int id, CancellationToken ct) =>
        Get<TmdbTv>($"tv/{id}", "&append_to_response=credits,external_ids", ct);

    public Task<TmdbSeason?> GetSeason(int tvId, int number, CancellationToken ct) =>
        Get<TmdbSeason>($"tv/{tvId}/season/{number}", "", ct);

    public async Task<string?> GetEnglishOverview(MediaType type, int id, CancellationToken ct)
    {
        var path = type == MediaType.Movie ? $"movie/{id}" : $"tv/{id}";
        var item = await Get<TmdbOverview>(path, "", ct, "en-US");
        return item?.Overview;
    }

    public Task<TmdbPage<TmdbListItem>?> Search(MediaType type, string query, int page, CancellationToken ct) =>
        Get<TmdbPage<TmdbListItem>>(
            type == MediaType.Movie ? "search/movie" : "search/tv",
            $"&query={Uri.EscapeDataString(query)}&page={page}&include_adult=false",
            ct);

    public Task<TmdbPage<TmdbListItem>?> GetList(string path, int page, CancellationToken ct) =>
        Get<TmdbPage<TmdbListItem>>(path, $"&page={page}&region={Region}", ct);

    public Task<TmdbPage<TmdbChange>?> GetChanges(MediaType type, DateOnly since, int page, CancellationToken ct) =>
        Get<TmdbPage<TmdbChange>>(
            type == MediaType.Movie ? "movie/changes" : "tv/changes",
            $"&start_date={since:yyyy-MM-dd}&page={page}",
            ct);

    private async Task<T?> Get<T>(string path, string query, CancellationToken ct, string language = Language)
    {
        using var response = await http.GetAsync($"{path}?api_key={apiKey}&language={language}{query}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return default;
        }
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(Json, ct);
    }

    private record TmdbOverview(string? Overview);
}
