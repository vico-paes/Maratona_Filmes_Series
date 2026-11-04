using Maratona.Api.Data;
using Maratona.Api.Data.Entities;
using Maratona.Api.Infrastructure;
using Maratona.Api.Titles;
using Microsoft.EntityFrameworkCore;

namespace Maratona.Api.Reviews;

public record ReviewDto(
    int Id,
    UserSummaryDto User,
    int TitleId,
    int? SeasonId,
    int? SeasonNumber,
    decimal? Rating,
    RatingStyle? RatingStyle,
    string Body,
    bool Spoiler,
    int Likes,
    bool LikedByMe,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    TitleCardDto? Title);

public static class ReviewQueries
{
    public static async Task<List<ReviewDto>> Load(
        AppDbContext db,
        TitleService titles,
        IQueryable<Review> query,
        int? viewerId,
        bool includeTitle,
        CancellationToken ct)
    {
        var rows = await query
            .Select(r => new
            {
                r.Id,
                r.User.Username,
                r.User.DisplayName,
                r.User.AvatarUrl,
                r.TitleId,
                r.SeasonId,
                SeasonNumber = r.Season != null ? r.Season.SeasonNumber : (int?)null,
                Rating = r.SeasonId == null
                    ? db.LibraryEntries.Where(e => e.UserId == r.UserId && e.TitleId == r.TitleId).Select(e => e.Rating).FirstOrDefault()
                    : db.SeasonEntries.Where(e => e.UserId == r.UserId && e.SeasonId == r.SeasonId).Select(e => e.Rating).FirstOrDefault(),
                RatingStyle = r.SeasonId == null
                    ? db.LibraryEntries.Where(e => e.UserId == r.UserId && e.TitleId == r.TitleId).Select(e => e.RatingStyle).FirstOrDefault()
                    : db.SeasonEntries.Where(e => e.UserId == r.UserId && e.SeasonId == r.SeasonId).Select(e => e.RatingStyle).FirstOrDefault(),
                r.Body,
                r.Spoiler,
                Likes = r.Likes.Count,
                LikedByMe = viewerId != null && r.Likes.Any(l => l.UserId == viewerId),
                r.CreatedAt,
                r.UpdatedAt
            })
            .ToListAsync(ct);
        var cards = includeTitle ? await titles.Cards(rows.Select(r => r.TitleId), ct) : [];
        return rows.Select(r => new ReviewDto(
            r.Id,
            new UserSummaryDto(r.Username, r.DisplayName, r.AvatarUrl),
            r.TitleId,
            r.SeasonId,
            r.SeasonNumber,
            r.Rating,
            r.RatingStyle,
            r.Body,
            r.Spoiler,
            r.Likes,
            r.LikedByMe,
            r.CreatedAt,
            r.UpdatedAt,
            cards.GetValueOrDefault(r.TitleId))).ToList();
    }
}
