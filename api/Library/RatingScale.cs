using Maratona.Api.Data.Entities;

namespace Maratona.Api.Library;

public static class RatingScale
{
    public static bool IsValid(decimal? rating, RatingStyle style) =>
        rating is null || (rating * 2 == Math.Floor(rating.Value * 2) && style switch
        {
            RatingStyle.Stars => rating >= 0.5m && rating <= 5,
            _ => rating >= 0 && rating <= 10
        });

    public static string Rule(RatingStyle style) => style == RatingStyle.Stars
        ? "A nota em estrelas vai de 0,5 a 5, de meia em meia estrela."
        : "A nota vai de 0 a 10, de meio em meio ponto.";

    public static decimal Convert(decimal rating, RatingStyle from, RatingStyle to)
    {
        if (from == to)
        {
            return rating;
        }
        if (to == RatingStyle.Numeric)
        {
            return rating * 2;
        }
        var stars = Math.Round(rating, MidpointRounding.AwayFromZero) / 2;
        return Math.Max(0.5m, stars);
    }
}
