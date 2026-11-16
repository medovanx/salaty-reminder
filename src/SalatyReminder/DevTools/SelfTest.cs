namespace SalatyReminder.DevTools;

/// <summary>
/// --selftest out.txt lat lon yyyy-MM-dd [method] [hanafi]
/// Writes computed times (local clock) to a file so they can be checked against a reference.
/// </summary>
static class SelfTest
{
    public static int Run(string[] a)
    {
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        string outPath = a[1];
        double lat = double.Parse(a[2], inv), lon = double.Parse(a[3], inv);
        var date = DateOnly.ParseExact(a[4], "yyyy-MM-dd", inv);
        var method = Methods.Get(a.Length > 5 ? a[5] : "Egyptian");
        bool hanafi = a.Length > 6 && a[6] == "hanafi";
        var tz = a.Length > 7 ? TimeZoneInfo.FindSystemTimeZoneById(a[7]) : TimeZoneInfo.Local;
        var t = PrayerCalc.Compute(date, lat, lon, method, hanafi, tz);
        var lines = Enum.GetValues<Prayer>().Select(p => $"{p}\t{t[p]:yyyy-MM-dd HH:mm}");
        File.WriteAllLines(outPath, lines);
        return 0;
    }
}
