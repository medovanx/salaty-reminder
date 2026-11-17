namespace SalatyReminder.Core.Islamic;

/// <summary>
/// The occasions of the Hijri year and the sunnah fasting days. Only well-established days are listed:
/// the Islamic New Year, Tasu'a and Ashura, Ramadan and its last ten nights (from Maghrib on the 20th), the Eids, the first ten days
/// of Dhu al-Hijjah, Arafah and Tashreeq. Fasting: Mondays and Thursdays, the white days (13 to 15),
/// Tasu'a and Ashura, Arafah and six days of Shawwal; never on an Eid or a day of Tashreeq.
/// </summary>
public static class Occasions
{
    public static DayInfo For(DateOnly date, int adjustDays = 0)
    {
        if (HijriDate.From(date, adjustDays) is not { } h) return new(null, FastKind.None);
        var occasion = OccasionOf(h);
        var info = new DayInfo(occasion, FastKind.None);
        if (info.FastingForbidden || h.IsRamadan) return info; // Ramadan is fasted anyway
        return info with { Fast = FastOf(h, date.DayOfWeek) };
    }

    static OccasionKey? OccasionOf(HijriDate h) => (h.Month, h.Day) switch
    {
        (HijriDate.Muharram, 1) => OccasionKey.IslamicNewYear,
        (HijriDate.Muharram, 9) => OccasionKey.Tasua,
        (HijriDate.Muharram, 10) => OccasionKey.Ashura,
        (HijriDate.Ramadan, 1) => OccasionKey.RamadanStart,
        // A Hijri night comes before its day: the 21st night, the first of the last ten, starts at Maghrib on the 20th.
        (HijriDate.Ramadan, >= 20) => OccasionKey.LastTenNights,
        (HijriDate.Shawwal, 1) => OccasionKey.EidAlFitr,
        (HijriDate.DhuAlHijjah, 9) => OccasionKey.Arafah,
        (HijriDate.DhuAlHijjah, 10) => OccasionKey.EidAlAdha,
        (HijriDate.DhuAlHijjah, >= 11 and <= 13) => OccasionKey.Tashreeq,
        (HijriDate.DhuAlHijjah, <= 8) => OccasionKey.FirstTenDhulHijjah,
        _ => null,
    };

    static FastKind FastOf(HijriDate h, DayOfWeek dow) => (h.Month, h.Day) switch
    {
        (HijriDate.DhuAlHijjah, 9) => FastKind.Arafah,
        (HijriDate.Muharram, 9 or 10) => FastKind.Ashura,
        (_, >= 13 and <= 15) => FastKind.WhiteDays,
        _ when dow is DayOfWeek.Monday or DayOfWeek.Thursday => FastKind.MondayThursday,
        (HijriDate.Shawwal, >= 2) => FastKind.SixOfShawwal,
        _ => FastKind.None,
    };
}
