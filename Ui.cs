using System.Drawing.Drawing2D;

namespace SalatyReminder;

/// <summary>Colours shared by the card, calendar and settings window.</summary>
sealed record Theme(bool Dark)
{
    public static Theme Current => new(Native.SystemDark());
    public Color Window => Dark ? Color.FromArgb(30, 32, 32) : Color.FromArgb(243, 245, 244);
    public Color Surface => Dark ? Color.FromArgb(40, 43, 43) : Color.White;
    public Color Input => Dark ? Color.FromArgb(52, 56, 56) : Color.FromArgb(246, 247, 247);
    public Color Border => Dark ? Color.FromArgb(62, 66, 66) : Color.FromArgb(222, 225, 224);
    public Color Text => Dark ? Color.FromArgb(238, 238, 238) : Color.FromArgb(25, 25, 25);
    public Color Dim => Dark ? Color.FromArgb(160, 166, 166) : Color.FromArgb(105, 110, 110);
    public Color Accent => Dark ? Color.FromArgb(86, 204, 170) : Color.FromArgb(12, 118, 96);
    public Color AccentFill => Color.FromArgb(18, 132, 108);
}

/// <summary>On/off switch in the Windows 11 style, teal when on.</summary>
sealed class ToggleSwitch : Control
{
    bool on, hover;
    public Theme Theme { get; set; } = Theme.Current;
    public event EventHandler? CheckedChanged;

    public ToggleSwitch()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint
                 | ControlStyles.SupportsTransparentBackColor | ControlStyles.Selectable, true);
        Size = new Size(44, 22);
        Cursor = Cursors.Hand;
        TabStop = true;
    }

    public bool Checked
    {
        get => on;
        set { if (on == value) return; on = value; Invalidate(); CheckedChanged?.Invoke(this, EventArgs.Empty); }
    }

    protected override void OnClick(EventArgs e) { Focus(); Checked = !Checked; base.OnClick(e); }
    protected override void OnKeyDown(KeyEventArgs e) { if (e.KeyCode == Keys.Space) Checked = !Checked; base.OnKeyDown(e); }
    protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); }
    protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Styler.Backdrop(this, Theme.Surface));
        float s = DeviceDpi / 96f;
        var track = new RectangleF(1, (Height - 20 * s) / 2, Width - 3, 20 * s);
        int alpha = Enabled ? 255 : 110;
        using (var p = Art.RoundRect(track, track.Height / 2))
        {
            if (on)
            {
                using var b = new SolidBrush(Color.FromArgb(alpha, hover ? Theme.Accent : Theme.AccentFill));
                g.FillPath(b, p);
            }
            else
            {
                using var pen = new Pen(Color.FromArgb(alpha, Theme.Dim), 1.2f * s);
                g.DrawPath(pen, p);
            }
        }
        float k = on ? 14 * s : 10 * s;
        bool right = on ^ (RightToLeft == RightToLeft.Yes);
        float cx = right ? track.Right - track.Height / 2 : track.Left + track.Height / 2;
        float cy = track.Top + track.Height / 2;
        using var kb = new SolidBrush(Color.FromArgb(alpha, on ? Color.White : Theme.Dim));
        g.FillEllipse(kb, cx - k / 2, cy - k / 2, k, k);
        if (Focused && ShowFocusCues)
        {
            using var fp = new Pen(Theme.Accent, 1) { DashStyle = DashStyle.Dot };
            using var p = Art.RoundRect(RectangleF.Inflate(track, 0, 0), track.Height / 2);
            g.DrawPath(fp, p);
        }
    }
}

/// <summary>Two or more pill options side by side; one is selected.</summary>
sealed class Segmented : Control
{
    readonly string[] options;
    int selected, hover = -1;
    public Theme Theme { get; set; } = Theme.Current;
    public event EventHandler? SelectedChanged;

    public Segmented(params string[] options)
    {
        this.options = options;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        Height = 34;
        Cursor = Cursors.Hand;
    }

    public int Selected
    {
        get => selected;
        set { if (selected == value) return; selected = value; Invalidate(); SelectedChanged?.Invoke(this, EventArgs.Empty); }
    }

    Rectangle Seg(int i)
    {
        int w = (Width - 6) / options.Length;
        int idx = RightToLeft == RightToLeft.Yes ? options.Length - 1 - i : i;
        return new Rectangle(3 + idx * w, 3, w, Height - 6);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        int h = Enumerable.Range(0, options.Length).FirstOrDefault(i => Seg(i).Contains(e.Location), -1);
        if (h != hover) { hover = h; Invalidate(); }
    }

