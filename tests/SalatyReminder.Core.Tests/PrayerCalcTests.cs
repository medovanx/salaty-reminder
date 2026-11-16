using SalatyReminder.Core.Prayers;

namespace SalatyReminder.Core.Tests;

public class PrayerCalcTests
{
    const double CairoLat = 30.0444, CairoLon = 31.2357;
    static readonly TimeZoneInfo Cairo = TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo");

    /// <summary>Reference times from api.aladhan.com (method 5, Egyptian General Authority of Survey).</summary>
    [Theory]
    [InlineData("2026-11-16", "04:51", "06:21", "11:40", "14:39", "16:58", "18:19")] // winter
    [InlineData("2026-05-01", "04:37", "06:12", "12:52", "16:28", "19:32", "20:57")] // Egyptian summer time (UTC+3)
    public void Cairo_matches_Aladhan_within_a_minute(string date, string fajr, string sunrise, string dhuhr, string asr, string maghrib, string isha)
    {
        var day = DateOnly.Parse(date);
        var t = PrayerCalc.Compute(day, CairoLat, CairoLon, Methods.Get("Egyptian"), hanafi: false, Cairo);
        string[] expected = [fajr, sunrise, dhuhr, asr, maghrib, isha];
        foreach (var p in Enum.GetValues<Prayer>())
        {
            var want = day.ToDateTime(TimeOnly.Parse(expected[(int)p]));
            Assert.True(Math.Abs((t[p] - want).TotalMinutes) <= 1, $"{p}: got {t[p]:HH:mm}, expected {expected[(int)p]}");
        }
    }

    [Fact]
    public void Times_are_in_order()
    {
        var t = PrayerCalc.Compute(new DateOnly(2026, 11, 16), CairoLat, CairoLon, Methods.Get("Egyptian"), false, Cairo);
        var all = Enum.GetValues<Prayer>().Select(p => t[p]).ToArray();
        Assert.Equal(all.OrderBy(x => x), all);
    }

    [Fact]
    public void Hanafi_Asr_is_later_than_standard()
    {
        var day = new DateOnly(2026, 11, 16);
        var std = PrayerCalc.Compute(day, CairoLat, CairoLon, Methods.Get("Egyptian"), false, Cairo);
        var han = PrayerCalc.Compute(day, CairoLat, CairoLon, Methods.Get("Egyptian"), true, Cairo);
        Assert.True(han[Prayer.Asr] > std[Prayer.Asr].AddMinutes(20));
    }

    [Fact]
    public void Umm_al_Qura_Isha_is_90_minutes_after_Maghrib()
    {
        var t = PrayerCalc.Compute(new DateOnly(2026, 11, 16), 21.4225, 39.8262, Methods.Get("UmmAlQura"), false,
            TimeZoneInfo.FindSystemTimeZoneById("Asia/Riyadh"));
        Assert.Equal(90, (t[Prayer.Isha] - t[Prayer.Maghrib]).TotalMinutes);
    }

    [Fact]
    public void High_latitude_summer_still_gives_ordered_times()
    {
        // Oslo at midsummer: the sun never reaches 18 degrees below the horizon.
        var t = PrayerCalc.Compute(new DateOnly(2026, 6, 21), 59.9139, 10.7522, Methods.Get("MWL"), false,
            TimeZoneInfo.FindSystemTimeZoneById("Europe/Oslo"));
        Assert.True(t[Prayer.Fajr] < t[Prayer.Sunrise]);
        Assert.True(t[Prayer.Maghrib] < t[Prayer.Isha]);
    }

    [Fact]
    public void Unknown_method_falls_back_to_Egyptian()
    {
        Assert.Equal("Egyptian", Methods.Get("does-not-exist").Key);
    }
}
