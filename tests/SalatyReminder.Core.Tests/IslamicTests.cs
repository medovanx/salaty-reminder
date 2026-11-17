using SalatyReminder.Core.Islamic;

namespace SalatyReminder.Core.Tests;

public class IslamicTests
{
    // Umm al-Qura dates for 1447 AH.
    static readonly DateOnly Ramadan1 = new(2026, 2, 18), Eid = new(2026, 3, 20), Arafah = new(2026, 5, 26), Adha = new(2026, 5, 27);

    [Fact]
    public void Ramadan_starts_and_ends_on_the_Umm_al_Qura_dates()
    {
        Assert.False(Ramadan.IsRamadan(Ramadan1.AddDays(-1)));
        Assert.Equal(1, Ramadan.Day(Ramadan1));
        Assert.Equal(OccasionKey.RamadanStart, Occasions.For(Ramadan1).Occasion);
        Assert.False(Ramadan.IsRamadan(Eid));
        Assert.Equal(OccasionKey.EidAlFitr, Occasions.For(Eid).Occasion);
    }

    [Fact]
    public void Hijri_adjustment_shifts_the_day()
    {
        Assert.Equal(0, Ramadan.Day(Ramadan1.AddDays(-1)));
        Assert.Equal(1, Ramadan.Day(Ramadan1.AddDays(-1), adjustDays: 1));
    }

    [Fact]
    public void No_fasting_on_Eid_or_Tashreeq_even_on_a_Monday_or_Thursday()
    {
        foreach (var d in new[] { Eid, Adha, Adha.AddDays(1), Adha.AddDays(2), Adha.AddDays(3) })
        {
            var info = Occasions.For(d);
            Assert.True(info.FastingForbidden, d.ToString());
            Assert.Equal(FastKind.None, info.Fast);
        }
    }

    [Fact]
    public void Arafah_is_an_occasion_and_a_fast()
    {
        var info = Occasions.For(Arafah);
        Assert.Equal(OccasionKey.Arafah, info.Occasion);
        Assert.Equal(FastKind.Arafah, info.Fast);
        Assert.True(info.IsMajor);
    }

    [Fact]
    public void Mondays_and_Thursdays_are_fasts_outside_Ramadan_but_not_in_it()
    {
        var monday = new DateOnly(2026, 11, 16);
        Assert.Equal(DayOfWeek.Monday, monday.DayOfWeek);
        Assert.Equal(FastKind.MondayThursday, Occasions.For(monday).Fast);
        var ramadanMonday = Enumerable.Range(0, 29).Select(i => Ramadan1.AddDays(i)).First(d => d.DayOfWeek == DayOfWeek.Monday);
        Assert.Equal(FastKind.None, Occasions.For(ramadanMonday).Fast);
    }

    [Fact]
    public void White_days_are_the_13th_to_15th()
    {
        var days = Enumerable.Range(0, 31).Select(i => new DateOnly(2026, 11, 1).AddDays(i))
            .Where(d => Occasions.For(d).Fast == FastKind.WhiteDays)
            .Select(d => HijriDate.From(d)!.Value.Day).ToArray();
        Assert.Equal([13, 14, 15], days);
    }

    [Fact]
    public void Night_times_split_Maghrib_to_Fajr()
    {
        var maghrib = new DateTime(2026, 11, 16, 16, 58, 0);
        var fajr = new DateTime(2026, 11, 17, 4, 52, 0);
        var n = NightTimes.Between(maghrib, fajr);
        Assert.Equal(new DateTime(2026, 11, 16, 22, 55, 0), n.Midnight);   // 11h54m night, half is 5h57m
        Assert.Equal(new DateTime(2026, 11, 17, 0, 54, 0), n.LastThird);   // two thirds is 7h56m
    }

    [Fact]
    public void Imsak_is_ten_minutes_before_Fajr()
    {
        var fajr = new DateTime(2026, 2, 18, 5, 0, 0);
        Assert.Equal(new DateTime(2026, 2, 18, 4, 50, 0), Ramadan.Imsak(fajr));
    }
}
