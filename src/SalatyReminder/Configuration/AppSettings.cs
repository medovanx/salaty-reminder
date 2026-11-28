using System.Globalization;
using System.Text.Json;
using Microsoft.Win32;

namespace SalatyReminder.Configuration;

public sealed class AppSettings
{
    public string Language { get; set; } = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar" ? "ar" : "en";

    public bool AutoLocation { get; set; } = true;
    public double Latitude { get; set; } = 30.0444;
    public double Longitude { get; set; } = 31.2357;
    public string LocationName { get; set; } = "Cairo, Egypt";
    public DateTime LocationUpdatedUtc { get; set; }

    /// <summary>
    /// Other cities to look at on the card. The location above stays the default: the widget, the hover card and
    /// every reminder follow it, whichever city the card shows.
    /// </summary>
    public List<SavedCity> Cities { get; set; } = [];
    /// <summary>The city the card shows: -1 for the default, otherwise an index into <see cref="Cities"/>.</summary>
    public int ActiveCity { get; set; } = -1;

    public string Method { get; set; } = "Egyptian";
    public bool HanafiAsr { get; set; }
    public bool Use24h { get; set; }
    public int HijriAdjust { get; set; }

    public bool NotifyAtTime { get; set; } = true;
    public bool EarlyNotify { get; set; } = true;
    public int EarlyMinutes { get; set; } = 10;
    public bool FlashWidget { get; set; } = true;
    public bool PlayAdhan { get; set; }
    public string AdhanFile { get; set; } = "";
    public bool ShowSunnah { get; set; } = true;

    /// <summary>In Ramadan the widget counts down to Iftar by day and to the end of Suhoor by night.</summary>
    public bool RamadanMode { get; set; } = true;
    /// <summary>Islamic occasions and sunnah fasting days on the calendar and the card.</summary>
    public bool ShowOccasions { get; set; } = true;
    /// <summary>A reminder the evening before a sunnah fast (Monday, Thursday, white days, Arafah, Ashura).</summary>
    public bool FastingReminders { get; set; }
    /// <summary>Friday: Surat Al-Kahf in the morning and Jumu'ah ahead of time.</summary>
    public bool FridayReminders { get; set; } = true;
    /// <summary>Morning adhkar after Fajr and evening adhkar after Asr.</summary>
    public bool AdhkarReminders { get; set; } = true;
    /// <summary>The middle of the night (Maghrib to Fajr) and its last third under the prayer list.</summary>
    public bool ShowNightTimes { get; set; } = true;

    /// <summary>"Right" = beside the tray, "Left" = left end of the taskbar.</summary>
    public string WidgetSide { get; set; } = "Right";
    /// <summary>Extra shift in pixels (at 100% scale), positive = towards the centre.</summary>
    public int WidgetOffset { get; set; }

    public bool AutoCheckUpdates { get; set; } = true;
    public bool AutoInstallUpdates { get; set; } = true;
    public DateTime LastUpdateCheckUtc { get; set; }

    static readonly string Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SalatyReminder");
    static readonly string FilePath = Path.Combine(Dir, "settings.json");
    static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new();
        }
        catch { /* corrupt file: fall back to defaults */ }
        return new();
    }

    public void Save()
    {
        Directory.CreateDirectory(Dir);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this, Json));
    }

    public AppSettings Clone() => JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(this))!;

    // Autostart lives in the registry, not the settings file.
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string RunName = "SalatyReminder";

    // Recent Windows 11 builds skip a Run entry that has no "enabled" mark in StartupApproved (the list Task
    // Manager's Startup apps page edits), so turning it on writes both.
    const string ApprovedKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";

    public static bool StartWithWindows
    {
        get
        {
            using var k = Registry.CurrentUser.OpenSubKey(RunKey);
            if (k?.GetValue(RunName) is not string) return false;
            // Turned off in Task Manager: the first byte of the mark is odd (03), on is even (02).
            using var a = Registry.CurrentUser.OpenSubKey(ApprovedKey);
            return a?.GetValue(RunName) is not byte[] mark || mark.Length == 0 || (mark[0] & 1) == 0;
        }
        set
        {
            using var k = Registry.CurrentUser.CreateSubKey(RunKey);
            using var a = Registry.CurrentUser.CreateSubKey(ApprovedKey);
            if (value)
            {
                k.SetValue(RunName, $"\"{Environment.ProcessPath}\"");
                a.SetValue(RunName, new byte[] { 0x02, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, RegistryValueKind.Binary);
            }
            else
            {
                k.DeleteValue(RunName, false);
                a.DeleteValue(RunName, false);
            }
        }
    }
}

/// <summary>A saved city: its name, coordinates and IANA time zone (from the city search).</summary>
public sealed record SavedCity(string Name, double Latitude, double Longitude, string TimeZone)
{
    /// <summary>The city's time zone, or this computer's if Windows doesn't know the id.</summary>
    public static TimeZoneInfo ZoneOf(string id)
    {
        if (!string.IsNullOrEmpty(id) && TimeZoneInfo.TryFindSystemTimeZoneById(id, out var tz)) return tz;
        Services.Log.Error(new TimeZoneNotFoundException($"Unknown time zone '{id}', using this computer's"));
        return TimeZoneInfo.Local;
    }
}
