namespace Khyout.Domain;

/// <summary>A Syrian city offered as a controlled value for company profiles.</summary>
public sealed record CityInfo(string Code, string NameAr, string NameEn);

/// <summary>
/// Reference data shared by validation (Phase 3) and the UI. Intentionally a static
/// list (not a lookup table) for the MVP; promote to a table only if admins must
/// manage it at runtime.
/// </summary>
public static class ReferenceData
{
    public static IReadOnlyList<CityInfo> Cities { get; } = new List<CityInfo>
    {
        new("damascus",     "دمشق",      "Damascus"),
        new("aleppo",       "حلب",       "Aleppo"),
        new("homs",         "حمص",       "Homs"),
        new("hama",         "حماة",      "Hama"),
        new("latakia",      "اللاذقية",  "Latakia"),
        new("tartous",      "طرطوس",     "Tartous"),
        new("idlib",        "إدلب",      "Idlib"),
        new("deir-ez-zor",  "دير الزور", "Deir ez-Zor"),
        new("raqqa",        "الرقة",     "Raqqa"),
        new("hasakah",      "الحسكة",    "Hasakah"),
        new("qamishli",     "القامشلي",  "Qamishli"),
        new("daraa",        "درعا",      "Daraa"),
        new("as-suwayda",   "السويداء",  "As-Suwayda"),
        new("quneitra",     "القنيطرة",  "Quneitra")
    };

    public static bool IsKnownCity(string? city) =>
        !string.IsNullOrWhiteSpace(city)
        && Cities.Any(c => string.Equals(c.Code, city, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(c.NameEn, city, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(c.NameAr, city, StringComparison.OrdinalIgnoreCase));
}
