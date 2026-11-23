using System.Drawing.Drawing2D;

namespace SalatyReminder.UI.Taskbar;

/// <summary>
/// A small card above the widget after the mouse rests on it: today's Hijri date and occasion, the six times,
/// Imsak and Iftar in Ramadan, and the middle and last third of the night. It never takes focus and hides as soon
/// as the mouse leaves; clicks pass through to the widget.
/// </summary>
sealed class HoverCard : Form
{
    const int W = 236;
    readonly System.Windows.Forms.Timer delay = new() { Interval = 450 };
    readonly SalatyApp app;
    Rectangle? anchor;
    Func<bool>? suppressed;
    bool dark;

    public HoverCard(SalatyApp app)
    {
        this.app = app;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        DoubleBuffered = true;
        delay.Tick += (_, _) => { delay.Stop(); ShowNow(); };
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ClassStyle |= 0x20000;               // CS_DROPSHADOW
            cp.ExStyle |= 0x80 | 0x08000000 | 0x20; // TOOLWINDOW | NOACTIVATE | TRANSPARENT (clicks pass through)
            return cp;
        }
    }

    float S => DeviceDpi / 96f;
    int Px(float v) => (int)Math.Round(v * S);

    /// <param name="suppress">Checked again when the delay ends: the card must never cover the open prayer card.</param>
    public void ShowSoon(Rectangle? widget, Func<bool>? suppress = null)
    {
        anchor = widget;
        suppressed = suppress;
        delay.Stop();
        delay.Start();
    }

    public void Cancel()
    {
        delay.Stop();
        Hide();
    }

    /// <summary>Lays the card out off screen for --shots.</summary>
    public void ShowForShot(bool darkTheme)
    {
        dark = darkTheme;
        Layout(new Rectangle(-4000, -4000, 10, 10));
        Location = new Point(-4000, -4000);
        Show();
    }

    // Rows: Hijri date, the day's label (if any), six times, then Ramadan and night lines when they apply.
    string DayLabel => app.S.ShowOccasions ? L.DayLabel(Occasions.For(DateOnly.FromDateTime(Clock.Now), app.S.HijriAdjust), Clock.Now.DayOfWeek) : "";
    bool RamadanToday => app.S.RamadanMode && Ramadan.IsRamadan(DateOnly.FromDateTime(Clock.Now), app.S.HijriAdjust);

    void Layout(Rectangle a)
    {
        RightToLeft = L.Ar ? RightToLeft.Yes : RightToLeft.No;
        BackColor = new Theme(dark).Surface;
        float h = 12 + 20 + (DayLabel.Length > 0 ? 18 : 0) + 8 + 6 * 24 + 6;
        if (RamadanToday) h += 6 + 2 * 20;
        if (app.S.ShowNightTimes) h += 6 + 2 * 20;
        Size = new Size(Px(W), Px(h + 6));
        var wa = Screen.FromRectangle(a).WorkingArea;
        int x = Math.Clamp(a.Left + a.Width / 2 - Width / 2, wa.Left + Px(6), wa.Right - Width - Px(6));
        Location = new Point(x, wa.Bottom - Height - Px(8));
    }

    void ShowNow()
    {
        if (anchor is not { } a || suppressed?.Invoke() == true || app.Today == null) return;
        dark = Native.SystemDark();
        Layout(a);
        Invalidate();
        // SW_SHOWNOACTIVATE: appear without taking focus, so whatever is active stays active.
        Native.ShowWindow(Handle, 4);
        Visible = true;
        Native.RoundCorners(Handle, dark);
    }

    Rectangle Box(float x, float y, float w, float h)
    {
        var r = new Rectangle(Px(x), Px(y), Px(w), Px(h));
        if (L.Ar) r.X = Width - r.Right;
        return r;
    }

    void Draw(Graphics g, string s, Font f, Color c, Rectangle r, bool far = false, bool center = false)
    {
        var flags = TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter;
        flags |= center ? TextFormatFlags.HorizontalCenter : (far ^ L.Ar) ? TextFormatFlags.Right : TextFormatFlags.Left;
        if (L.Ar) flags |= TextFormatFlags.RightToLeft;
        TextRenderer.DrawText(g, s, f, r, c, flags);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var today = app.Today;
        if (today == null) return;
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var t = new Theme(dark);
        var now = Clock.Now;
        bool h24 = app.S.Use24h;
        using var fHead = new Font("Segoe UI Semibold", 9.5f);
        using var fSmall = new Font("Segoe UI", 8.5f);
        using var fRow = new Font("Segoe UI", 9.5f);
        using var fRowB = new Font("Segoe UI Semibold", 9.5f);

        float y = 12;
        Draw(g, L.Hijri(now, app.S.HijriAdjust), fHead, t.Accent, Box(14, y, W - 28, 20));
        y += 20;
        if (DayLabel is { Length: > 0 } label) { Draw(g, label, fSmall, Art.GoldText(dark), Box(14, y, W - 28, 18)); y += 18; }
        y += 4;
        using (var pen = new Pen(t.Border)) g.DrawLine(pen, Px(12), Px(y), Width - Px(12), Px(y));
        y += 4;

        var (next, _, _) = app.GetNextShown(now);
        foreach (var p in Enum.GetValues<Prayer>())
        {
            // The sunrise row is Duha, at the time it starts.
            var at = p == Prayer.Sunrise ? today[p].AddMinutes(SalatyApp.DuhaMinutes) : today[p];
            bool isNext = p == next && at.Date == now.Date;
            bool past = at <= now && !isNext;
            if (isNext)
                Art.FillRound(g, new RectangleF(Px(6), Px(y + 1), Width - Px(12), Px(22)), Px(6), Color.FromArgb(dark ? 40 : 26, t.Accent));
            Color c = isNext ? t.Accent : past ? t.Dim : t.Text;
            Art.PrayerGlyph(g, p, Box(14, y + 4, 16, 16), dark, past, past ? Color.FromArgb(120, t.Dim) : t.Dim);
            Draw(g, p == Prayer.Sunrise ? L.T("Duha") : L.PrayerName(p, at), isNext ? fRowB : fRow, c, Box(38, y, 110, 24));
            Draw(g, L.Time(at, h24), isNext ? fRowB : fRow, c, Box(130, y, W - 144, 24), far: true);
            y += 24;
        }

        // Extra rows under a thin rule, each a label and a time like the prayers above, so nothing gets cut off.
        void Extra(string label, DateTime at, Color c)
        {
            Draw(g, label, fSmall, c, Box(14, y, 150, 20));
            Draw(g, L.Time(at, h24), fSmall, c, Box(130, y, W - 144, 20), far: true);
            y += 20;
        }
        void Rule()
        {
            y += 3;
            using var pen = new Pen(t.Border);
            g.DrawLine(pen, Px(12), Px(y), Width - Px(12), Px(y));
            y += 3;
        }
        if (RamadanToday)
        {
            Rule();
            Extra(L.T("ImsakLabel"), Ramadan.Imsak(today[Prayer.Fajr]), Art.GoldText(dark));
            Extra(L.T("Iftar"), today[Prayer.Maghrib], Art.GoldText(dark));
        }
        if (app.S.ShowNightTimes)
        {
            Rule();
            var night = app.NightAfter(today.Date);
            Extra(L.T("MiddleOfNight"), night.Midnight, t.Dim);
            Extra(L.T("LastThirdOfNight"), night.LastThird, t.Dim);
        }
    }
}
