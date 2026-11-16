namespace SalatyReminder;

public enum Prayer { Fajr, Sunrise, Dhuhr, Asr, Maghrib, Isha }

/// <param name="IshaMinutes">When set, Isha is this many minutes after Maghrib instead of an angle.</param>
public sealed record MethodDef(string Key, double FajrAngle, double IshaAngle, int IshaMinutes, string NameEn, string NameAr)
{
    public string Name => L.Ar ? NameAr : NameEn;
}

public static class Methods
{
    public static readonly MethodDef[] All =
    [
        new("Egyptian", 19.5, 17.5, 0, "Egyptian General Authority of Survey", "الهيئة المصرية العامة للمساحة"),
        new("MWL", 18, 17, 0, "Muslim World League", "رابطة العالم الإسلامي"),
        new("UmmAlQura", 18.5, 0, 90, "Umm al-Qura, Makkah", "أم القرى، مكة المكرمة"),
        new("Karachi", 18, 18, 0, "University of Islamic Sciences, Karachi", "جامعة العلوم الإسلامية، كراتشي"),
        new("ISNA", 15, 15, 0, "Islamic Society of North America", "الجمعية الإسلامية لأمريكا الشمالية"),
        new("Kuwait", 18, 17.5, 0, "Kuwait", "الكويت"),
        new("Qatar", 18, 0, 90, "Qatar", "قطر"),
        new("Dubai", 18.2, 18.2, 0, "Dubai", "دبي"),
        new("Turkey", 18, 17, 0, "Diyanet, Turkey", "رئاسة الشؤون الدينية، تركيا"),
    ];

    public static MethodDef Get(string key) => All.FirstOrDefault(m => m.Key == key) ?? All[0];
}

public sealed class DayTimes(DateOnly date, DateTime[] times)
{
    public DateOnly Date { get; } = date;
    public DateTime this[Prayer p] => times[(int)p];
}

/// <summary>
/// Astronomical prayer times (the PrayTimes.org algorithm), computed in UTC and converted
/// with the time zone's rules for that date, so DST changes are handled per day.
/// </summary>
public static class PrayerCalc
{
    public static readonly Prayer[] FivePrayers = [Prayer.Fajr, Prayer.Dhuhr, Prayer.Asr, Prayer.Maghrib, Prayer.Isha];

    public static DayTimes Compute(DateOnly date, double lat, double lng, MethodDef m, bool hanafi, TimeZoneInfo? tz = null)
    {
        tz ??= TimeZoneInfo.Local;
        double jDate = Julian(date.Year, date.Month, date.Day) - lng / (15 * 24.0);

        // Initial guesses in hours, refined by one pass (as PrayTimes does).
        double fajr = 5, sunrise = 6, dhuhr = 12, asr = 13, sunset = 18, isha = 18;
        for (int i = 0; i < 2; i++)
        {
            fajr = SunAngleTime(jDate, lat, m.FajrAngle, fajr / 24, ccw: true);
            sunrise = SunAngleTime(jDate, lat, 0.833, sunrise / 24, ccw: true);
            dhuhr = MidDay(jDate, dhuhr / 24);
            asr = AsrTime(jDate, lat, hanafi ? 2 : 1, asr / 24);
            sunset = SunAngleTime(jDate, lat, 0.833, sunset / 24, ccw: false);
            if (m.IshaMinutes == 0) isha = SunAngleTime(jDate, lat, m.IshaAngle, isha / 24, ccw: false);
        }

        double shift = -lng / 15; // to UTC
        fajr += shift; sunrise += shift; dhuhr += shift; asr += shift; sunset += shift; isha += shift;
        double maghrib = sunset;
        if (m.IshaMinutes > 0) isha = maghrib + m.IshaMinutes / 60.0;

        // High-latitude fallback (angle-based night portion) when the sun never reaches the angle.
        double night = Fix(sunrise - sunset, 24);
        double fajrPortion = m.FajrAngle / 60 * night;
        if (double.IsNaN(fajr) || Fix(sunrise - fajr, 24) > fajrPortion) fajr = sunrise - fajrPortion;
        if (m.IshaMinutes == 0)
        {
            double ishaPortion = m.IshaAngle / 60 * night;
            if (double.IsNaN(isha) || Fix(isha - sunset, 24) > ishaPortion) isha = sunset + ishaPortion;
        }

        var midnightUtc = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Utc);
        DateTime ToLocal(double hours)
        {
            var utc = midnightUtc.AddMinutes(Math.Round(hours * 60));
            return TimeZoneInfo.ConvertTimeFromUtc(utc, tz);
        }

        return new DayTimes(date, [ToLocal(fajr), ToLocal(sunrise), ToLocal(dhuhr), ToLocal(asr), ToLocal(maghrib), ToLocal(isha)]);
    }

    static double SunAngleTime(double jDate, double lat, double angle, double t, bool ccw)
    {
        var (decl, _) = SunPosition(jDate + t);
        double noon = MidDay(jDate, t);
        double T = DArccos((-DSin(angle) - DSin(decl) * DSin(lat)) / (DCos(decl) * DCos(lat))) / 15;
        return noon + (ccw ? -T : T);
    }

    static double AsrTime(double jDate, double lat, double factor, double t)
    {
        var (decl, _) = SunPosition(jDate + t);
        double angle = -DArccot(factor + DTan(Math.Abs(lat - decl)));
        return SunAngleTime(jDate, lat, angle, t, ccw: false);
    }

    static double MidDay(double jDate, double t)
    {
        var (_, eqt) = SunPosition(jDate + t);
        return Fix(12 - eqt, 24);
    }

    static (double decl, double eqt) SunPosition(double jd)
    {
        double D = jd - 2451545.0;
        double g = Fix(357.529 + 0.98560028 * D, 360);
        double q = Fix(280.459 + 0.98564736 * D, 360);
        double L = Fix(q + 1.915 * DSin(g) + 0.020 * DSin(2 * g), 360);
        double e = 23.439 - 0.00000036 * D;
        double ra = DArctan2(DCos(e) * DSin(L), DCos(L)) / 15;
        double eqt = q / 15 - Fix(ra, 24);
        double decl = DArcsin(DSin(e) * DSin(L));
        return (decl, eqt);
    }

    static double Julian(int y, int m, int d)
    {
        if (m <= 2) { y--; m += 12; }
        double a = Math.Floor(y / 100.0);
        double b = 2 - a + Math.Floor(a / 4);
        return Math.Floor(365.25 * (y + 4716)) + Math.Floor(30.6001 * (m + 1)) + d + b - 1524.5;
    }

    static double Fix(double a, double b) { a -= b * Math.Floor(a / b); return a < 0 ? a + b : a; }
    const double Rad = Math.PI / 180;
    static double DSin(double d) => Math.Sin(d * Rad);
    static double DCos(double d) => Math.Cos(d * Rad);
    static double DTan(double d) => Math.Tan(d * Rad);
    static double DArcsin(double x) => Math.Asin(x) / Rad;
    static double DArccos(double x) => Math.Acos(x) / Rad;
    static double DArctan2(double y, double x) => Math.Atan2(y, x) / Rad;
    static double DArccot(double x) => Math.Atan(1 / x) / Rad;
}
