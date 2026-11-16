using Microsoft.Win32;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace SalatyReminder;

sealed class SalatyApp : ApplicationContext
{
    public AppSettings S { get; private set; }
    public Icon AppIcon { get; }
    public DayTimes? Today { get; private set; }
    DayTimes? tomorrow;

    const string AdhanResource = "adhan.ogg";
    public static bool HasBundledAdhan => typeof(SalatyApp).Assembly.GetManifestResourceInfo(AdhanResource) != null;

    /// <summary>The built-in adhan, extracted once from the exe to %APPDATA% (the release is a single file).</summary>
    public static string BundledAdhanPath
    {
        get
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SalatyReminder", AdhanResource);
            if (File.Exists(path) || !HasBundledAdhan) return path;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                using var src = typeof(SalatyApp).Assembly.GetManifestResourceStream(AdhanResource)!;
                using var dst = File.Create(path);
                src.CopyTo(dst);
            }
            catch (Exception ex) { Log.Error(ex); }
            return path;
        }
    }

    readonly HostWindow host;
    readonly TaskbarWidget widget = new();
    readonly NotifyIcon tray;
    readonly ContextMenuStrip menu = new();
    readonly System.Windows.Forms.Timer timer = new() { Interval = 1000 };
    readonly HashSet<string> fired = [];
    PrayerCard? card;
    SettingsForm? settingsForm;
    MediaPlayer? player;
    DateTime flashUntil = DateTime.MinValue;
    Prayer flashPrayer;
    int ticks;

    public bool AdhanPlaying { get; private set; }

    /// <summary>Headless instance for --shots: no tray, widget, timers or saving.</summary>
    public bool Headless { get; }

    public SalatyApp(AppSettings? settings = null)
    {
        Headless = settings != null;
        S = settings ?? AppSettings.Load();
        L.Ar = S.Language == "ar";
        AppIcon = LoadIcon();

        host = new HostWindow(this);
        menu.Opening += (_, e) => { BuildMenu(); e.Cancel = false; };

        tray = new NotifyIcon
        {
            Icon = new Icon(AppIcon, SystemInformation.SmallIconSize),
            Text = L.T("AppName"),
            ContextMenuStrip = menu,
            Visible = !Headless,
        };
        if (Headless) { Recompute(); return; }
        tray.MouseClick += (_, e) => { if (e.Button == MouseButtons.Left) ToggleCard(); };
        tray.BalloonTipClicked += (_, _) => { StopAlert(); ShowCard(); };

        widget.Clicked += () => { StopAlert(); ToggleCard(); };
        widget.RightClicked += () =>
        {
            Native.SetForegroundWindow(host.Handle);
            menu.Show(Cursor.Position);
        };

        SystemEvents.TimeChanged += (_, _) => host.BeginInvoke(Recompute);
        SystemEvents.PowerModeChanged += (_, e) => { if (e.Mode == PowerModes.Resume) host.BeginInvoke(Recompute); };
        SystemEvents.DisplaySettingsChanged += (_, _) => host.BeginInvoke(() => { widget.Invalidate(); widget.Ensure(); });
        SystemEvents.UserPreferenceChanged += (_, _) => host.BeginInvoke(widget.Invalidate);

        Recompute();
        // Prayers already past at startup must not fire later today.
        var now = DateTime.Now;
        foreach (var p in PrayerCalc.FivePrayers)
        {
            if (Today![p].AddMinutes(3) <= now) fired.Add(Key("A", p));
            if (Today[p].AddMinutes(-S.EarlyMinutes + 3) <= now) fired.Add(Key("E", p));
        }
        widget.SetPlacement(S.WidgetSide == "Left", S.WidgetOffset);
        widget.Ensure();
        UpdateWidget(now);

        timer.Tick += (_, _) => Tick();
        timer.Start();

        Updater.CleanupOld();
        if (Environment.GetCommandLineArgs().Contains("--updated"))
            tray.ShowBalloonTip(6000, L.T("AppName"), L.F("Updated", Updater.CurrentText), ToolTipIcon.Info);
        updateTimer.Tick += async (_, _) => { updateTimer.Interval = 6 * 3600 * 1000; await CheckForUpdatesAsync(false); };
        updateTimer.Start(); // first check 45 s after start, then every 6 hours

        if (S.AutoLocation && (DateTime.UtcNow - S.LocationUpdatedUtc).TotalHours > 6)
            _ = RefreshLocationAsync(userInitiated: false);
    }

    static Icon LoadIcon()
    {
        using var stream = typeof(SalatyApp).Assembly.GetManifestResourceStream("app.ico");
        return stream != null ? new Icon(stream) : SystemIcons.Application;
    }

    string Key(string kind, Prayer p) => $"{Today?.Date:yyyy-MM-dd}:{kind}:{p}";

    void Recompute()
    {
        var date = DateOnly.FromDateTime(DateTime.Now);
        if (Today?.Date != date) fired.Clear();
        var m = Methods.Get(S.Method);
        Today = PrayerCalc.Compute(date, S.Latitude, S.Longitude, m, S.HanafiAsr);
        tomorrow = PrayerCalc.Compute(date.AddDays(1), S.Latitude, S.Longitude, m, S.HanafiAsr);
        UpdateWidget(DateTime.Now);
        if (card?.Visible == true) card.Invalidate();
    }

    void Tick()
    {
        var now = DateTime.Now;
        if (Today == null || DateOnly.FromDateTime(now) != Today.Date) Recompute();
        CheckReminders(now);
        if (++ticks % 2 == 0) widget.Ensure(); // handles Explorer restarts and tray width changes
        UpdateWidget(now);
    }

    /// <summary>Next of the five prayers (sunrise excluded); after Isha this is tomorrow's Fajr.</summary>
    public (Prayer prayer, DateTime at) GetNext(DateTime now)
    {
        foreach (var p in PrayerCalc.FivePrayers)
            if (Today![p] > now) return (p, Today[p]);
        return (Prayer.Fajr, tomorrow![Prayer.Fajr]);
    }

    public DayTimes TimesFor(DateOnly date) =>
        PrayerCalc.Compute(date, S.Latitude, S.Longitude, Methods.Get(S.Method), S.HanafiAsr);

    /// <summary>The most recent of the five prayers; before Fajr this is (approximately) yesterday's Isha.</summary>
    public (Prayer prayer, DateTime at) GetPrevious(DateTime now)
    {
        for (int i = PrayerCalc.FivePrayers.Length - 1; i >= 0; i--)
        {
            var p = PrayerCalc.FivePrayers[i];
            if (Today![p] <= now) return (p, Today[p]);
        }
        return (Prayer.Isha, Today![Prayer.Isha].AddDays(-1));
    }

    void UpdateWidget(DateTime now)
    {
        if (Today == null) return;
        bool flashing = now < flashUntil;
        if (flashing)
        {
            var t = Today[flashPrayer];
            widget.SetContent($"{L.PrayerName(flashPrayer, t)} {L.Time(t, S.Use24h)}", L.T("TimeToPray"), now.Second % 2 == 0, L.Ar);
        }
        else
        {
            var (p, at) = GetNext(now);
            widget.SetContent($"{L.PrayerName(p, at)} {L.Time(at, S.Use24h)}", L.Countdown(at - now), false, L.Ar);
        }
        tray.Text = Truncate($"{L.T("AppName")}: {L.PrayerName(GetNext(now).prayer, now)} {L.Time(GetNext(now).at, S.Use24h)}", 127);
    }

    static string Truncate(string s, int n) => s.Length <= n ? s : s[..n];

    void CheckReminders(DateTime now)
    {
        foreach (var p in PrayerCalc.FivePrayers)
        {
            var t = Today![p];
            var name = L.PrayerName(p, t);
            if (S.EarlyNotify && S.EarlyMinutes > 0)
            {
                var e = t.AddMinutes(-S.EarlyMinutes);
                if (now >= e && now < e.AddMinutes(3) && fired.Add(Key("E", p)))
                    tray.ShowBalloonTip(10000, L.F("EarlyTitle", name, S.EarlyMinutes),
                        $"{L.Time(t, S.Use24h)}, {S.LocationName}", ToolTipIcon.None);
            }
            if (now >= t && now < t.AddMinutes(3) && fired.Add(Key("A", p)))
            {
                if (S.NotifyAtTime)
                    tray.ShowBalloonTip(15000, L.F("NotifTitle", name), $"{L.Time(t, S.Use24h)}, {S.LocationName}", ToolTipIcon.None);
                if (S.FlashWidget) { flashPrayer = p; flashUntil = now.AddMinutes(10); }
                if (S.PlayAdhan) PlayAdhan(S.AdhanFile);
            }
        }
    }

    /// <summary>Plays the given file, or the built-in adhan when the path is empty.</summary>
    public bool PlayAdhan(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) path = BundledAdhanPath;
        if (!File.Exists(path)) return false;
        try
        {
            if (player == null)
            {
                player = new MediaPlayer();
                player.MediaEnded += (_, _) => AdhanPlaying = false;
                player.MediaFailed += (_, _) => AdhanPlaying = false;
            }
            player.Source = MediaSource.CreateFromUri(new Uri(path));
            player.Play();
            AdhanPlaying = true;
            return true;
        }
        catch { return false; }
    }

    public void StopAdhan()
    {
        if (player == null) return;
        player.Pause();
        player.Source = null;
        AdhanPlaying = false;
    }

    void StopAlert()
    {
        StopAdhan();
        flashUntil = DateTime.MinValue;
        UpdateWidget(DateTime.Now);
    }

    void ToggleCard()
    {
        if (card?.Visible == true) { card.Hide(); return; }
        // A click on the widget first deactivates (hides) the card; don't reopen it on that same click.
        if (card != null && (DateTime.Now - card.LastHidden).TotalMilliseconds < 300) return;
        ShowCard();
    }

    void ShowCard()
    {
        card ??= new PrayerCard(this);
        var anchor = widget.ScreenBounds ?? new Rectangle(Cursor.Position, new Size(1, 1));
        card.ShowNear(anchor);
    }

    public void ShowSettings()
    {
        if (settingsForm is { IsDisposed: false }) { settingsForm.Activate(); return; }
        settingsForm = new SettingsForm(this);
        settingsForm.Show();
        settingsForm.Activate();
    }

    public void ApplySettings(AppSettings s, bool relocate)
    {
        S = s;
        widget.SetPlacement(S.WidgetSide == "Left", S.WidgetOffset);
        L.Ar = S.Language == "ar";
        S.Save();
        card?.Dispose();
        card = null;
        Recompute();
        widget.Invalidate();
        if (relocate) _ = RefreshLocationAsync(userInitiated: false);
    }

    public async Task RefreshLocationAsync(bool userInitiated)
    {
        var r = await LocationService.DetectAsync(S.Language);
        if (r == null)
        {
            if (userInitiated)
                tray.ShowBalloonTip(8000, L.T("AppName"), L.F("LocFailed", S.LocationName), ToolTipIcon.Warning);
            return;
        }
        if (!S.AutoLocation && !userInitiated) return;
        S.AutoLocation = true;
        S.Latitude = r.Lat;
        S.Longitude = r.Lon;
        S.LocationName = r.Name;
        S.LocationUpdatedUtc = DateTime.UtcNow;
        S.Save();
        Recompute();
    }

    readonly System.Windows.Forms.Timer updateTimer = new() { Interval = 45_000 };
    Updater.Release? pendingUpdate;
    bool installing;

    /// <summary>Checks GitHub; installs automatically when allowed and not near a prayer. Returns a status line.</summary>
    public async Task<string> CheckForUpdatesAsync(bool userInitiated)
    {
        if (!userInitiated && !S.AutoCheckUpdates) return "";
        Updater.Release? rel;
        try { rel = await Updater.CheckAsync(); }
        catch (Exception ex)
        {
            Log.Error(ex);
            return L.T("UpdateCheckFailed");
        }
        S.LastUpdateCheckUtc = DateTime.UtcNow;
        S.Save();
        if (rel == null) return L.F("UpToDate", Updater.CurrentText);

        pendingUpdate = rel;
        string ver = rel.Version.ToString(3);
        if (userInitiated || (S.AutoInstallUpdates && !NearPrayer()))
        {
            _ = InstallUpdateAsync(rel);
            return L.F("Updating", ver);
        }
        tray.ShowBalloonTip(10000, L.T("AppName"), L.F("UpdateAvailable", ver), ToolTipIcon.Info);
        return L.F("UpdateAvailable", ver);
    }

    /// <summary>Don't restart during the adhan or in the few minutes around a prayer.</summary>
    bool NearPrayer()
    {
        if (AdhanPlaying || DateTime.Now < flashUntil) return true;
        var now = DateTime.Now;
        return PrayerCalc.FivePrayers.Any(p => Math.Abs((Today![p] - now).TotalMinutes) < 15);
    }

    async Task InstallUpdateAsync(Updater.Release rel)
    {
        if (installing) return;
        installing = true;
        tray.ShowBalloonTip(4000, L.T("AppName"), L.F("Updating", rel.Version.ToString(3)), ToolTipIcon.Info);
        if (await Updater.InstallAsync(rel))
        {
            Exit();
            return;
        }
        installing = false;
        tray.ShowBalloonTip(8000, L.T("AppName"), L.T("UpdateFailed"), ToolTipIcon.Warning);
        Updater.OpenPage(rel.PageUrl);
    }

    void BuildMenu()
    {
        menu.Items.Clear();
        menu.RightToLeft = L.Ar ? RightToLeft.Yes : RightToLeft.No;
        menu.Items.Add(L.T("ShowTimes"), null, (_, _) => ShowCard()).Font = new Font(menu.Font, FontStyle.Bold);
        if (AdhanPlaying) menu.Items.Add(L.T("StopAdhan"), null, (_, _) => StopAlert());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(L.T("RefreshLocation"), null, async (_, _) => await RefreshLocationAsync(userInitiated: true));
        menu.Items.Add(L.T("Settings"), null, (_, _) => ShowSettings());
        menu.Items.Add(L.T("CheckForUpdates"), null, async (_, _) =>
        {
            var msg = await CheckForUpdatesAsync(userInitiated: true);
            if (!installing) tray.ShowBalloonTip(6000, L.T("AppName"), msg, ToolTipIcon.Info);
        });
        var startup = new ToolStripMenuItem(L.T("StartWithWindows")) { Checked = AppSettings.StartWithWindows };
        startup.Click += (_, _) => { try { AppSettings.StartWithWindows = !startup.Checked; } catch { } };
        menu.Items.Add(startup);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(L.T("Exit"), null, (_, _) => Exit());
    }

    public const string ShowCardMessage = "SalatyReminder.ShowCard";

    public void OnTaskbarCreated() => widget.Ensure();

    public void ShowCardFromOutside()
    {
        if (card?.Visible == true) return;
        ShowCard();
    }

    void Exit()
    {
        timer.Stop();
        StopAdhan();
        tray.Visible = false;
        tray.Dispose();
        if (widget.Handle != IntPtr.Zero) widget.DestroyHandle();
        ExitThread();
    }

    /// <summary>Hidden top-level window: receives TaskbarCreated (Explorer restart) and owns menus.</summary>
    sealed class HostWindow : Form
    {
        readonly SalatyApp app;
        readonly uint taskbarCreated = Native.RegisterWindowMessage("TaskbarCreated");
        readonly uint showCard = Native.RegisterWindowMessage(ShowCardMessage);

        public HostWindow(SalatyApp app)
        {
            this.app = app;
            ShowInTaskbar = false;
            _ = Handle;
        }

        protected override void SetVisibleCore(bool value) => base.SetVisibleCore(false);

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == (int)taskbarCreated) app.OnTaskbarCreated();
            else if (m.Msg == (int)showCard) app.ShowCardFromOutside();
            base.WndProc(ref m);
        }
    }
}
