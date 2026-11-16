namespace SalatyReminder;

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
            Save(card, Path.Combine(dir, $"card-{lang}.png"));
            card.OpenCalendarForShot();
            Save(card, Path.Combine(dir, $"calendar-{lang}.png"));
            card.Close();

            var settings = new SettingsForm(app);
            settings.Show();
            for (int i = 0; i < settings.PageCount; i++)
            {
                settings.ShowPageForShot(i);
                Save(settings, Path.Combine(dir, $"settings-{i}-{lang}.png"));
            }
            settings.Close();

            var (p, at) = app.GetNext(DateTime.Now);
            WidgetStrip(Path.Combine(dir, $"widget-{lang}.png"),
                $"{L.PrayerName(p, at)} {L.Time(at, false)}", L.Countdown(at - DateTime.Now), false, lang == "ar");
            WidgetStrip(Path.Combine(dir, $"widget-flash-{lang}.png"),
                $"{L.PrayerName(p, at)} {L.Time(at, false)}", L.T("TimeToPray"), true, lang == "ar");
        }
        return 0;
    }

    /// <summary>The widget on a dark taskbar-coloured strip, at 150% scale for a crisp README image.</summary>
    static void WidgetStrip(string path, string l1, string l2, bool flash, bool rtl)
    {
        const float s = 1.5f;
        int h = (int)(48 * s);
        using var widget = TaskbarWidget.Paint(l1, l2, flash, false, rtl, true, s, h);
        using var strip = new Bitmap(widget.Width + (int)(40 * s), h);
        using (var g = Graphics.FromImage(strip))
        {
            g.Clear(Color.FromArgb(28, 28, 28));
            g.DrawImage(widget, (int)(20 * s), 0);
        }
        strip.Save(path, System.Drawing.Imaging.ImageFormat.Png);
    }

    static void Save(Form f, string path)
    {
        Application.DoEvents();
        f.Refresh();
        using var bmp = new Bitmap(f.Width, f.Height);
        f.DrawToBitmap(bmp, new Rectangle(Point.Empty, f.Size));
        bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
    }
}
