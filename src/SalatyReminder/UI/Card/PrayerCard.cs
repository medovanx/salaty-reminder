using System.Drawing.Drawing2D;

namespace SalatyReminder.UI.Card;

/// <summary>The popup above the taskbar: a patterned header with the next prayer, dates and all six times.</summary>
sealed class PrayerCard : Form
{
    const int W = 340, HeaderH = 192;
    float RowH => app.S.ShowSunnah ? 46 : 38;
    float NightH => app.S.ShowNightTimes ? 22 : 0;
    int H => (int)(258 + RowH * 6 + NightH + 92);

    readonly SalatyApp app;
    readonly System.Windows.Forms.Timer tick = new() { Interval = 1000 };
    readonly IconButton btnSettings = new(), btnRefresh = new();
    /// <summary>Explains the Islamic midnight when the mouse rests on the "i" beside it.</summary>
    readonly ToolTip tip = new() { OwnerDraw = true, InitialDelay = 250, ReshowDelay = 100 };
    Rectangle rInfo;
    bool tipShown;
    bool dark;
    public DateTime LastHidden { get; private set; }

    public PrayerCard(SalatyApp app)
    {
        this.app = app;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        DoubleBuffered = true;
        KeyPreview = true;
        Text = L.T("AppName");
        Icon = app.AppIcon;

        btnSettings.Glyph = Art.IconSettings;
        btnRefresh.Glyph = Art.IconLocation;
        Controls.Add(btnSettings);
        Controls.Add(btnRefresh);
        btnSettings.Click += (_, _) => { Hide(); app.ShowSettings(); };
        btnRefresh.Click += async (_, _) =>
        {
            btnRefresh.Text = L.T("Locating");
            await app.RefreshLocationAsync(userInitiated: true);
            btnRefresh.Text = L.T("RefreshLocation");
            Invalidate();
        };
        tick.Tick += (_, _) => Invalidate();
        // The tooltip in the card's theme instead of the stock light one.
        tip.Popup += (_, e) =>
        {
            using var f = new Font("Segoe UI", 9f);
            var sz = TextRenderer.MeasureText(L.T("MidnightInfo"), f, new Size(Px(250), 0), TextFormatFlags.WordBreak);
            e.ToolTipSize = new Size(sz.Width + Px(20), sz.Height + Px(14));
        };
        tip.Draw += (_, e) =>
        {
            var th = new Theme(dark);
            using (var b = new SolidBrush(th.Surface)) e.Graphics.FillRectangle(b, e.Bounds);
            using (var p = new Pen(th.Border)) e.Graphics.DrawRectangle(p, 0, 0, e.Bounds.Width - 1, e.Bounds.Height - 1);
            using var f = new Font("Segoe UI", 9f);
            TextRenderer.DrawText(e.Graphics, e.ToolTipText, f, Rectangle.Inflate(e.Bounds, -Px(10), -Px(7)), th.Text,
                TextFormatFlags.WordBreak | TextFormatFlags.VerticalCenter | (L.Ar ? TextFormatFlags.RightToLeft | TextFormatFlags.Right : 0));
        };
        WireNavigation();
        KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape && !(cal.Visible && cal.Back())) Hide(); };
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ClassStyle |= 0x20000;   // CS_DROPSHADOW
            cp.ExStyle |= 0x80;         // WS_EX_TOOLWINDOW
            return cp;
        }
    }

    float S => DeviceDpi / 96f;
    int Px(float v) => (int)Math.Round(v * S);

    public void ShowNear(Rectangle anchor)
    {
        dark = Native.SystemDark();
        RightToLeft = L.Ar ? RightToLeft.Yes : RightToLeft.No;
        BackColor = dark ? Color.FromArgb(30, 32, 32) : Color.FromArgb(250, 250, 249);
        var fg = dark ? Color.White : Color.FromArgb(25, 25, 25);
        var theme = new Theme(dark);
        foreach (var b in new[] { btnSettings, btnRefresh }) { b.Theme = theme; b.RightToLeft = RightToLeft; b.Invalidate(); }
        btnSettings.Text = L.T("Settings");
        btnRefresh.Text = L.T("RefreshLocation");

        var wa = Screen.FromRectangle(anchor).WorkingArea;
        Size = new Size(Px(W), Px(H));
        int x = anchor.Left + anchor.Width / 2 - Width / 2;
        x = Math.Clamp(x, wa.Left + Px(8), wa.Right - Width - Px(8));
        Location = new Point(x, wa.Bottom - Height - Px(10));

        int pad = Px(16), bw = (Width - pad * 2 - Px(8)) / 2, bh = Px(34), by = Height - pad - bh;
        btnRefresh.Bounds = new Rectangle(pad, by, bw, bh);
        btnSettings.Bounds = new Rectangle(pad + bw + Px(8), by, bw, bh);
        if (L.Ar) (btnRefresh.Left, btnSettings.Left) = (btnSettings.Left, btnRefresh.Left);

        viewDate = null;
        cal.Visible = false;
        activated = false;
        Show();
        Native.RoundCorners(Handle, dark);
        Activate();
        Native.SetForegroundWindow(Handle);
        tick.Start();
    }

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);
        activated = true;
    }

    protected override void OnDeactivate(EventArgs e)
    {
        base.OnDeactivate(e);
        // Opened without focus (e.g. from a second launch): stay until the user clicks elsewhere after focusing it.
        if (activated) Hide();
    }

    // ---- Day navigation ----
    DateOnly? viewDate;               // null = today
    Rectangle rPrev, rNext, rDate, rToday;
    bool activated;
    readonly CalendarPanel cal = new() { Visible = false };

    DateOnly Today => DateOnly.FromDateTime(Clock.Now);
    DateOnly Viewing => viewDate ?? Today;

    void Go(int days) => SetView(Viewing.AddDays(days));

    void SetView(DateOnly d)
    {
        viewDate = d == Today ? null : d;
        cal.Visible = false;
        Invalidate();
    }

    void ToggleCalendar()
    {
        if (cal.Visible) { cal.Visible = false; return; }
        cal.RightToLeft = L.Ar ? RightToLeft.Yes : RightToLeft.No;
        var pref = cal.Preferred;
        cal.Location = new Point((Width - pref.Width) / 2, rDate.Bottom + Px(4));
        cal.Open(Viewing, app.S.HijriAdjust, dark, app.S.ShowOccasions);
    }

    public void OpenCalendarForShot() { Refresh(); ToggleCalendar(); }
    public void ZoomCalendarForShot() => cal.ZoomOutForShot();

    void WireNavigation()
    {
        Controls.Add(cal);
        cal.DatePicked += SetView;
        MouseClick += (_, e) =>
        {
            if (e.Button != MouseButtons.Left) return;
            if (rPrev.Contains(e.Location)) Go(-1);
            else if (rNext.Contains(e.Location)) Go(1);
            else if (rDate.Contains(e.Location)) ToggleCalendar();
            else if (viewDate != null && rToday.Contains(e.Location)) SetView(Today);
            else if (cal.Visible) cal.Visible = false;
        };
        MouseMove += (_, e) =>
        {
            bool overInfo = rInfo.Contains(e.Location);
            if (overInfo != tipShown)
            {
                if (overInfo) tip.Show(L.T("MidnightInfo"), this, rInfo.Left, rInfo.Bottom + Px(4));
                else tip.Hide(this);
                tipShown = overInfo;
                Invalidate(rInfo); // the "i" turns teal while it is pointed at
            }
            bool hot = rPrev.Contains(e.Location) || rNext.Contains(e.Location) || rDate.Contains(e.Location)
                       || (viewDate != null && rToday.Contains(e.Location));
            Cursor = overInfo ? Cursors.Help : hot ? Cursors.Hand : Cursors.Default;
        };
        MouseWheel += (_, e) => { if (!cal.Visible) Go(e.Delta > 0 ? -1 : 1); };
        KeyDown += (_, e) =>
        {
            int dir = e.KeyCode switch { Keys.Left => -1, Keys.Right => 1, _ => 0 };
            if (dir != 0 && !cal.Focused) { Go(L.Ar ? -dir : dir); e.Handled = true; }
            if (e.KeyCode == Keys.Home) SetView(Today);
        };
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if (!Visible) { tick.Stop(); LastHidden = Clock.Now; if (tipShown) { tip.Hide(this); tipShown = false; } }
    }

    /// <summary>Mirrors a left-to-right layout box for Arabic.</summary>
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
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var now = Clock.Now;
        var st = app.S;
        var today = app.Today;
        if (today == null) return;

        Color fg = dark ? Color.FromArgb(240, 240, 240) : Color.FromArgb(25, 25, 25);
        Color dim = dark ? Color.FromArgb(160, 166, 166) : Color.FromArgb(105, 110, 110);
        Color faint = dark ? Color.FromArgb(105, 110, 110) : Color.FromArgb(165, 168, 168);
        Color accent = dark ? Color.FromArgb(86, 204, 170) : Color.FromArgb(12, 118, 96);

        using var fTiny = new Font("Segoe UI", 8.5f);
        using var fSmall = new Font("Segoe UI", 9f);
        using var fSmallB = new Font("Segoe UI Semibold", 9.5f);
        using var fBig = new Font("Segoe UI Semibold", 22f);
        using var fTime = new Font("Segoe UI Light", 18f);
        using var fCount = new Font("Segoe UI Semibold", 11f);
        using var fRow = new Font("Segoe UI", 10.5f);
        using var fRowB = new Font("Segoe UI Semibold", 10.5f);

        // ---- Header: teal gradient + star pattern ----
        var header = new Rectangle(0, 0, Width, Px(HeaderH));
        using (var grad = new LinearGradientBrush(header, Art.TealLight, Art.TealDark, LinearGradientMode.ForwardDiagonal))
            g.FillRectangle(grad, header);
        Art.Pattern(g, header, Px(34), Color.FromArgb(22, 255, 255, 255));
        using (var shade = new LinearGradientBrush(header, Color.FromArgb(0, 0, 0, 0), Color.FromArgb(70, 0, 0, 0), LinearGradientMode.Vertical))
            g.FillRectangle(shade, header);

        var white = Color.White;
        var soft = Color.FromArgb(215, 235, 230);
        var glyphBox = Box(16, 14, 18, 18);
        Art.Crescent(g, glyphBox, Art.Gold);
        Draw(g, L.T("AppName"), fSmallB, white, Box(40, 12, 110, 22));
        Draw(g, st.LocationName, fTiny, soft, Box(150, 12, W - 166, 22), far: true);

        var (next, at) = app.GetNext(now);
        var (prev, prevAt) = app.GetPrevious(now);
        string headLabel = L.T("NextPrayer"), headName = L.PrayerName(next, at), headAt = L.Time(at, st.Use24h);
        string fromLabel = $"{L.PrayerName(prev, prevAt)} {L.Time(prevAt, st.Use24h)}", toLabel = $"{L.PrayerName(next, at)} {L.Time(at, st.Use24h)}";
        DateTime from = prevAt, to = at;
        // Ramadan: the header counts down to Iftar by day (the bar is the fast, Fajr to Maghrib) and to the end of Suhoor by night.
        if (app.RamadanTarget(now) is { } r)
        {
            bool iftar = r.at == today[Prayer.Maghrib];
            int rd = Ramadan.Day(DateOnly.FromDateTime(r.at), st.HijriAdjust);
            headLabel = L.F("RamadanDay", rd);
            headName = r.label;
            headAt = L.Time(r.at, st.Use24h);
            to = r.at;
            from = iftar ? today[Prayer.Fajr] : now < today[Prayer.Fajr] ? today[Prayer.Isha].AddDays(-1) : today[Prayer.Isha];
            var fromP = iftar ? Prayer.Fajr : Prayer.Isha;
            var toP = iftar ? Prayer.Maghrib : Prayer.Fajr;
            fromLabel = $"{L.PrayerName(fromP, from)} {L.Time(from, st.Use24h)}";
            toLabel = $"{L.PrayerName(toP, to)} {L.Time(to, st.Use24h)}";
        }
        Draw(g, headLabel, fSmall, soft, Box(16, 46, 200, 20));
        Draw(g, headName, fBig, white, Box(14, 64, 190, 44));
        Draw(g, headAt, fTime, white, Box(170, 68, W - 186, 40), far: true);
        Draw(g, L.Countdown(to - now), fCount, Art.Gold, Box(16, 110, W - 32, 22));

        // Progress from the previous prayer to the next one (or across the fast in Ramadan).
        double frac = Math.Clamp((now - from).TotalSeconds / Math.Max(1, (to - from).TotalSeconds), 0, 1);
        var bar = new RectangleF(Px(16), Px(142), Width - Px(32), Px(6));
        using (var track = Art.RoundRect(bar, Px(3)))
        using (var tb = new SolidBrush(Color.FromArgb(55, 255, 255, 255)))
            g.FillPath(tb, track);
        float fillW = Math.Max(Px(6), (float)(bar.Width * frac));
        var fill = new RectangleF(L.Ar ? bar.Right - fillW : bar.Left, bar.Top, fillW, bar.Height);
        using (var fp = Art.RoundRect(fill, Px(3)))
        using (var fb = new SolidBrush(Art.Gold))
            g.FillPath(fb, fp);
        Draw(g, fromLabel, fTiny, soft, Box(16, 154, 150, 20));
        Draw(g, toLabel, fTiny, soft, Box(W - 166, 154, 150, 20), far: true);

        // ---- Date navigator: ‹ date (click for calendar) › ----
        var day = Viewing;
        bool isToday = viewDate == null;
        var dayTimes = isToday ? today : app.TimesFor(day);
        var dayDt = day.ToDateTime(new TimeOnly(12, 0));
        using var fArrow = new Font(Art.IconFontName, 11f);
        rPrev = Box(10, 198, 32, 44);
        rNext = Box(W - 42, 198, 32, 44);
        rDate = Box(46, 198, W - 92, 44);
        // Icon-font glyphs aren't mirrored by RTL text, so pick them by visual direction.
        TextRenderer.DrawText(g, L.Ar ? Art.IconNext : Art.IconPrev, fArrow, rPrev, dim, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        TextRenderer.DrawText(g, L.Ar ? Art.IconPrev : Art.IconNext, fArrow, rNext, dim, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        var top = new Rectangle(rDate.X, rDate.Y, rDate.Width, rDate.Height / 2);
        var bottom = new Rectangle(rDate.X, rDate.Y + rDate.Height / 2, rDate.Width, rDate.Height / 2);
        Draw(g, L.Gregorian(dayDt), isToday ? fSmall : fSmallB, fg, top, center: true);
        // The viewed day's occasion or sunnah fast, beside its Hijri date.
        string dayLabel = st.ShowOccasions ? L.DayLabel(Occasions.For(day, st.HijriAdjust), day.DayOfWeek) : "";
        Draw(g, dayLabel.Length > 0 ? $"{L.HijriShort(dayDt, st.HijriAdjust)}, {dayLabel}" : L.Hijri(dayDt, st.HijriAdjust), fSmall, accent, bottom, center: true);

        if (isToday)
        {
            rToday = Rectangle.Empty;
            using var pen = new Pen(dark ? Color.FromArgb(52, 56, 56) : Color.FromArgb(228, 230, 230));
            g.DrawLine(pen, Px(16), Px(250), Width - Px(16), Px(250));
        }
        else
        {
            using var fLink = new Font("Segoe UI", 8.5f, FontStyle.Underline);
            rToday = Box(W / 2 - 70, 242, 140, 16);
            Draw(g, L.T("BackToToday"), fLink, accent, rToday, center: true);
        }

        // ---- Six times for the viewed day ----
        bool nextIsTomorrow = at.Date > now.Date;
        bool ramadanDay = st.RamadanMode && Ramadan.IsRamadan(day, st.HijriAdjust);
        float y = 258;
        foreach (var p in Enum.GetValues<Prayer>())
        {
            var t = dayTimes[p];
            bool isNext = isToday && !nextIsTomorrow && p == next;
            bool past = isToday && t <= now && !isNext;
            float rh = RowH;
            if (isNext)
            {
                var hr = new RectangleF(Px(10), Px(y + 1), Width - Px(20), Px(rh - 2));
                using var path = Art.RoundRect(hr, Px(8));
                using var hb = new SolidBrush(Color.FromArgb(dark ? 40 : 26, accent));
                g.FillPath(hb, path);
                var stripe = Box(10, y + rh / 2 - 10, 3, 20);
                using var sb = new SolidBrush(accent);
                g.FillRectangle(sb, stripe);
            }
            var c = isNext ? accent : past ? faint : p == Prayer.Sunrise ? dim : fg;
            var f = isNext ? fRowB : fRow;
            // Each prayer in full colour; passed ones fade, with the horizon in the row's quiet tone.
            Art.PrayerGlyph(g, p, Box(24, y + rh / 2 - 10, 20, 20), dark, past, past ? faint : dim);
            // In Ramadan, Fajr's line gives Imsak and Maghrib's says it is Iftar.
            string sub = ramadanDay && p == Prayer.Fajr ? L.F("Imsak", L.Time(Ramadan.Imsak(t), st.Use24h))
                       : ramadanDay && p == Prayer.Maghrib ? L.T("Iftar")
                       : L.Sunnah(p);
            if (st.ShowSunnah && sub.Length > 0)
            {
                Draw(g, L.PrayerName(p, t), f, c, Box(56, y + 5, 170, 20));
                Draw(g, sub, fTiny, past ? faint : ramadanDay && p is Prayer.Fajr or Prayer.Maghrib ? Art.GoldText(dark) : dim, Box(56, y + 24, 200, 16));
            }
            else Draw(g, L.PrayerName(p, t), f, c, Box(56, y, 160, rh));
            Draw(g, L.Time(t, st.Use24h), f, c, Box(220, y, W - 244, rh), far: true);
            y += rh;
        }

        // The night after the viewed day: its middle (with an "i" that explains it is not 12:00) and the start of its last third.
        rInfo = Rectangle.Empty;
        if (st.ShowNightTimes)
        {
            var night = app.NightAfter(day);
            string a = L.F("Midnight", L.Time(night.Midnight, st.Use24h)), b = L.F("LastThird", L.Time(night.LastThird, st.Use24h));
            var mf = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;
            int wa = TextRenderer.MeasureText(g, a, fTiny, Size.Empty, mf).Width, wb = TextRenderer.MeasureText(g, b, fTiny, Size.Empty, mf).Width;
            int icon = Px(13), gap = Px(4), sep = Px(18), lineY = Px(y + 2), lineH = Px(20);
            int total = wa + gap + icon + sep + wb, x0 = (Width - total) / 2;
            // Left to right: "Midnight 10:55 PM (i)   Last third 12:54 AM"; Arabic mirrors the order.
            int ax = L.Ar ? x0 + total - wa : x0;
            int ix = L.Ar ? ax - gap - icon : ax + wa + gap;
            int bx = L.Ar ? x0 : ix + icon + sep;
            var tf = mf | TextFormatFlags.VerticalCenter | (L.Ar ? TextFormatFlags.RightToLeft : 0);
            TextRenderer.DrawText(g, a, fTiny, new Rectangle(ax, lineY, wa + Px(2), lineH), dim, tf);
            TextRenderer.DrawText(g, b, fTiny, new Rectangle(bx, lineY, wb + Px(2), lineH), dim, tf);
            rInfo = new Rectangle(ix - Px(2), lineY, icon + Px(4), lineH);
            // Centred on the digits' ink, a little taller than them, not on the text box (whose middle is lower).
            var (inkTop, inkBottom) = Art.TextInk(fTiny, lineH);
            // One pixel taller than the digits on each side, so its middle row is exactly theirs.
            float inkH = inkBottom - inkTop;
            Art.GlyphFit(g, "", new RectangleF(ix, lineY + inkTop - 1, icon, inkH + 2), tipShown ? accent : dim);
            y += NightH;
        }

        Draw(g, L.MethodName(Methods.Get(st.Method)) + (st.HanafiAsr ? ", " + L.T("AsrHanafi") : ""), fTiny, faint, Box(18, y + 6, W - 36, 20), center: true);
    }
}
