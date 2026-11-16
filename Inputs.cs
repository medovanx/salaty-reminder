using System.Drawing.Drawing2D;
using System.Globalization;

namespace SalatyReminder;

/// <summary>Dropdown drawn in the app theme, with its own themed list popup (the stock ComboBox ignores colours).</summary>
sealed class ThemedCombo : Control
{
    public List<string> Items { get; } = [];
    public Theme Theme { get; set; } = Theme.Current;
    public event EventHandler? SelectedIndexChanged;

    int selected = -1;
    bool hover, open;
    ToolStripDropDown? popup;

    public ThemedCombo()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint
                 | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        Size = new Size(220, 30);
        Cursor = Cursors.Hand;
        TabStop = true;
    }

    public int SelectedIndex
    {
        get => selected;
        set
        {
            if (value < -1 || value >= Items.Count || value == selected) return;
            selected = value;
            Invalidate();
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Setting true opens the list (used after a city search).</summary>
    public bool DroppedDown
    {
        get => open;
        set { if (value && !open) Open(); else if (!value) popup?.Close(); }
    }

    protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); }
    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
    protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();
        if (e.Button == MouseButtons.Left && Items.Count > 0) Open();
    }

    protected override bool IsInputKey(Keys k) => k is Keys.Up or Keys.Down || base.IsInputKey(k);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Down && e.Alt || e.KeyCode is Keys.F4 or Keys.Space or Keys.Enter) Open();
        else if (e.KeyCode == Keys.Down) SelectedIndex = Math.Min(Items.Count - 1, selected + 1);
        else if (e.KeyCode == Keys.Up) SelectedIndex = Math.Max(0, selected - 1);
        base.OnKeyDown(e);
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        if (!Focused || Items.Count == 0) return;
        SelectedIndex = Math.Clamp(selected + (e.Delta > 0 ? -1 : 1), 0, Items.Count - 1);
    }

    void Open()
    {
        if (open || Items.Count == 0) return;
        var list = new ThemedList(this) { Theme = Theme, RightToLeft = RightToLeft, Font = Font };
        int rowH = (int)(30 * DeviceDpi / 96f);
        int visible = Math.Min(Items.Count, 10);
        list.Size = new Size(Width, rowH * visible + 2);
        var host = new ToolStripControlHost(list) { Margin = Padding.Empty, Padding = Padding.Empty, AutoSize = false, Size = list.Size };
        popup = new ToolStripDropDown { Padding = Padding.Empty, DropShadowEnabled = true, BackColor = Theme.Surface, Renderer = new BorderRenderer(Theme) };
        popup.Items.Add(host);
        popup.Closed += (_, _) => { open = false; Invalidate(); popup?.Dispose(); popup = null; };
        open = true;
        Invalidate();
        popup.Show(this, new Point(0, Height + 2));
        list.Focus();
    }

    internal void Pick(int index)
    {
        SelectedIndex = index;
        popup?.Close();
        Focus();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Styler.Backdrop(this, Theme.Surface));
        float s = DeviceDpi / 96f;
        bool rtl = RightToLeft == RightToLeft.Yes;
        var box = new RectangleF(0.5f, 0.5f, Width - 1, Height - 1);
        using (var p = Art.RoundRect(box, 6 * s))
        {
            using var b = new SolidBrush(hover && Enabled ? Color.FromArgb(Theme.Dark ? 60 : 238, Theme.Dark ? 64 : 241, Theme.Dark ? 64 : 240) : Theme.Input);
            g.FillPath(b, p);
            using var pen = new Pen(open || Focused ? Theme.Accent : Theme.Border, open || Focused ? 1.4f * s : 1f);
            g.DrawPath(pen, p);
        }

        Color fg = Enabled ? Theme.Text : Theme.Dim;
        int chevW = (int)(28 * s), pad = (int)(10 * s);
        var chev = new Rectangle(rtl ? 0 : Width - chevW, 0, chevW, Height);
        using (var f = new Font(Art.IconFontName, 8f))
            TextRenderer.DrawText(g, "\uE70D", f, chev, Theme.Dim, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        if (selected >= 0)
        {
            var tr = rtl ? new Rectangle(chevW, 0, Width - chevW - pad, Height) : new Rectangle(pad, 0, Width - chevW - pad, Height);
            TextRenderer.DrawText(g, Items[selected], Font, tr, fg, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine
                | (rtl ? TextFormatFlags.Right | TextFormatFlags.RightToLeft : TextFormatFlags.Left));
        }
    }

    sealed class BorderRenderer(Theme t) : ToolStripRenderer
    {
        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            using var pen = new Pen(t.Border);
            e.Graphics.DrawRectangle(pen, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
        }
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e) => e.Graphics.Clear(t.Surface);
    }

    /// <summary>The list shown inside the popup.</summary>
    sealed class ThemedList(ThemedCombo owner) : Control
    {
        public Theme Theme { get; set; } = Theme.Current;
        int hover = -1, top;

        int RowH => (int)(30 * DeviceDpi / 96f);
        int Visible => Math.Max(1, (Height - 2) / RowH);

        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            hover = owner.selected;
            top = Math.Clamp(owner.selected - Visible + 1, 0, Math.Max(0, owner.Items.Count - Visible));
        }

        int IndexAt(Point p) { int i = top + (p.Y - 1) / RowH; return i >= 0 && i < owner.Items.Count ? i : -1; }

        protected override void OnMouseMove(MouseEventArgs e) { int i = IndexAt(e.Location); if (i != hover) { hover = i; Invalidate(); } }
        protected override void OnMouseClick(MouseEventArgs e) { int i = IndexAt(e.Location); if (i >= 0) owner.Pick(i); }
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            top = Math.Clamp(top + (e.Delta > 0 ? -1 : 1), 0, Math.Max(0, owner.Items.Count - Visible));
            Invalidate();
        }
        protected override bool IsInputKey(Keys k) => k is Keys.Up or Keys.Down or Keys.Enter || base.IsInputKey(k);
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down) hover = Math.Min(owner.Items.Count - 1, hover + 1);
            else if (e.KeyCode == Keys.Up) hover = Math.Max(0, hover - 1);
            else if (e.KeyCode == Keys.Enter && hover >= 0) { owner.Pick(hover); return; }
            if (hover < top) top = hover;
            if (hover >= top + Visible) top = hover - Visible + 1;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Theme.Surface);
            float s = DeviceDpi / 96f;
            bool rtl = RightToLeft == RightToLeft.Yes;
            for (int i = top; i < Math.Min(owner.Items.Count, top + Visible); i++)
            {
                var r = new Rectangle(1, 1 + (i - top) * RowH, Width - 2, RowH);
                var inner = Rectangle.Inflate(r, -(int)(4 * s), -(int)(2 * s));
                bool sel = i == owner.selected;
                if (i == hover || sel)
                {
                    using var p = Art.RoundRect(inner, 5 * s);
                    using var b = new SolidBrush(i == hover ? Color.FromArgb(Theme.Dark ? 40 : 24, Theme.Accent) : Color.FromArgb(Theme.Dark ? 22 : 12, Theme.Text));
                    g.FillPath(b, p);
                }
                if (sel)
                {
                    using var ab = new SolidBrush(Theme.Accent);
                    g.FillRectangle(ab, rtl ? inner.Right - (int)(3 * s) : inner.Left, inner.Top + (int)(7 * s), (int)(3 * s), inner.Height - (int)(14 * s));
                }
                var tr = Rectangle.Inflate(inner, -(int)(10 * s), 0);
                TextRenderer.DrawText(g, owner.Items[i], Font, tr, sel ? Theme.Accent : Theme.Text,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine
                    | (rtl ? TextFormatFlags.Right | TextFormatFlags.RightToLeft : TextFormatFlags.Left));
            }
        }
    }
}

