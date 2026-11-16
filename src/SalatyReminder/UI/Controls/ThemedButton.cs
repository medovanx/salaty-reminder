namespace SalatyReminder.UI.Controls;

/// <summary>
/// A flat button that paints its own text. A stock disabled button ignores ForeColor and draws
/// its text in the system grey, which looks black on the dark theme; this one fades to the theme instead.
/// </summary>
sealed class ThemedButton : Button
{
    bool hover, down;

    public ThemedButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        FlatStyle = FlatStyle.Flat;
    }

    protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { hover = down = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { down = true; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { down = false; Invalidate(); base.OnMouseUp(e); }
    protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        var fa = FlatAppearance;
        Color bg = !Enabled ? BackColor
            : down && !fa.MouseDownBackColor.IsEmpty ? fa.MouseDownBackColor
            : hover && !fa.MouseOverBackColor.IsEmpty ? fa.MouseOverBackColor
            : BackColor;
        g.Clear(bg);
        if (fa.BorderSize > 0)
        {
            using var pen = new Pen(fa.BorderColor.IsEmpty ? ForeColor : fa.BorderColor, fa.BorderSize);
            g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }
        // Disabled: the text colour pulled most of the way to the background.
        Color fg = Enabled ? ForeColor : Blend(ForeColor, bg, 0.55f);
        var flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine
                    | TextFormatFlags.EndEllipsis | (RightToLeft == RightToLeft.Yes ? TextFormatFlags.RightToLeft : 0);
        TextRenderer.DrawText(g, Text, Font, ClientRectangle, fg, flags);
        if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(g, Rectangle.Inflate(ClientRectangle, -3, -3), fg, bg);
    }

    static Color Blend(Color a, Color b, float t) => Color.FromArgb(
        (int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));
}
