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
    ContextMenuStrip? menu;
    readonly System.Windows.Forms.Timer timer = new() { Interval = 1000 };
    /// <summary>Slides the widget's update bar while the end isn't known.</summary>
    readonly System.Windows.Forms.Timer barTimer = new() { Interval = 40 };
    float barPhase = 0.4f;
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
        barTimer.Tick += (_, _) => { barPhase = (barPhase + 0.02f) % 1f; SyncWidgetBar(); };

        if (Headless) { Recompute(); return; }
        // No tray icon: the widget is the app's face, and notifications are real Windows toasts.
        Toast.Init(AppIcon, a => host.BeginInvoke(a));
        if (Environment.GetCommandLineArgs().Contains("--test-toast"))
            Toast.Show(L.F("NotifTitle", L.PrayerName(Prayer.Fajr, DateTime.Now)), $"4:40 AM, {S.LocationName}", ShowCard);

        widget.Clicked += () => { StopAlert(); ToggleCard(); };
        widget.RightClicked += () =>
        {
            Native.SetForegroundWindow(host.Handle);
            // A fresh menu each time: it reflects the current language, theme and update state.
            var m = BuildMenu();
            menu = m;
            m.Closed += (_, _) => host.BeginInvoke(() => { m.Dispose(); if (menu == m) menu = null; });
            m.Show(Cursor.Position);
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
            Toast.Show(L.T("AppName"), L.F("Updated", Updater.CurrentText));
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
            // While an update runs the second line says so, with the bar under it.
            string line2 = !UpdateBusy ? L.Countdown(at - now)
                : UpdateFraction is { } f && f < 1 ? L.F("WidgetUpdating", L.Ltr($"{f * 100:0}%"))
                : L.T(installing ? "WidgetInstalling" : "WidgetChecking");
            widget.SetContent($"{L.PrayerName(p, at)} {L.Time(at, S.Use24h)}", line2, false, L.Ar);
        }
    }

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
                    Toast.Show(L.F("EarlyTitle", name, S.EarlyMinutes), $"{L.Time(t, S.Use24h)}, {S.LocationName}", ShowCard);
            }
            if (now >= t && now < t.AddMinutes(3) && fired.Add(Key("A", p)))
            {
                if (S.NotifyAtTime)
                    Toast.Show(L.F("NotifTitle", name), $"{L.Time(t, S.Use24h)}, {S.LocationName}",
                        () => { StopAlert(); ShowCard(); }, silent: S.PlayAdhan);
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
                Toast.Show(L.T("AppName"), L.F("LocFailed", S.LocationName));
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

    /// <summary>True while checking for or installing an update, so the Updates page can show progress.</summary>
    public bool UpdateBusy { get; private set; }
    /// <summary>0 to 1 while downloading, null when the end isn't known (checking, verifying).</summary>
    public double? UpdateFraction { get; private set; }
    /// <summary>What the update is doing now, or its last result.</summary>
    public string UpdateStatus { get; private set; } = "";
    public event Action? UpdateChanged;

    /// <summary>For --shots: shows an update in progress without downloading anything.</summary>
    public void DemoUpdate(bool busy, double? fraction, string status) => SetUpdate(busy, fraction, status);

    void SetUpdate(bool busy, double? fraction, string status)
    {
        UpdateBusy = busy; UpdateFraction = fraction; UpdateStatus = status;
        barTimer.Enabled = busy && fraction == null && !Headless;
        UpdateWidget(DateTime.Now);
        SyncWidgetBar();
        UpdateChanged?.Invoke();
    }

    /// <summary>Checks GitHub; installs automatically when allowed and not near a prayer. Returns a status line.</summary>
    public async Task<string> CheckForUpdatesAsync(bool userInitiated)
    {
        if (!userInitiated && !S.AutoCheckUpdates) return "";
        if (installing) return UpdateStatus;
        Updater.Release? rel;
        if (userInitiated) SetUpdate(true, null, L.T("CheckingUpdates"));
        try { rel = await Updater.CheckAsync(); }
        catch (Exception ex)
        {
            Log.Error(ex);
            if (userInitiated) SetUpdate(false, null, L.T("UpdateCheckFailed"));
            return L.T("UpdateCheckFailed");
        }
        S.LastUpdateCheckUtc = DateTime.UtcNow;
        S.Save();
        if (rel == null)
        {
            if (userInitiated) SetUpdate(false, null, L.F("UpToDate", Updater.CurrentText));
            return L.F("UpToDate", Updater.CurrentText);
        }

        pendingUpdate = rel;
        string ver = rel.Version.ToString(3);
        if (userInitiated || (S.AutoInstallUpdates && !NearPrayer()))
        {
            _ = InstallUpdateAsync(rel);
            return L.F("Updating", ver);
        }
        Toast.Show(L.T("AppName"), L.F("UpdateAvailable", ver), () => _ = InstallUpdateAsync(rel));
        SetUpdate(false, null, L.F("UpdateAvailable", ver));
        return L.F("UpdateAvailable", ver);
    }

    void SyncWidgetBar()
    {
        if (!Headless) widget.SetProgress(UpdateBusy, UpdateFraction, barPhase);
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
        string ver = rel.Version.ToString(3);
        Toast.Show(L.T("AppName"), L.F("Updating", ver));
        SetUpdate(true, 0, L.F("Updating", ver));
        // Progress<T> made here posts its reports back to the UI thread.
        var progress = new Progress<Updater.Progress>(p =>
        {
            if (!installing) return;
            if (p.Verifying) SetUpdate(true, null, L.T("UpdateVerifying"));
            else if (p.Total > 0)
                SetUpdate(true, (double)p.Done / p.Total,
                    L.F("UpdateDownloading", ver, L.Ltr($"{100.0 * p.Done / p.Total:0}%  {L.Size(p.Done)} / {L.Size(p.Total)}")));
            else SetUpdate(true, null, L.F("UpdateDownloading", ver, L.Ltr(L.Size(p.Done))));
        });
        if (await Updater.InstallAsync(rel, progress))
        {
            SetUpdate(true, 1, L.F("UpdateRestarting", ver));
            Exit();
            return;
        }
        installing = false;
        SetUpdate(false, null, L.T("UpdateFailed"));
        Toast.Show(L.T("AppName"), L.T("UpdateFailed"));
        Updater.OpenPage(rel.PageUrl);
    }

    /// <summary>For --shots: the widget's right-click menu, built and shown off screen.</summary>
    public ContextMenuStrip MenuForShot()
    {
        var m = BuildMenu();
        m.Show(new Point(-3000, -3000));
        return m;
    }

    /// <summary>The widget's right-click menu in the app's style: theme surface, rounded rows and corners, icons.</summary>
    ContextMenuStrip BuildMenu()
    {
        var t = Theme.Current;
        var m = new ContextMenuStrip { RightToLeft = L.Ar ? RightToLeft.Yes : RightToLeft.No };
        ToolStripMenuItem Item(string text, string? glyph, Action? click = null)
        {
            var it = new ToolStripMenuItem(text) { Tag = glyph == null ? null : new ThemedMenuRenderer.MenuGlyph(glyph) };
            if (click != null) it.Click += (_, _) => click();
            return it;
        }

        // The app and its version, as a quiet header.
        int iconPx = (int)(16 * m.DeviceDpi / 96f);
        m.Items.Add(new ToolStripMenuItem($"{L.T("AppName")}  {L.Ltr("v" + Updater.CurrentText)}") { Enabled = false, Image = new Icon(AppIcon, iconPx, iconPx).ToBitmap() });
        m.Items.Add(new ToolStripSeparator());
        var show = Item(L.T("ShowTimes"), Art.IconCalendar, ShowCard);
        show.Font = new Font("Segoe UI Semibold", 9.5f);
        m.Items.Add(show);
        if (AdhanPlaying) m.Items.Add(Item(L.T("StopAdhan"), "", StopAlert));
        m.Items.Add(new ToolStripSeparator());
        m.Items.Add(Item(L.T("RefreshLocation"), Art.IconLocation, () => _ = RefreshLocationAsync(userInitiated: true)));

        // Language: applied at once; an open Settings window closes so it can't save the old one back.
        var lang = Item(L.T("Language"), "");
        foreach (var (code, name) in new[] { ("en", "English"), ("ar", "العربية") })
        {
            var item = new ToolStripMenuItem(name) { Checked = S.Language == code };
            item.Click += (_, _) => SetLanguage(code);
            lang.DropDownItems.Add(item);
        }
        m.Items.Add(lang);

        m.Items.Add(new ToolStripSeparator());
        m.Items.Add(Item(L.T("Settings"), Art.IconSettings, ShowSettings));
        // While an update is checked for or installed, the item says how far it is instead (the widget shows a bar too).
        if (UpdateBusy) m.Items.Add(new ToolStripMenuItem(UpdateStatus) { Enabled = false, Tag = new ThemedMenuRenderer.MenuGlyph("") });
        else m.Items.Add(Item(L.T("CheckForUpdates"), "", async () =>
        {
            var msg = await CheckForUpdatesAsync(userInitiated: true);
            if (!installing) Toast.Show(L.T("AppName"), msg);
        }));
        var startup = new ToolStripMenuItem(L.T("StartWithWindows")) { Checked = AppSettings.StartWithWindows };
        startup.Click += (_, _) => { try { AppSettings.StartWithWindows = !startup.Checked; } catch { } };
        m.Items.Add(startup);
        m.Items.Add(new ToolStripSeparator());
        m.Items.Add(Item(L.T("Exit"), "", Exit));

        ThemedMenuRenderer.Style(m, t);
        return m;
    }

    void SetLanguage(string code)
    {
        if (S.Language == code) return;
        if (settingsForm is { IsDisposed: false }) settingsForm.Close();
        var s = S.Clone();
        s.Language = code;
        ApplySettings(s, relocate: false);
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
