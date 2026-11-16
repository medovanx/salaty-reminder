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

    public static bool StartWithWindows
    {
        get
        {
            using var k = Registry.CurrentUser.OpenSubKey(RunKey);
            return k?.GetValue(RunName) is string;
        }
        set
        {
            using var k = Registry.CurrentUser.CreateSubKey(RunKey);
            if (value) k.SetValue(RunName, $"\"{Environment.ProcessPath}\"");
            else k.DeleteValue(RunName, false);
        }
    }
}
