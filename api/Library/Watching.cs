using Maratona.Api.Data;
using Maratona.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Maratona.Api.Library;

public record NewSeasonInfo(int TitleId, int SeasonId, int SeasonNumber, string Name, DateOnly? AirDate, bool Released);

public static class Watching
{
    public static void Mark(LibraryEntry entry, bool watched, DateOnly? date = null)
    {
        entry.Watched = watched;
        entry.WatchedAt = WatchedAt(watched, date, entry.WatchedAt);
        if (watched)
        {
            entry.InWatchlist = false;
        }
    }

    public static void Mark(SeasonEntry entry, bool watched, DateOnly? date = null)
    {
        entry.Watched = watched;
        entry.WatchedAt = WatchedAt(watched, date, entry.WatchedAt);
    }

    public static bool IsValidDate(DateOnly? date) =>
        date is null || (date >= new DateOnly(1900, 1, 1) && date <= DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)));

    public static IQueryable<LibraryEntry> InProgress(this IQueryable<LibraryEntry> query, AppDbContext db) =>
        query.Where(e => !e.Watched && db.SeasonEntries.Any(s => s.UserId == e.UserId && s.Season.TitleId == e.TitleId && s.Watched));

    private static DateTime? WatchedAt(bool watched, DateOnly? date, DateTime? current) =>
        !watched ? null
        : date is { } day ? day.ToDateTime(new TimeOnly(12, 0), DateTimeKind.Utc)
        : current ?? DateTime.UtcNow;

    private static Task<bool> HasWatchedSeasons(AppDbContext db, int userId, int titleId, CancellationToken ct) =>
        db.SeasonEntries.AnyAsync(e => e.UserId == userId && e.Season.TitleId == titleId && e.Watched, ct);

    public static async Task<LibraryEntry?> ChangeEntry(AppDbContext db, int userId, int titleId, Action<LibraryEntry> change, CancellationToken ct)
    {
        if (!await db.Titles.AnyAsync(t => t.Id == titleId, ct))
        {
            return null;
        }
        var entry = await db.LibraryEntries.FindAsync([userId, titleId], ct);
        if (entry is null)
        {
            entry = new LibraryEntry { UserId = userId, TitleId = titleId };
            db.LibraryEntries.Add(entry);
        }
        change(entry);
        entry.UpdatedAt = DateTime.UtcNow;
        if (!entry.InWatchlist && !entry.Watched && entry.Rating is null && !await HasWatchedSeasons(db, userId, titleId, ct))
        {
            db.LibraryEntries.Remove(entry);
        }
        await db.SaveChangesAsync(ct);
        return entry;
    }

    public static async Task<(Season Season, SeasonEntry Entry, bool TitleWatched)?> ChangeSeason(
        AppDbContext db,
        int userId,
        int seasonId,
        Action<SeasonEntry> change,
        CancellationToken ct)
    {
        var season = await db.Seasons.FindAsync([seasonId], ct);
        if (season is null)
        {
            return null;
        }
        var entry = await db.SeasonEntries.FindAsync([userId, seasonId], ct);
        if (entry is null)
        {
            entry = new SeasonEntry { UserId = userId, SeasonId = seasonId };
            db.SeasonEntries.Add(entry);
        }
        var wasWatched = entry.Watched;
        change(entry);
        entry.UpdatedAt = DateTime.UtcNow;
        if (!entry.Watched && entry.Rating is null)
        {
            db.SeasonEntries.Remove(entry);
        }
        await db.SaveChangesAsync(ct);
        var titleWatched = await SyncSeries(db, userId, season.TitleId, entry.Watched, entry.Watched != wasWatched, ct);
        await ChangeEntry(db, userId, season.TitleId, _ => { }, ct);
        return (season, entry, titleWatched);
    }

    public static Task<LibraryEntry?> WatchTitle(AppDbContext db, int userId, int titleId, CancellationToken ct) =>
        ChangeEntry(db, userId, titleId, e => Mark(e, true), ct);

    public static async Task<List<NewSeasonInfo>> NewSeasons(AppDbContext db, int userId, IReadOnlyCollection<int>? onlyTitleIds, CancellationToken ct)
    {
        var finished = await db.LibraryEntries
            .Where(e => e.UserId == userId
                && e.Watched
                && e.WatchedAt != null
                && e.Title.MediaType == MediaType.Tv
                && (onlyTitleIds == null || onlyTitleIds.Contains(e.TitleId)))
            .ToDictionaryAsync(e => e.TitleId, e => DateOnly.FromDateTime(e.WatchedAt!.Value), ct);
        if (finished.Count == 0)
        {
            return [];
        }
        var titleIds = finished.Keys.ToList();
        var candidates = await db.Seasons
            .Where(s => titleIds.Contains(s.TitleId)
                && s.SeasonNumber > 0
                && s.AirDate != null
                && !db.SeasonEntries.Any(e => e.UserId == userId && e.SeasonId == s.Id && e.Watched))
            .ToListAsync(ct);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return candidates
            .Where(s => s.AirDate > finished[s.TitleId])
            .OrderBy(s => s.AirDate)
            .Select(s => new NewSeasonInfo(s.TitleId, s.Id, s.SeasonNumber, s.Name, s.AirDate, s.AirDate <= today))
            .ToList();
    }

    private static async Task<bool> SyncSeries(AppDbContext db, int userId, int titleId, bool seasonWatched, bool changed, CancellationToken ct)
    {
        var titleWatched = await db.LibraryEntries.AnyAsync(e => e.UserId == userId && e.TitleId == titleId && e.Watched, ct);
        if (!changed)
        {
            return titleWatched;
        }
        if (seasonWatched && !titleWatched)
        {
            var pending = await db.Seasons.AnyAsync(s =>
                s.TitleId == titleId
                && s.SeasonNumber > 0
                && !db.SeasonEntries.Any(e => e.UserId == userId && e.SeasonId == s.Id && e.Watched), ct);
            if (!pending)
            {
                await WatchTitle(db, userId, titleId, ct);
                return true;
            }
        }
        if (!seasonWatched && titleWatched)
        {
            await ChangeEntry(db, userId, titleId, e => Mark(e, false), ct);
            return false;
        }
        return titleWatched;
    }
}
