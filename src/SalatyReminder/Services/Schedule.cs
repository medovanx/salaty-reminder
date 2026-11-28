namespace SalatyReminder.Services;

/// <summary>
/// Prayer times for one place, in that place's own time zone. The default city (<paramref name="city"/> null)
/// is what the widget, the hover card and every reminder follow; a saved city is only shown on the card.
/// </summary>
sealed class Schedule(Func<AppSettings> settings, SavedCity? city)
{
    readonly Dictionary<DateOnly, DayTimes> cache = [];
    string cacheKey = "";

    AppSettings S => settings();

    public bool IsDefault => city == null;
    public string Name => city?.Name ?? S.LocationName;

    /// <summary>The city's own time zone; the default city is where this computer is.</summary>
    public TimeZoneInfo Zone { get; } = city == null ? TimeZoneInfo.Local : SavedCity.ZoneOf(city.TimeZone);

    /// <summary>The time now on the city's clock.</summary>
    public DateTime Now => city == null ? Clock.Now : TimeZoneInfo.ConvertTime(Clock.Now, Zone);

    double Lat => city?.Latitude ?? S.Latitude;
    double Lon => city?.Longitude ?? S.Longitude;

    public DayTimes TimesFor(DateOnly date)
    {
        // Settings can change under us (method, Asr, a new default location): start the cache over.
        string key = $"{Lat}|{Lon}|{S.Method}|{S.HanafiAsr}";
        if (key != cacheKey || cache.Count > 14) { cache.Clear(); cacheKey = key; }
        if (!cache.TryGetValue(date, out var t))
            cache[date] = t = PrayerCalc.Compute(date, Lat, Lon, Methods.Get(S.Method), S.HanafiAsr, Zone);
        return t;
    }

    public DayTimes TodayAt(DateTime now) => TimesFor(DateOnly.FromDateTime(now));

    /// <summary>Next of the five prayers (sunrise excluded); after Isha this is tomorrow's Fajr.</summary>
    public (Prayer prayer, DateTime at) GetNext(DateTime now)
    {
        var today = TodayAt(now);
        foreach (var p in PrayerCalc.FivePrayers)
            if (today[p] > now) return (p, today[p]);
        return (Prayer.Fajr, TimesFor(today.Date.AddDays(1))[Prayer.Fajr]);
    }

    /// <summary>
    /// What is pointed to next: from Fajr until Duha starts that is Duha (shown on the Sunrise row, whose line
    /// describes it), otherwise the next of the five prayers.
    /// </summary>
    public (Prayer row, DateTime at, string name) GetNextShown(DateTime now)
    {
        var today = TodayAt(now);
        var duha = today[Prayer.Sunrise].AddMinutes(SalatyApp.DuhaMinutes);
        if (now >= today[Prayer.Fajr] && now < duha) return (Prayer.Sunrise, duha, L.T("Duha"));
        var (p, at) = GetNext(now);
        return (p, at, L.PrayerName(p, at));
    }

    /// <summary>The most recent of the five prayers; before Fajr this is (approximately) yesterday's Isha.</summary>
    public (Prayer prayer, DateTime at) GetPrevious(DateTime now)
    {
        var today = TodayAt(now);
        for (int i = PrayerCalc.FivePrayers.Length - 1; i >= 0; i--)
        {
            var p = PrayerCalc.FivePrayers[i];
            if (today[p] <= now) return (p, today[p]);
        }
        return (Prayer.Isha, today[Prayer.Isha].AddDays(-1));
    }

    /// <summary>
    /// In Ramadan (with Ramadan mode on): by day the countdown to Iftar, after Isha and before Fajr the countdown
    /// to the end of Suhoor at Fajr. Null at other times, between Maghrib and Isha, and outside Ramadan.
    /// </summary>
    public (string label, DateTime at)? RamadanTarget(DateTime now)
    {
        if (!S.RamadanMode) return null;
        var today = TodayAt(now);
        var date = today.Date;
        bool fasting = Ramadan.IsRamadan(date, S.HijriAdjust);
        if (fasting && now < today[Prayer.Fajr]) return (L.T("SuhoorEnds"), today[Prayer.Fajr]);
        if (fasting && now < today[Prayer.Maghrib]) return (L.T("Iftar"), today[Prayer.Maghrib]);
        if (now >= today[Prayer.Isha] && Ramadan.IsRamadan(date.AddDays(1), S.HijriAdjust))
            return (L.T("SuhoorEnds"), TimesFor(date.AddDays(1))[Prayer.Fajr]);
        return null;
    }

    /// <summary>The middle and the last third of the night that follows <paramref name="date"/>'s Maghrib.</summary>
    public NightTimes NightAfter(DateOnly date) =>
        NightTimes.Between(TimesFor(date)[Prayer.Maghrib], TimesFor(date.AddDays(1))[Prayer.Fajr]);

    /// <summary>The day from Fajr to Maghrib and the night from Maghrib to the next Fajr, as the fiqh counts them.</summary>
    public (TimeSpan Day, TimeSpan Night) DayAndNight(DateOnly date)
    {
        var day = TimesFor(date);
        return (day[Prayer.Maghrib] - day[Prayer.Fajr], TimesFor(date.AddDays(1))[Prayer.Fajr] - day[Prayer.Maghrib]);
    }
}
