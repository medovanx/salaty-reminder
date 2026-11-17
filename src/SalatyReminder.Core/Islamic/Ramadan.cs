namespace SalatyReminder.Core.Islamic;

/// <summary>Ramadan's daily times: Imsak a set number of minutes before Fajr, Iftar at Maghrib.</summary>
public static class Ramadan
{
    /// <summary>Imsak, the customary stop to eating ahead of Fajr (10 minutes in Egypt and most timetables).</summary>
    public const int ImsakMinutes = 10;

    public static DateTime Imsak(DateTime fajr) => fajr.AddMinutes(-ImsakMinutes);

    public static bool IsRamadan(DateOnly date, int adjustDays = 0) => HijriDate.From(date, adjustDays)?.IsRamadan == true;

    /// <summary>Day of Ramadan (1 to 30), or 0 outside it.</summary>
    public static int Day(DateOnly date, int adjustDays = 0) =>
        HijriDate.From(date, adjustDays) is { IsRamadan: true } h ? h.Day : 0;
}
