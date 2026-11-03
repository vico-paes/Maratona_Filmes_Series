using Maratona.Api.Data.Entities;

namespace Maratona.Api.Omdb;

public static class RefreshSchedule
{
    public static readonly TimeSpan NotFound = TimeSpan.FromDays(30);

    public static TimeSpan Next(Title title, DateTime now)
    {
        var airing = title.MediaType == MediaType.Tv && title.Status is "Returning Series" or "In Production";
        var reference = airing ? title.LastAirDate ?? title.ReleaseDate : title.ReleaseDate;
        if (reference is null)
        {
            return TimeSpan.FromDays(30);
        }
        var age = DateOnly.FromDateTime(now).DayNumber - reference.Value.DayNumber;
        var days = airing
            ? age <= 30 ? 7 : age <= 90 ? 14 : 30
            : age <= 30 ? 7 : age <= 90 ? 14 : age <= 180 ? 30 : age <= 365 ? 60 : 120;
        return TimeSpan.FromDays(days);
    }
}