    protected override void OnMouseLeave(EventArgs e) { hover = -1; Invalidate(); }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        for (int i = 0; i < options.Length; i++) if (Seg(i).Contains(e.Location)) Selected = i;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Styler.Backdrop(this, Theme.Surface));
        float s = DeviceDpi / 96f;
        using (var p = Art.RoundRect(new RectangleF(0.5f, 0.5f, Width - 1, Height - 1), 8 * s))
        using (var b = new SolidBrush(Theme.Input))
        using (var pen = new Pen(Theme.Border))
        {
            g.FillPath(b, p);
            g.DrawPath(pen, p);
        }
        for (int i = 0; i < options.Length; i++)
        {
            var r = Seg(i);
            if (i == selected || i == hover)
            {
                using var p = Art.RoundRect(r, 6 * s);
                using var b = new SolidBrush(i == selected ? Theme.AccentFill : Color.FromArgb(Theme.Dark ? 30 : 18, Theme.Text));
                g.FillPath(b, p);
            }
            var flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis
                        | (RightToLeft == RightToLeft.Yes ? TextFormatFlags.RightToLeft : 0);
            using var f = new Font(Font, i == selected ? FontStyle.Bold : FontStyle.Regular);
            TextRenderer.DrawText(g, options[i], f, r, i == selected ? Color.White : Theme.Text, flags);
        }
    }
}

/// <summary>A rounded "card" with a title, holding a settings group.</summary>
sealed class Section : Panel
{
    public Theme Theme { get; set; } = Theme.Current;
    public string Glyph { get; set; } = "";

    public Section(string title)
    {
        Text = title;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        Padding = new Padding(16, 44, 16, 12);
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Styler.Backdrop(this, Theme.Window));
        float s = DeviceDpi / 96f;
        using (var p = Art.RoundRect(new RectangleF(0.5f, 0.5f, Width - 1, Height - 1), 10 * s))
        using (var b = new SolidBrush(Theme.Surface))
        using (var pen = new Pen(Theme.Border))
        {
            g.FillPath(b, p);
            g.DrawPath(pen, p);
        }
        bool rtl = RightToLeft == RightToLeft.Yes;
        int x = (int)(16 * s), y = (int)(12 * s), gs = (int)(20 * s);
        var glyphRect = new Rectangle(rtl ? Width - x - gs : x, y, gs, gs);
        if (Glyph.Length > 0)
        {
            using var gf = new Font(Art.IconFontName, 11f);
            TextRenderer.DrawText(g, Glyph, gf, glyphRect, Theme.Accent, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
        using var tf = new Font("Segoe UI Semibold", 10.5f);
        int tx = x + gs + (int)(8 * s);
        var tr = rtl ? new Rectangle(x, y, Width - tx - x, gs) : new Rectangle(tx, y, Width - tx - x, gs);
        TextRenderer.DrawText(g, Text, tf, tr, Theme.Text, TextFormatFlags.VerticalCenter
            | (rtl ? TextFormatFlags.Right | TextFormatFlags.RightToLeft : TextFormatFlags.Left));
    }
}

/// <summary>Flat button with an icon-font glyph and text centred together as one group.</summary>
sealed class IconButton : Control
{
    bool hover, down;
    public string Glyph { get; set; } = "";
    public Theme Theme { get; set; } = Theme.Current;
    public bool Primary { get; set; }

    public IconButton()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        Cursor = Cursors.Hand;
        Font = new Font("Segoe UI", 9f);
    }

    protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { hover = down = false; Invalidate(); }
    protected override void OnMouseDown(MouseEventArgs e) { down = true; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { down = false; Invalidate(); base.OnMouseUp(e); }
    protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); Invalidate(); }
    protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Styler.Backdrop(this, Theme.Window));
        float s = DeviceDpi / 96f;
        Color bg = Primary ? (hover ? Color.FromArgb(24, 150, 122) : Theme.AccentFill)
                 : Theme.Dark ? (down ? Color.FromArgb(40, 43, 43) : hover ? Color.FromArgb(58, 62, 62) : Color.FromArgb(46, 49, 49))
                 : (down ? Color.FromArgb(232, 235, 234) : hover ? Color.FromArgb(242, 245, 244) : Color.White);
        Color fg = Primary ? Color.White : Theme.Text;
        if (!Enabled) fg = Color.FromArgb(120, fg);
        using (var p = Art.RoundRect(new RectangleF(0.5f, 0.5f, Width - 1, Height - 1), 6 * s))
        {
            using var b = new SolidBrush(bg);
            g.FillPath(b, p);
            if (!Primary) { using var pen = new Pen(Theme.Border); g.DrawPath(pen, p); }
        }

        bool rtl = RightToLeft == RightToLeft.Yes;
        using var fIcon = new Font(Art.IconFontName, Font.SizeInPoints + 1.5f);
        var flags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | (rtl ? TextFormatFlags.RightToLeft : 0);
        Size it = Glyph.Length > 0 ? TextRenderer.MeasureText(g, Glyph, fIcon, Size.Empty, TextFormatFlags.NoPadding) : Size.Empty;
        Size tt = TextRenderer.MeasureText(g, Text, Font, Size.Empty, flags);
        int gap = Glyph.Length > 0 ? (int)(8 * s) : 0;
        int total = it.Width + gap + tt.Width;
        int x = (Width - total) / 2;
        int iconX = rtl ? x + tt.Width + gap : x;
        int textX = rtl ? x : x + it.Width + gap;
        if (Glyph.Length > 0)
            TextRenderer.DrawText(g, Glyph, fIcon, new Rectangle(iconX, 0, it.Width, Height), fg,
                TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
        TextRenderer.DrawText(g, Text, Font, new Rectangle(textX, 0, tt.Width + 2, Height), fg, flags | TextFormatFlags.VerticalCenter);
    }
}

