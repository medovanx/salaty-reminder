namespace SalatyReminder;

static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--selftest")
            return SelfTest.Run(args);
        if (args.Length > 1 && args[0] == "--shots")
            return Shots.Run(args[1]);
        if (args.Length > 1 && args[0] == "--render-icon")
            return IconMaker.Run(args[1]);

        using var mutex = new Mutex(false, @"Local\SalatyReminder.SingleInstance");
        bool created;
        try
        {
            // After an update the old version is still exiting: wait for it to let go.
            created = mutex.WaitOne(args.Contains("--updated") ? TimeSpan.FromSeconds(20) : TimeSpan.Zero);
        }
        catch (AbandonedMutexException) { created = true; }
        if (!created)
        {
            // Already running: launching again opens the prayer-times card.
            Native.AllowSetForegroundWindow(-1); // ASFW_ANY: let the running instance take focus
            Native.PostMessage((IntPtr)0xFFFF, Native.RegisterWindowMessage(SalatyApp.ShowCardMessage), IntPtr.Zero, IntPtr.Zero);
            return 0;
        }

        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => Log.Error(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Log.Error(e.ExceptionObject as Exception);
        Application.Run(new SalatyApp());
        return 0;
    }
}

static class Log
{
    public static readonly string Path = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SalatyReminder", "error.log");

    public static void Error(Exception? ex)
    {
        try
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
            File.AppendAllText(Path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex}\r\n\r\n");
        }
        catch { }
    }
}

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
