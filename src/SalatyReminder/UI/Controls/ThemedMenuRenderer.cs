namespace SalatyReminder.UI.Controls;

/// <summary>Menus in the app theme (the stock ones stay light in dark mode).</summary>
sealed class ThemedMenuRenderer(Theme t) : ToolStripProfessionalRenderer(new Colors(t))
{
    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        if (e.Item.ForeColor == SystemColors.ControlText) e.TextColor = e.Item.Enabled ? t.Text : t.Dim;
        base.OnRenderItemText(e);
    }

    protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
    {
        e.ArrowColor = t.Text;
        base.OnRenderArrow(e);
    }

    /// <summary>The stock check mark is a black bitmap; draw the icon font's check in the theme colour instead.</summary>
    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
    {
        var r = e.ImageRectangle;
        Art.FillRound(e.Graphics, Rectangle.Inflate(r, 2, 2), 4, Color.FromArgb(t.Dark ? 60 : 30, t.Accent));
        using var f = new Font(Art.IconFontName, 9f);
        TextRenderer.DrawText(e.Graphics, "", f, Rectangle.Inflate(r, 2, 2), t.Accent,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }

    sealed class Colors(Theme t) : ProfessionalColorTable
    {
        Color Hover => Color.FromArgb(t.Dark ? 60 : 30, t.Accent);
        public override Color ToolStripDropDownBackground => t.Surface;
        public override Color MenuBorder => t.Border;
        public override Color MenuItemBorder => Hover;
        public override Color MenuItemSelected => Hover;
        public override Color MenuItemSelectedGradientBegin => Hover;
        public override Color MenuItemSelectedGradientEnd => Hover;
        public override Color MenuItemPressedGradientBegin => Hover;
        public override Color MenuItemPressedGradientEnd => Hover;
        public override Color ImageMarginGradientBegin => t.Surface;
        public override Color ImageMarginGradientMiddle => t.Surface;
        public override Color ImageMarginGradientEnd => t.Surface;
        public override Color CheckBackground => t.Surface;
        public override Color CheckSelectedBackground => t.Surface;
        public override Color CheckPressedBackground => t.Surface;
        public override Color SeparatorDark => t.Border;
        public override Color SeparatorLight => t.Surface;
    }
}
