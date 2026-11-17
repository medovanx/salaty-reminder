using System.Drawing.Drawing2D;
using System.Globalization;

namespace SalatyReminder.UI.Controls;

/// <summary>
/// A month grid drawn in the card's style, with the Hijri day under each Gregorian day.
/// Clicking the title zooms out to a grid of months, then to a grid of years, to jump far quickly.
/// Islamic occasions get a gold mark and sunnah fasting days a teal one; the day under the mouse (or the
/// selected day) is named under the grid.
/// </summary>
sealed class CalendarPanel : Control
{
    public event Action<DateOnly>? DatePicked;

    DateOnly month;          // first day of the shown month
    DateOnly selected;
    int hijriAdjust;
    bool dark, occasions;
    Rectangle rPrev, rNext, rTitle, rToday;
    readonly Dictionary<Rectangle, DateOnly> cells = [];
    readonly Dictionary<Rectangle, int> picks = [];   // month (1-12) or year cells in the zoomed-out views

    enum View { Days, Months, Years }
    View view;
    int yearStart;           // first year on the years page

    const int MinYear = 1901, MaxYear = 2076;   // the span Umm al-Qura dates cover
    Point hoverPt = new(-1, -1);

    public CalendarPanel()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint
                 | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        TabStop = true;
    }

    float S => DeviceDpi / 96f;
    int Px(float v) => (int)Math.Round(v * S);

    public Size Preferred => new(Px(300), Px(occasions ? 338 : 318));

    public void Open(DateOnly date, int hijri, bool isDark, bool showOccasions)
    {
        occasions = showOccasions;
        selected = date;
        month = new DateOnly(date.Year, date.Month, 1);
        hijriAdjust = hijri;
        view = View.Days;
        dark = isDark;
        Size = Preferred;
        Visible = true;
        BringToFront();
        Focus();
        Invalidate();
    }

    /// <summary>The previous/next arrows: a month, a year or a page of years depending on the view.</summary>
    void Shift(int step)
    {
        switch (view)
        {
            case View.Days:
                var m = month.AddMonths(step);
                if (m.Year is >= MinYear and <= MaxYear) month = m;
                break;
            case View.Months:
                month = new DateOnly(Math.Clamp(month.Year + step, MinYear, MaxYear), month.Month, 1);
                break;
            case View.Years:
                yearStart = Math.Clamp(yearStart + step * 12, MinYear, MaxYear - 11);
                break;
        }
        Invalidate();
    }

    void ZoomOut()
    {
        if (view == View.Days) view = View.Months;
        else if (view == View.Months)
        {
            view = View.Years;
            yearStart = Math.Clamp(month.Year - 4, MinYear, MaxYear - 11);
        }
        Invalidate();
    }

    public void ZoomOutForShot() => ZoomOut();

    /// <summary>Esc: step back towards the day grid. Returns false when already there.</summary>
    public bool Back()
    {
        if (view == View.Days) return false;
        view = view == View.Years ? View.Months : View.Days;
        Invalidate();
        return true;
    }

    void Pick(int value)
    {
        if (view == View.Months) { month = new DateOnly(month.Year, value, 1); view = View.Days; }
        else { month = new DateOnly(value, month.Month, 1); view = View.Months; }
        Invalidate();
    }

    protected override void OnMouseWheel(MouseEventArgs e) { Shift(e.Delta > 0 ? -1 : 1); }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        hoverPt = e.Location;
        bool hot = rPrev.Contains(e.Location) || rNext.Contains(e.Location) || rToday.Contains(e.Location)
                   || (view != View.Years && rTitle.Contains(e.Location))
                   || cells.Keys.Any(r => r.Contains(e.Location)) || picks.Keys.Any(r => r.Contains(e.Location));
        Cursor = hot ? Cursors.Hand : Cursors.Default;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e) { hoverPt = new(-1, -1); Invalidate(); }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        if (rPrev.Contains(e.Location)) Shift(L.Ar ? 1 : -1);
        else if (rNext.Contains(e.Location)) Shift(L.Ar ? -1 : 1);
        else if (rToday.Contains(e.Location)) DatePicked?.Invoke(DateOnly.FromDateTime(Clock.Now));
        else if (rTitle.Contains(e.Location)) ZoomOut();
        else if (view == View.Days) { foreach (var (r, d) in cells) if (r.Contains(e.Location)) { DatePicked?.Invoke(d); return; } }
        else foreach (var (r, v) in picks) if (r.Contains(e.Location)) { Pick(v); return; }
    }

    protected override bool IsInputKey(Keys k) => k is Keys.Left or Keys.Right or Keys.Up or Keys.Down || base.IsInputKey(k);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (view != View.Days)
        {
            if (e.KeyCode == Keys.PageUp) Shift(-1);
            else if (e.KeyCode == Keys.PageDown) Shift(1);
            base.OnKeyDown(e);
            return;
        }
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
        cells.Clear();
        picks.Clear();
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
        string title = view switch
        {
            View.Days => mdt.ToString("MMMM yyyy", culture),
            View.Months => month.Year.ToString(CultureInfo.InvariantCulture),
            // Laid out visually: the first year sits on the side the grid starts from.
            _ => rtl ? $"{yearStart + 11} - {yearStart}" : $"{yearStart} - {yearStart + 11}",
        };
        // The title is a button (except at the top level): hover shows it, a chevron hints at it.
        int tw = TextRenderer.MeasureText(g, title, fTitle, Size.Empty, TextFormatFlags.NoPadding).Width;
        bool zoomable = view != View.Years;
        int chevW = zoomable ? Px(16) : 0;
        int titleW = tw + chevW + Px(12);
        rTitle = new Rectangle((Width - titleW) / 2, Px(5), titleW, Px(24));
        if (zoomable && rTitle.Contains(hoverPt)) FillRound(g, rTitle, Px(6), Color.FromArgb(dark ? 30 : 18, fg));
        var rText = new Rectangle(rTitle.X + Px(6) + (rtl ? chevW : 0), rTitle.Y, tw, rTitle.Height);
        TextRenderer.DrawText(g, title, fTitle, rText, fg, view == View.Days ? center : center & ~TextFormatFlags.RightToLeft);
        if (zoomable)
        {
            using var fChev = new Font(Art.IconFontName, 7f);
            var rChev = new Rectangle(rtl ? rTitle.X + Px(4) : rText.Right + Px(2), rTitle.Y + Px(1), chevW - Px(2), rTitle.Height);
            TextRenderer.DrawText(g, "\uE70D", fChev, rChev, dim, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
        if (view != View.Days)
        {
            PaintPicker(g, culture, rtl, fg, dim, accent, center, fSub);
            DrawToday(g, accent, center);
            return;
        }
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
        var today = DateOnly.FromDateTime(Clock.Now);
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

            // A dot in the cell's top corner, kept for the days worth spotting at a glance: the major occasions
            // in gold, the white days and Tasu'a/Ashura fasts in teal. Weekly fasts and the ten-day seasons would
            // dot half the month, so they are only named (under the grid) when the day is hovered or selected.
            if (occasions && Occasions.For(d, hijriAdjust) is var info && (info.IsMajor || info.Fast is FastKind.WhiteDays or FastKind.Ashura))
            {
                Color dot = isSel ? Color.White : info.IsMajor ? Art.GoldText(dark) : accent;
                if (!inMonth) dot = Color.FromArgb(110, dot);
                float ds = Math.Max(4f, 5f * S);
                float dx = rtl ? inner.Left + Px(5) : inner.Right - Px(5) - ds;
                using var db = new SolidBrush(dot);
                g.FillEllipse(db, dx, inner.Top + Px(4), ds, ds);
            }
        }

        // Name of the day under the mouse, or of the selected day.
        if (occasions)
        {
            DateOnly? shown = null;
            foreach (var (r, d) in cells) if (r.Contains(hoverPt)) shown = d;
            shown ??= selected;
            var info = Occasions.For(shown.Value, hijriAdjust);
            string label = L.DayLabel(info, shown.Value.DayOfWeek);
            if (label.Length > 0)
            {
                string when = shown.Value.ToDateTime(TimeOnly.MinValue).ToString("d MMMM", culture);
                TextRenderer.DrawText(g, $"{when}, {label}", fSub, new Rectangle(Px(8), top + 6 * ch + Px(2), Width - Px(16), Px(18)),
                    info.Occasion != null ? Art.GoldText(dark) : accent, center | TextFormatFlags.EndEllipsis);
            }
        }

        DrawToday(g, accent, center);
    }

    void DrawToday(Graphics g, Color accent, TextFormatFlags center)
    {
        rToday = new Rectangle(Width / 2 - Px(60), Height - Px(26), Px(120), Px(20));
        using var fLink = new Font("Segoe UI Semibold", 9f, rToday.Contains(hoverPt) ? FontStyle.Underline : FontStyle.Regular);
        TextRenderer.DrawText(g, L.T("Today"), fLink, rToday, accent, center);
    }

    /// <summary>A 3 x 4 grid of months (of the shown year) or of years.</summary>
    void PaintPicker(Graphics g, CultureInfo culture, bool rtl, Color fg, Color dim, Color accent, TextFormatFlags center, Font fSub)
    {
        // Hijri years the page spans, under the title, like the day view's Hijri months.
        int gy1 = view == View.Months ? month.Year : yearStart, gy2 = view == View.Months ? month.Year : yearStart + 11;
        var cal = new UmAlQuraCalendar();
        DateTime a = new DateTime(gy1, 1, 1).AddDays(hijriAdjust), b = new DateTime(gy2, 12, 31).AddDays(hijriAdjust);
        if (a >= cal.MinSupportedDateTime && b <= cal.MaxSupportedDateTime)
        {
            int h1 = cal.GetYear(a), h2 = cal.GetYear(b);
            string sub = rtl ? (h1 == h2 ? $"هـ {h1}" : $"هـ {h2} - {h1}") : h1 == h2 ? $"{h1} AH" : $"{h1} - {h2} AH";
            TextRenderer.DrawText(g, sub, fSub, new Rectangle(Px(6), Px(28), Width - Px(12), Px(16)), accent, center & ~TextFormatFlags.RightToLeft);
        }

        using var fItem = new Font("Segoe UI", 10f);
        using var fItemB = new Font("Segoe UI Semibold", 10f);
        var now = Clock.Now;
        int left = Px(10), top = Px(52), cols = 3, rows = 4;
        int cw = (Width - Px(20)) / cols, ch = (Height - Px(34) - top) / rows;
        for (int i = 0; i < 12; i++)
        {
            int col = i % cols, row = i / cols;
            var r = new Rectangle(left + (rtl ? cols - 1 - col : col) * cw, top + row * ch, cw, ch);
            int value = view == View.Months ? i + 1 : yearStart + i;
            if (value > MaxYear && view == View.Years) continue;
            picks[r] = value;
            bool isSel = view == View.Months ? value == month.Month : value == month.Year;
            bool isNow = view == View.Months ? month.Year == now.Year && value == now.Month : value == now.Year;
            var inner = Rectangle.Inflate(r, -Px(3), -Px(3));
            if (isSel) FillRound(g, inner, Px(8), accent);
            else if (r.Contains(hoverPt)) FillRound(g, inner, Px(8), Color.FromArgb(dark ? 28 : 16, fg));
            if (isNow && !isSel)
            {
                using var pen = new Pen(accent, Math.Max(1.2f, 1.5f * S));
                using var p = Art.RoundRect(inner, Px(8));
                g.DrawPath(pen, p);
            }
            string text = view == View.Months
                ? (rtl ? culture.DateTimeFormat.GetMonthName(value) : culture.DateTimeFormat.GetAbbreviatedMonthName(value))
                : value.ToString(CultureInfo.InvariantCulture);
            TextRenderer.DrawText(g, text, isSel || isNow ? fItemB : fItem, inner, isSel ? Color.White : fg, center);
        }
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
