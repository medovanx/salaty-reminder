using System.Drawing.Drawing2D;
using System.Globalization;

namespace SalatyReminder;

/// <summary>A month grid drawn in the card's style, with the Hijri day under each Gregorian day.</summary>
sealed class CalendarPanel : Control
{
    public event Action<DateOnly>? DatePicked;

    DateOnly month;          // first day of the shown month
    DateOnly selected;
    int hijriAdjust;
    bool dark;
    Rectangle rPrev, rNext, rTitle, rToday;
    readonly Dictionary<Rectangle, DateOnly> cells = [];
    Point hoverPt = new(-1, -1);

    public CalendarPanel()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint
                 | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        TabStop = true;
    }

    float S => DeviceDpi / 96f;
    int Px(float v) => (int)Math.Round(v * S);

    public Size Preferred => new(Px(300), Px(318));

    public void Open(DateOnly date, int hijri, bool isDark)
    {
        selected = date;
        month = new DateOnly(date.Year, date.Month, 1);
        hijriAdjust = hijri;
        dark = isDark;
        Size = Preferred;
        Visible = true;
        BringToFront();
        Focus();
        Invalidate();
    }

    void Shift(int months) { month = month.AddMonths(months); Invalidate(); }

    protected override void OnMouseWheel(MouseEventArgs e) { Shift(e.Delta > 0 ? -1 : 1); }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        hoverPt = e.Location;
        bool hot = rPrev.Contains(e.Location) || rNext.Contains(e.Location) || rToday.Contains(e.Location)
                   || cells.Keys.Any(r => r.Contains(e.Location));
        Cursor = hot ? Cursors.Hand : Cursors.Default;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e) { hoverPt = new(-1, -1); Invalidate(); }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        if (rPrev.Contains(e.Location)) Shift(L.Ar ? 1 : -1);
        else if (rNext.Contains(e.Location)) Shift(L.Ar ? -1 : 1);
        else if (rToday.Contains(e.Location)) DatePicked?.Invoke(DateOnly.FromDateTime(DateTime.Now));
        else foreach (var (r, d) in cells) if (r.Contains(e.Location)) { DatePicked?.Invoke(d); return; }
    }

    protected override bool IsInputKey(Keys k) => k is Keys.Left or Keys.Right or Keys.Up or Keys.Down || base.IsInputKey(k);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        int days = e.KeyCode switch { Keys.Left => L.Ar ? 1 : -1, Keys.Right => L.Ar ? -1 : 1, Keys.Up => -7, Keys.Down => 7, _ => 0 };
        if (days != 0)
        {
            selected = selected.AddDays(days);
            month = new DateOnly(selected.Year, selected.Month, 1);
            Invalidate();
        }
        else if (e.KeyCode == Keys.Enter) DatePicked?.Invoke(selected);
        else if (e.KeyCode == Keys.PageUp) Shift(-1);
        else if (e.KeyCode == Keys.PageDown) Shift(1);
        base.OnKeyDown(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        bool rtl = L.Ar;
        var culture = L.Culture;
        Color bg = dark ? Color.FromArgb(38, 41, 41) : Color.White;
        Color border = dark ? Color.FromArgb(62, 66, 66) : Color.FromArgb(222, 224, 224);
        Color fg = dark ? Color.FromArgb(238, 238, 238) : Color.FromArgb(25, 25, 25);
        Color dim = dark ? Color.FromArgb(120, 126, 126) : Color.FromArgb(150, 154, 154);
        Color accent = dark ? Color.FromArgb(86, 204, 170) : Color.FromArgb(12, 118, 96);

        var outer = new RectangleF(0.5f, 0.5f, Width - 1, Height - 1);
        using (var p = Art.RoundRect(outer, Px(10)))
        {
            using var b = new SolidBrush(bg);
            g.FillPath(b, p);
            using var pen = new Pen(border);
            g.DrawPath(pen, p);
        }

        // Header: ‹ November 2026 ›  +  Hijri months covered
        rPrev = new Rectangle(Px(8), Px(8), Px(32), Px(32));
        rNext = new Rectangle(Width - Px(40), Px(8), Px(32), Px(32));
        rTitle = new Rectangle(Px(40), Px(6), Width - Px(80), Px(22));
        using var fIcon = new Font(Art.IconFontName, 10f);
        using var fTitle = new Font("Segoe UI Semibold", 10.5f);
        using var fSub = new Font("Segoe UI", 8f);
        using var fDay = new Font("Segoe UI", 9.5f);
        using var fDayB = new Font("Segoe UI Semibold", 9.5f);
        using var fHijri = new Font("Segoe UI", 6.75f);
        var center = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding
                     | (rtl ? TextFormatFlags.RightToLeft : 0);

        foreach (var (r, glyph) in new[] { (rPrev, Art.IconPrev), (rNext, Art.IconNext) })
        {
            if (r.Contains(hoverPt)) FillRound(g, r, Px(6), Color.FromArgb(dark ? 30 : 18, fg));
            TextRenderer.DrawText(g, glyph, fIcon, r, fg, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
        var mdt = month.ToDateTime(TimeOnly.MinValue);
        TextRenderer.DrawText(g, mdt.ToString("MMMM yyyy", culture), fTitle, rTitle, fg, center);
        var lastDay = month.AddMonths(1).AddDays(-1).ToDateTime(TimeOnly.MinValue);
        string h1 = HijriMonthYear(mdt), h2 = HijriMonthYear(lastDay);
        // "Jumada al-Ula - Jumada al-Akhirah 1448 AH": the year once when both months share it.
        string y1 = h1.Split(' ')[^2], y2 = h2.Split(' ')[^2];
        string range = h1 == h2 ? h1 : y1 == y2 ? $"{h1[..h1.LastIndexOf(' ', h1.LastIndexOf(' ') - 1)]} - {h2}" : $"{h1} - {h2}";
        TextRenderer.DrawText(g, range, fSub, new Rectangle(Px(6), Px(28), Width - Px(12), Px(16)), accent, center | TextFormatFlags.EndEllipsis);

        // Weekday header (week starts Saturday in Arabic locales, Sunday in English).
        var firstDow = rtl ? DayOfWeek.Saturday : DayOfWeek.Sunday;
        int gridLeft = Px(10), gridTop = Px(50), cw = (Width - Px(20)) / 7, ch = Px(36);
        for (int i = 0; i < 7; i++)
        {
            var dow = (DayOfWeek)(((int)firstDow + i) % 7);
            string name = culture.DateTimeFormat.GetShortestDayName(dow);
            TextRenderer.DrawText(g, name, fSub, CellRect(i, gridLeft, gridTop, cw, Px(18), rtl), dim, center);
        }

        // Day grid
        cells.Clear();
        var today = DateOnly.FromDateTime(DateTime.Now);
        int offset = ((int)month.DayOfWeek - (int)firstDow + 7) % 7;
        var start = month.AddDays(-offset);
        int top = gridTop + Px(20);
        for (int i = 0; i < 42; i++)
        {
            var d = start.AddDays(i);
            var r = CellRect(i % 7, gridLeft, top + (i / 7) * ch, cw, ch, rtl);
            cells[r] = d;
            bool inMonth = d.Month == month.Month;
            bool isSel = d == selected, isToday = d == today;
            var inner = Rectangle.Inflate(r, -Px(2), -Px(1));

            if (isSel) FillRound(g, inner, Px(8), accent);
            else if (r.Contains(hoverPt)) FillRound(g, inner, Px(8), Color.FromArgb(dark ? 28 : 16, fg));
            if (isToday && !isSel)
            {
                using var pen = new Pen(accent, Math.Max(1.2f, 1.5f * S));
                using var p = Art.RoundRect(inner, Px(8));
                g.DrawPath(pen, p);
            }

            Color dc = isSel ? Color.White : inMonth ? fg : dim;
            Color hc = isSel ? Color.FromArgb(220, 255, 255, 255) : inMonth ? accent : dim;
            var dt = d.ToDateTime(TimeOnly.MinValue);
            TextRenderer.DrawText(g, d.Day.ToString(CultureInfo.InvariantCulture), isSel || isToday ? fDayB : fDay,
                new Rectangle(r.X, r.Y + Px(2), r.Width, Px(20)), dc, center);
            TextRenderer.DrawText(g, HijriDay(dt), fHijri, new Rectangle(r.X, r.Y + Px(20), r.Width, Px(13)),
                Color.FromArgb(inMonth || isSel ? 255 : 150, hc), center);
        }

        // "Today" link
        rToday = new Rectangle(Width / 2 - Px(60), Height - Px(26), Px(120), Px(20));
        using var fLink = new Font("Segoe UI Semibold", 9f, rToday.Contains(hoverPt) ? FontStyle.Underline : FontStyle.Regular);
        TextRenderer.DrawText(g, L.T("Today"), fLink, rToday, accent, center);
    }

    static Rectangle CellRect(int col, int left, int top, int cw, int ch, bool rtl) =>
        new(left + (rtl ? 6 - col : col) * cw, top, cw, ch);

    static void FillRound(Graphics g, Rectangle r, int rad, Color c)
    {
        using var p = Art.RoundRect(r, rad);
        using var b = new SolidBrush(c);
        g.FillPath(b, p);
    }

    string HijriDay(DateTime d)
    {
        var cal = new UmAlQuraCalendar();
        var x = d.AddDays(hijriAdjust);
        return x < cal.MinSupportedDateTime || x > cal.MaxSupportedDateTime ? "" : cal.GetDayOfMonth(x).ToString(CultureInfo.InvariantCulture);
    }

    string HijriMonthYear(DateTime d)
    {
        var full = L.Hijri(d, hijriAdjust); // "6 Jumada al-Akhirah 1448 AH"
        int sp = full.IndexOf(' ');
        return sp > 0 ? full[(sp + 1)..] : full;
    }
}
