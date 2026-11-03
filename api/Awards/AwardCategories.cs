using System.Globalization;

namespace Maratona.Api.Awards;

public static class AwardCategories
{
    private const string EmmyPrefix = "Primetime Emmy Award for Outstanding ";

    private static readonly Dictionary<string, string> Oscar = new()
    {
        ["BEST PICTURE"] = "Melhor Filme",
        ["DIRECTING"] = "Melhor Direção",
        ["ACTOR IN A LEADING ROLE"] = "Melhor Ator",
        ["ACTRESS IN A LEADING ROLE"] = "Melhor Atriz",
        ["ACTOR IN A SUPPORTING ROLE"] = "Melhor Ator Coadjuvante",
        ["ACTRESS IN A SUPPORTING ROLE"] = "Melhor Atriz Coadjuvante",
        ["WRITING (Original Screenplay)"] = "Melhor Roteiro Original",
        ["WRITING (Adapted Screenplay)"] = "Melhor Roteiro Adaptado",
        ["ANIMATED FEATURE FILM"] = "Melhor Animação",
        ["INTERNATIONAL FEATURE FILM"] = "Melhor Filme Internacional",
        ["DOCUMENTARY (Feature)"] = "Melhor Documentário",
        ["DOCUMENTARY (Short Subject)"] = "Melhor Documentário em Curta-Metragem",
        ["CINEMATOGRAPHY"] = "Melhor Fotografia",
        ["FILM EDITING"] = "Melhor Montagem",
        ["ART DIRECTION"] = "Melhor Design de Produção",
        ["COSTUME DESIGN"] = "Melhor Figurino",
        ["MAKEUP AND HAIRSTYLING"] = "Melhor Maquiagem e Penteado",
        ["MUSIC (Original Score)"] = "Melhor Trilha Sonora",
        ["MUSIC (Original Song)"] = "Melhor Canção Original",
        ["SOUND MIXING"] = "Melhor Som",
        ["SOUND EDITING"] = "Melhor Edição de Som",
        ["VISUAL EFFECTS"] = "Melhores Efeitos Visuais",
        ["SHORT FILM (Animated)"] = "Melhor Curta de Animação",
        ["SHORT FILM (Live Action)"] = "Melhor Curta-Metragem",
        ["CASTING"] = "Melhor Seleção de Elenco",
        ["SPECIAL AWARD"] = "Prêmio Especial"
    };

    private static readonly HashSet<string> OscarMajor =
    [
        "BEST PICTURE",
        "DIRECTING",
        "ACTOR IN A LEADING ROLE",
        "ACTRESS IN A LEADING ROLE",
        "ACTOR IN A SUPPORTING ROLE",
        "ACTRESS IN A SUPPORTING ROLE",
        "WRITING (Original Screenplay)",
        "WRITING (Adapted Screenplay)"
    ];

    private static readonly (string From, string To)[] EmmyNormalize =
    [
        ("Limited or Anthology Series or Movie", "Limited Series"),
        ("Limited Series or Movie", "Limited Series"),
        ("Miniseries or a Movie", "Limited Series"),
        ("Miniseries or Movie", "Limited Series"),
        ("Limited or Anthology Series", "Limited Series"),
        ("Miniseries", "Limited Series")
    ];

    private static readonly Dictionary<string, string> Emmy = new()
    {
        ["Drama Series"] = "Melhor Série de Drama",
        ["Comedy Series"] = "Melhor Série de Comédia",
        ["Limited Series"] = "Melhor Série Limitada",
        ["Lead Actor in a Drama Series"] = "Melhor Ator em Série de Drama",
        ["Lead Actress in a Drama Series"] = "Melhor Atriz em Série de Drama",
        ["Lead Actor in a Comedy Series"] = "Melhor Ator em Série de Comédia",
        ["Lead Actress in a Comedy Series"] = "Melhor Atriz em Série de Comédia",
        ["Lead Actor in a Limited Series"] = "Melhor Ator em Série Limitada",
        ["Lead Actress in a Limited Series"] = "Melhor Atriz em Série Limitada",
        ["Supporting Actor in a Drama Series"] = "Melhor Ator Coadjuvante em Série de Drama",
        ["Supporting Actress in a Drama Series"] = "Melhor Atriz Coadjuvante em Série de Drama",
        ["Supporting Actor in a Comedy Series"] = "Melhor Ator Coadjuvante em Série de Comédia",
        ["Supporting Actress in a Comedy Series"] = "Melhor Atriz Coadjuvante em Série de Comédia",
        ["Supporting Actor in a Limited Series"] = "Melhor Ator Coadjuvante em Série Limitada",
        ["Supporting Actress in a Limited Series"] = "Melhor Atriz Coadjuvante em Série Limitada",
        ["Directing for a Drama Series"] = "Melhor Direção em Série de Drama",
        ["Directing for a Comedy Series"] = "Melhor Direção em Série de Comédia",
        ["Directing for a Limited Series"] = "Melhor Direção em Série Limitada",
        ["Writing for a Drama Series"] = "Melhor Roteiro em Série de Drama",
        ["Writing for a Comedy Series"] = "Melhor Roteiro em Série de Comédia",
        ["Writing for a Limited Series"] = "Melhor Roteiro em Série Limitada",
        ["Guest Actor in a Drama Series"] = "Melhor Ator Convidado em Série de Drama",
        ["Guest Actress in a Drama Series"] = "Melhor Atriz Convidada em Série de Drama",
        ["Guest Actor in a Comedy Series"] = "Melhor Ator Convidado em Série de Comédia",
        ["Guest Actress in a Comedy Series"] = "Melhor Atriz Convidada em Série de Comédia"
    };

    private static readonly HashSet<string> EmmyMajor =
    [
        "Drama Series",
        "Comedy Series",
        "Limited Series",
        "Lead Actor in a Drama Series",
        "Lead Actress in a Drama Series",
        "Lead Actor in a Comedy Series",
        "Lead Actress in a Comedy Series",
        "Lead Actor in a Limited Series",
        "Lead Actress in a Limited Series"
    ];

    public static (string Category, bool IsMajor) FromOscar(string canonical, string category) =>
        Oscar.TryGetValue(canonical, out var name)
            ? (name, OscarMajor.Contains(canonical))
            : (CultureInfo.InvariantCulture.TextInfo.ToTitleCase(category.ToLowerInvariant()), false);

    public static bool IsEmmy(string label) => label.StartsWith(EmmyPrefix, StringComparison.Ordinal);

    public static (string Category, bool IsMajor) FromEmmy(string label)
    {
        var key = label[EmmyPrefix.Length..];
        foreach (var (from, to) in EmmyNormalize)
        {
            key = key.Replace(from, to);
        }
        return (Emmy.GetValueOrDefault(key, key), EmmyMajor.Contains(key));
    }
}
