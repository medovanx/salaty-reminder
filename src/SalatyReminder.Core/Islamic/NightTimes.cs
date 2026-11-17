namespace SalatyReminder.Core.Islamic;

/// <summary>
/// The night from Maghrib to the next Fajr: its middle (the end of Isha's preferred time) and the start
/// of its last third, the best time for Qiyam.
/// </summary>
public readonly record struct NightTimes(DateTime Midnight, DateTime LastThird)
{
    public static NightTimes Between(DateTime maghrib, DateTime nextFajr)
    {
        var night = nextFajr - maghrib;
        return new(Round(maghrib + night / 2), Round(maghrib + night * 2 / 3));
    }

    static DateTime Round(DateTime t) => new DateTime(t.Ticks - t.Ticks % TimeSpan.TicksPerMinute, t.Kind)
        .AddMinutes(t.Second >= 30 ? 1 : 0);
}