/// <summary>Single-line text box with a rounded themed border (the stock border can't be recoloured).</summary>
sealed class ThemedText : Control
{
    readonly TextBox box = new() { BorderStyle = BorderStyle.None };
    public Theme Theme { get; set; } = Theme.Current;

    public ThemedText()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        Size = new Size(260, 30);
        Controls.Add(box);
        box.KeyDown += (_, e) => OnKeyDown(e);
        box.TextChanged += (_, e) => OnTextChanged(e);
        box.GotFocus += (_, _) => Invalidate();
        box.LostFocus += (_, _) => Invalidate();
        Cursor = Cursors.IBeam;
    }

#pragma warning disable CS8765 // Text is never null here
    public override string Text { get => box.Text; set => box.Text = value ?? ""; }
#pragma warning restore CS8765
    public string PlaceholderText { get => box.PlaceholderText; set => box.PlaceholderText = value; }

    protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); box.Focus(); }
    protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); box.Enabled = Enabled; Invalidate(); }

    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        int pad = (int)(10 * DeviceDpi / 96f);
        box.Font = Font;
        box.RightToLeft = RightToLeft;
        box.Width = Math.Max(10, Width - pad * 2);
        box.Location = new Point(pad, (Height - box.PreferredHeight) / 2 + 1);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Styler.Backdrop(this, Theme.Surface));
        box.BackColor = Theme.Input;
        box.ForeColor = Enabled ? Theme.Text : Theme.Dim;
        float s = DeviceDpi / 96f;
        using var p = Art.RoundRect(new RectangleF(0.5f, 0.5f, Width - 1, Height - 1), 6 * s);
        using var b = new SolidBrush(Theme.Input);
        g.FillPath(b, p);
        using var pen = new Pen(box.Focused ? Theme.Accent : Theme.Border, box.Focused ? 1.4f * s : 1f);
        g.DrawPath(pen, p);
    }
}

