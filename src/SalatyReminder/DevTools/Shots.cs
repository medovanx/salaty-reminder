namespace SalatyReminder.DevTools;

/// <summary>
/// --shots &lt;dir&gt;: renders the card, calendar, settings pages and widget to PNGs for the README and
/// for visual checks, using a fixed Cairo location (never the user's own) and a fixed clock-free layout.
/// </summary>
static class Shots
{
    public static int Run(string dir)
    {
        Directory.CreateDirectory(dir);
        ApplicationConfiguration.Initialize();
        foreach (var lang in new[] { "en", "ar" })
        {
            var s = new AppSettings
            {
                Language = lang, AutoLocation = false, Latitude = 30.0444, Longitude = 31.2357,
                LocationName = lang == "ar" ? "القاهرة، مصر" : "Cairo, Egypt", Method = "Egyptian",
            };
            var app = new SalatyApp(s);

            var card = new PrayerCard(app);
            card.ShowNear(new Rectangle(Screen.PrimaryScreen!.WorkingArea.Right - 300, Screen.PrimaryScreen.WorkingArea.Bottom, 100, 40));
            SaveOffscreen(card, Path.Combine(dir, $"card-{lang}.png"));
            card.OpenCalendarForShot();
            SaveOffscreen(card, Path.Combine(dir, $"calendar-{lang}.png"));
            card.ZoomCalendarForShot();
            SaveOffscreen(card, Path.Combine(dir, $"calendar-months-{lang}.png"));
            card.ZoomCalendarForShot();
            SaveOffscreen(card, Path.Combine(dir, $"calendar-years-{lang}.png"));
            card.Close();

            // The card on the light theme too, to check the deeper light-theme colours.
            Native.ForceDark = false;
            var light = new PrayerCard(app);
            light.ShowNear(new Rectangle(Screen.PrimaryScreen!.WorkingArea.Right - 300, Screen.PrimaryScreen.WorkingArea.Bottom, 100, 40));
            SaveOffscreen(light, Path.Combine(dir, $"card-light-{lang}.png"));
            light.Close();
            Native.ForceDark = null;

            var settings = new SettingsForm(app);
            settings.Show();
            for (int i = 0; i < settings.PageCount; i++)
            {
                settings.ShowPageForShot(i);
                Save(settings, Path.Combine(dir, $"settings-{i}-{lang}.png"));
            }
            settings.ShowUpdatesForShot();
            app.DemoUpdate(true, null, L.T("CheckingUpdates"));
            Save(settings, Path.Combine(dir, $"settings-checking-{lang}.png"));
            app.DemoUpdate(true, 0.42, L.F("UpdateDownloading", "9.9.9", L.Ltr($"42%  {L.Size(34_400_000)} / {L.Size(81_920_000)}")));
            Save(settings, Path.Combine(dir, $"settings-updating-{lang}.png"));
            app.DemoUpdate(false, null, "");
            settings.Close();

            var (p, at) = app.GetNext(DateTime.Now);
            WidgetStrip(Path.Combine(dir, $"widget-{lang}.png"),
                $"{L.PrayerName(p, at)} {L.Time(at, false)}", L.Countdown(at - DateTime.Now), false, lang == "ar");
            WidgetStrip(Path.Combine(dir, $"widget-flash-{lang}.png"),
                $"{L.PrayerName(p, at)} {L.Time(at, false)}", L.T("TimeToPray"), true, lang == "ar");
            WidgetStrip(Path.Combine(dir, $"widget-checking-{lang}.png"),
                $"{L.PrayerName(p, at)} {L.Time(at, false)}", L.T("WidgetChecking"), false, lang == "ar", bar: true);
            WidgetStrip(Path.Combine(dir, $"widget-updating-{lang}.png"),
                $"{L.PrayerName(p, at)} {L.Time(at, false)}", L.F("WidgetUpdating", L.Ltr("42%")), false, lang == "ar", bar: true, barValue: 0.42);
            var menu = app.MenuForShot();
            using (var mb = new Bitmap(menu.Width, menu.Height))
            {
                menu.DrawToBitmap(mb, new Rectangle(Point.Empty, menu.Size));
                mb.Save(Path.Combine(dir, $"menu-{lang}.png"), System.Drawing.Imaging.ImageFormat.Png);
            }
            menu.Close();
        }
        return 0;
    }

    /// <summary>The widget on a dark taskbar-coloured strip, at 150% scale for a crisp README image.</summary>
    static void WidgetStrip(string path, string l1, string l2, bool flash, bool rtl, bool bar = false, double? barValue = null)
    {
        const float s = 1.5f;
        int h = (int)(48 * s);
        using var widget = TaskbarWidget.Paint(l1, l2, flash, false, rtl, true, s, h, bar, barValue, 0.5f);
        using var strip = new Bitmap(widget.Width + (int)(40 * s), h);
        using (var g = Graphics.FromImage(strip))
        {
            g.Clear(Color.FromArgb(28, 28, 28));
            g.DrawImage(widget, (int)(20 * s), 0);
        }
        strip.Save(path, System.Drawing.Imaging.ImageFormat.Png);
    }

    /// <summary>
    /// The card hides itself when it loses focus, so a screen capture could catch whatever is behind it.
    /// It has no text boxes, so the off-screen renderer is exact for it.
    /// </summary>
    static void SaveOffscreen(Form f, string path)
    {
        f.Refresh();
        Application.DoEvents();
        using var bmp = new Bitmap(f.Width, f.Height);
        f.DrawToBitmap(bmp, new Rectangle(Point.Empty, f.Size));
        bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
    }

    /// <summary>Captures the window as it really looks on screen (DrawToBitmap misrenders child text boxes).</summary>
    static void Save(Form f, string path)
    {
        f.TopMost = true;
        f.BringToFront();
        f.Refresh();
        for (int i = 0; i < 10; i++) { Application.DoEvents(); Thread.Sleep(30); }
        var r = f.Bounds;
        // DWMWA_EXTENDED_FRAME_BOUNDS: the visible frame, without the invisible resize border.
        if (Native.DwmGetWindowAttribute(f.Handle, 9, out var wr, System.Runtime.InteropServices.Marshal.SizeOf<Native.RECT>()) == 0)
            r = Rectangle.FromLTRB(wr.Left, wr.Top, wr.Right, wr.Bottom);
        using var bmp = new Bitmap(r.Width, r.Height);
        using (var g = Graphics.FromImage(bmp)) g.CopyFromScreen(r.Location, Point.Empty, r.Size);
        bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
    }
}
