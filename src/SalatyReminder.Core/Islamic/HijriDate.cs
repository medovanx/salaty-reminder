using System.Globalization;

namespace SalatyReminder.Core.Islamic;

/// <summary>A day of the Umm al-Qura calendar, after the user's Hijri adjustment (a day or two either way).</summary>
public readonly record struct HijriDate(int Year, int Month, int Day)
{
    public const int Muharram = 1, Rajab = 7, Shaban = 8, Ramadan = 9, Shawwal = 10, DhuAlHijjah = 12;

    static readonly UmAlQuraCalendar Cal = new();

    /// <summary>The Hijri date for <paramref name="date"/>, or null outside the years Umm al-Qura covers (1900 to 2077).</summary>
    public static HijriDate? From(DateOnly date, int adjustDays = 0)
    {
        var d = date.ToDateTime(TimeOnly.MinValue).AddDays(adjustDays);
        if (d < Cal.MinSupportedDateTime || d > Cal.MaxSupportedDateTime) return null;
        return new HijriDate(Cal.GetYear(d), Cal.GetMonth(d), Cal.GetDayOfMonth(d));
    }

    public bool IsRamadan => Month == Ramadan;
}