/// <summary>Number box drawn in the app theme: [−] value [+], typing, arrow keys and mouse wheel.</summary>
sealed class ThemedNumber : Control
{
    readonly TextBox text = new() { BorderStyle = BorderStyle.None, TextAlign = HorizontalAlignment.Center };
    decimal value;
    Rectangle rMinus, rPlus;
    int hot; // -1 minus, 1 plus
    public Theme Theme { get; set; } = Theme.Current;
    public decimal Minimum { get; set; }
    public decimal Maximum { get; set; } = 100;
    public int DecimalPlaces { get; set; }
    public decimal Increment { get; set; } = 1;
    public event EventHandler? ValueChanged;

    public ThemedNumber()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        Size = new Size(130, 30);
        Controls.Add(text);
        text.Leave += (_, _) => Commit();
        text.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) { Commit(); e.SuppressKeyPress = true; }
            else if (e.KeyCode == Keys.Up) { Step(1); e.SuppressKeyPress = true; }
            else if (e.KeyCode == Keys.Down) { Step(-1); e.SuppressKeyPress = true; }
        };
        text.MouseWheel += (_, e) => Step(e.Delta > 0 ? 1 : -1);
        text.GotFocus += (_, _) => Invalidate();
        text.LostFocus += (_, _) => Invalidate();
        text.Text = "0";
    }

    public decimal Value
    {
        get => value;
        set
        {
            var v = Math.Round(Math.Clamp(value, Minimum, Maximum), DecimalPlaces);
            bool changed = v != this.value;
            this.value = v;
            text.Text = v.ToString("F" + DecimalPlaces, CultureInfo.InvariantCulture);
            if (changed) ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    void Commit()
    {
        if (decimal.TryParse(text.Text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var v)) Value = v;
        else Value = value; // restore the last good value
    }

    void Step(int dir) { Commit(); Value = value + dir * Increment; }

    protected override void OnMouseWheel(MouseEventArgs e) => Step(e.Delta > 0 ? 1 : -1);

    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        int b = Height;
        rMinus = new Rectangle(0, 0, b, Height);
        rPlus = new Rectangle(Width - b, 0, b, Height);
        text.Font = Font;
        text.Width = Math.Max(10, Width - 2 * b - 4);
        text.Location = new Point(b + 2, (Height - text.PreferredHeight) / 2 + 1);
        ApplyColors();
    }

    void ApplyColors()
    {
        text.BackColor = Theme.Input;
        text.ForeColor = Enabled ? Theme.Text : Theme.Dim;
    }

    protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); text.Enabled = Enabled; ApplyColors(); Invalidate(); }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        int h = rMinus.Contains(e.Location) ? -1 : rPlus.Contains(e.Location) ? 1 : 0;
        Cursor = h != 0 ? Cursors.Hand : Cursors.Default;
        if (h != hot) { hot = h; Invalidate(); }
    }

    protected override void OnMouseLeave(EventArgs e) { hot = 0; Invalidate(); }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (!Enabled || e.Button != MouseButtons.Left) return;
        if (rMinus.Contains(e.Location)) Step(-1);
        else if (rPlus.Contains(e.Location)) Step(1);
        else text.Focus();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Styler.Backdrop(this, Theme.Surface));
        ApplyColors();
        float s = DeviceDpi / 96f;
        var box = new RectangleF(0.5f, 0.5f, Width - 1, Height - 1);
        using (var p = Art.RoundRect(box, 6 * s))
        {
            using var b = new SolidBrush(Theme.Input);
            g.FillPath(b, p);
            using var pen = new Pen(text.Focused ? Theme.Accent : Theme.Border, text.Focused ? 1.4f * s : 1f);
            g.DrawPath(pen, p);
        }
        using var f = new Font(Art.IconFontName, 8f);
        Color c = Enabled ? Theme.Text : Theme.Dim;
        foreach (var (r, glyph, id) in new[] { (rMinus, "\uE738", -1), (rPlus, "\uE710", 1) })
        {
            if (hot == id && Enabled)
            {
                using var hp = Art.RoundRect(Rectangle.Inflate(r, -(int)(3 * s), -(int)(3 * s)), 4 * s);
                using var hb = new SolidBrush(Color.FromArgb(Theme.Dark ? 36 : 20, Theme.Accent));
                g.FillPath(hb, hp);
            }
            TextRenderer.DrawText(g, glyph, f, r, c, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }
}