static class Styler
{
    /// <summary>Applies the theme to standard controls (inputs, labels, buttons) inside a container.</summary>
    public static void Apply(Control root, Theme t)
    {
        foreach (Control c in root.Controls)
        {
            switch (c)
            {
                case ComboBox cb:
                    ThemeCombo(cb, t); break;
                case TextBox tb:
                    tb.BorderStyle = BorderStyle.FixedSingle; tb.BackColor = t.Input; tb.ForeColor = t.Text; break;
                case NumericUpDown nu:
                    nu.BorderStyle = BorderStyle.FixedSingle; nu.BackColor = t.Input; nu.ForeColor = t.Text; break;
                case Button b:
                    b.FlatStyle = FlatStyle.Flat;
                    b.FlatAppearance.BorderColor = t.Border;
                    b.BackColor = t.Input; b.ForeColor = t.Text;
                    b.FlatAppearance.MouseOverBackColor = t.Dark ? Color.FromArgb(64, 68, 68) : Color.FromArgb(236, 240, 239);
                    b.Cursor = Cursors.Hand;
                    break;
                case Label l:
                    l.ForeColor = l.Tag as string == "dim" ? t.Dim : t.Text; l.BackColor = Color.Transparent; break;
                case ToggleSwitch ts: ts.Theme = t; break;
                case ThemedCombo tc: tc.Theme = t; break;
                case ThemedNumber tn: tn.Theme = t; break;
                case ThemedText tt: tt.Theme = t; break;
                case Segmented sg: sg.Theme = t; break;
                case Section se: se.Theme = t; break;
            }
            if (c is Panel or TableLayoutPanel or FlowLayoutPanel && c is not Section) c.BackColor = Color.Transparent;
            if (c is not (ThemedText or ThemedNumber or ThemedCombo)) Apply(c, t); // they style their own insides
        }
    }

    /// <summary>First real (non-transparent) background colour up the parent chain.</summary>
    public static Color Backdrop(Control c, Color fallback)
    {
        for (var p = c.Parent; p != null; p = p.Parent)
            if (p is Section s) return s.Theme.Surface;
            else if (p.BackColor.A == 255) return p.BackColor;
        return fallback;
    }

    /// <summary>Dark-friendly dropdown: owner-drawn items and closed box in the theme colours.</summary>
    static void ThemeCombo(ComboBox cb, Theme t)
    {
        cb.FlatStyle = FlatStyle.Flat;
        cb.BackColor = t.Input;
        cb.ForeColor = t.Text;
        cb.DrawMode = DrawMode.OwnerDrawFixed;
        cb.ItemHeight = 22;
        cb.DrawItem += (_, e) =>
        {
            if (e.Index < 0) return;
            bool sel = (e.State & DrawItemState.Selected) != 0 && (e.State & DrawItemState.ComboBoxEdit) == 0;
            using var b = new SolidBrush(sel ? t.AccentFill : t.Input);
            e.Graphics.FillRectangle(b, e.Bounds);
            var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis
                        | (cb.RightToLeft == RightToLeft.Yes ? TextFormatFlags.Right | TextFormatFlags.RightToLeft : TextFormatFlags.Left);
            var r = Rectangle.Inflate(e.Bounds, -4, 0);
            TextRenderer.DrawText(e.Graphics, cb.Items[e.Index]?.ToString(), cb.Font, r, sel ? Color.White : t.Text, flags);
        };
    }

    public static void Primary(Button b, Theme t)
    {
        b.FlatStyle = FlatStyle.Flat;
        b.BackColor = t.AccentFill;
        b.ForeColor = Color.White;
        b.FlatAppearance.BorderColor = t.AccentFill;
        b.FlatAppearance.MouseOverBackColor = Color.FromArgb(24, 150, 122);
        b.Font = new Font("Segoe UI Semibold", 9.5f);
    }
}
