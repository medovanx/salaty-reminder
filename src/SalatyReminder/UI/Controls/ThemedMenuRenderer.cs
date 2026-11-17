using System.Drawing.Drawing2D;

namespace SalatyReminder.UI.Controls;

/// <summary>
/// Menus in the app's own style, like the hover card and the flyout's popups: the theme surface, rounded
/// hover rows inset from the edges, an accent check, dim separators and submenu arrows, and an icon-font
/// glyph beside an item whose <see cref="ToolStripItem.Tag"/> is a <see cref="MenuGlyph"/>.
/// Call <see cref="Style"/> on a menu to also round its corners and give it roomier rows.
/// </summary>
sealed class ThemedMenuRenderer(Theme t) : ToolStripRenderer
{
    /// <summary>Put in an item's Tag to draw this icon-font glyph in the image margin.</summary>
    public sealed record MenuGlyph(string Glyph, Color? Color = null);

    static readonly Color Danger = Color.FromArgb(232, 72, 85);
    readonly Dictionary<ToolStrip, Rectangle> margins = [];

    Color Hover => Color.FromArgb(t.Dark ? 40 : 24, t.Accent);

    /// <summary>Rounds the menu and every submenu, gives the rows room, and themes submenus too.</summary>
    public static void Style(ToolStripDropDown menu, Theme theme)
    {
        var r = new ThemedMenuRenderer(theme);
        float s = menu.DeviceDpi / 96f;
        void Apply(ToolStripDropDown d)
        {
            d.Renderer = r;
            d.Padding = new Padding(0, (int)(4 * s), 0, (int)(4 * s));
            d.Font = new Font("Segoe UI", 9.5f);
            d.BackColor = theme.Surface;
            if (d is ToolStripDropDownMenu dm) dm.ImageScalingSize = new Size((int)(16 * s), (int)(16 * s));
            d.Opened += (_, _) => Native.RoundCorners(d.Handle, theme.Dark);
            foreach (ToolStripItem it in d.Items)
            {
                if (it is ToolStripSeparator) continue;
                it.Padding = new Padding(0, (int)(4 * s), 0, (int)(4 * s));
                if (it is ToolStripMenuItem { HasDropDownItems: true } mi) Apply(mi.DropDown);
            }
        }
        Apply(menu);
    }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        using var b = new SolidBrush(t.Surface);
        e.Graphics.FillRectangle(b, e.AffectedBounds);
    }

    // Windows draws the rounded border (RoundCorners); a square one would poke out of the corners.
    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e) { }

    protected override void OnRenderImageMargin(ToolStripRenderEventArgs e) => margins[e.ToolStrip] = e.AffectedBounds;

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var it = e.Item;
        float s = it.Owner?.DeviceDpi / 96f ?? 1f;
        if (it.Selected && it.Enabled)
        {
            var r = new RectangleF(4 * s, 1 * s, it.Width - 8 * s, it.Height - 2 * s);
            bool danger = it.ForeColor.ToArgb() == Danger.ToArgb();
            Art.FillRound(g, r, 5 * s, Color.FromArgb(t.Dark ? 40 : 24, danger ? Danger : t.Accent));
        }
        if (it.Tag is MenuGlyph mg && it.Owner is { } owner && margins.TryGetValue(owner, out var m) && m.Width > 0)
        {
            bool rtl = owner.RightToLeft == RightToLeft.Yes;
            float w = m.Width;
            var box = new RectangleF(rtl ? it.Width - w : 0, 0, w, it.Height);
            // Up a little: text is centred on its line box, whose visual middle sits above the box middle.
            box.Offset(rtl ? -2 * s : 2 * s, -TextLift(s));
            Art.GlyphCentered(g, mg.Glyph, 14 * s, box, !it.Enabled ? Color.FromArgb(120, t.Dim) : mg.Color ?? t.Dim);
        }
    }

    static float TextLift(float s) => 1.5f * s;

    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
    {
        float s = e.Item.Owner?.DeviceDpi / 96f ?? 1f;
        Art.GlyphCentered(e.Graphics, "\uE73E", 11 * s, RectangleF.FromLTRB(e.ImageRectangle.Left, e.ImageRectangle.Top - TextLift(s), e.ImageRectangle.Right, e.ImageRectangle.Bottom - TextLift(s)), t.Accent);
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        var it = e.Item;
        bool danger = it.ForeColor.ToArgb() == Danger.ToArgb();
        e.TextColor = !it.Enabled ? t.Dim : danger ? Danger : it is ToolStripMenuItem { Checked: true } ? t.Accent : t.Text;
        base.OnRenderItemText(e);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        float s = e.Item.Owner?.DeviceDpi / 96f ?? 1f;
        int y = e.Item.Height / 2;
        using var pen = new Pen(t.Border, Math.Max(1, s));
        e.Graphics.DrawLine(pen, 12 * s, y, e.Item.Width - 12 * s, y);
    }

    protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
    {
        e.ArrowColor = t.Dim;
        base.OnRenderArrow(e);
    }
}
