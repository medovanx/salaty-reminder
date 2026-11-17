using System.Drawing.Drawing2D;

namespace SalatyReminder.UI.Styling;

/// <summary>Shared drawing: brand colours, crescent glyph, geometric pattern, prayer icons.</summary>
static class Art
{
    public static readonly Color Gold = Color.FromArgb(246, 200, 92);
    public static readonly Color TealLight = Color.FromArgb(24, 150, 122);
    public static readonly Color TealDark = Color.FromArgb(9, 62, 58);

    // Windows icon font: Segoe Fluent Icons (Win11) or Segoe MDL2 Assets (Win10) share these code points.
    public const string IconSettings = "\uE713", IconLocation = "\uE707", IconCalendar = "\uE787",
                        IconPrev = "\uE76B", IconNext = "\uE76C", IconOpen = "\uE8A7", IconDocument = "\uE8A5";

    public static readonly string IconFontName =
        new System.Drawing.Text.InstalledFontCollection().Families.Any(f => f.Name == "Segoe Fluent Icons")
            ? "Segoe Fluent Icons" : "Segoe MDL2 Assets";

    public static Bitmap IconBitmap(string glyph, int px, Color c)
    {
        var bmp = new Bitmap(px, px, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        using var f = new Font(IconFontName, px * 0.8f, GraphicsUnit.Pixel);
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        using var b = new SolidBrush(c);
        g.DrawString(glyph, f, b, new RectangleF(0, 1, px, px), sf);
        return bmp;
    }

    /// <summary>Draws an icon-font glyph with its real ink centred in <paramref name="box"/> (glyph boxes sit off-centre).</summary>
    public static void GlyphCentered(Graphics g, string glyph, float emPixels, RectangleF box, Color c)
    {
        using var path = new GraphicsPath();
        using var family = new FontFamily(IconFontName);
        path.AddString(glyph, family, (int)FontStyle.Regular, emPixels, PointF.Empty, StringFormat.GenericTypographic);
        var b = path.GetBounds();
        if (b.Width <= 0 || b.Height <= 0) return;
        using (var m = new Matrix())
        {
            m.Translate(box.Left + (box.Width - b.Width) / 2f - b.Left, box.Top + (box.Height - b.Height) / 2f - b.Top);
            path.Transform(m);
        }
        var old = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var brush = new SolidBrush(c);
        g.FillPath(brush, path);
        g.SmoothingMode = old;
    }

    public static void FillRound(Graphics g, RectangleF r, float rad, Color c)
    {
        using var p = RoundRect(r, rad);
        using var b = new SolidBrush(c);
        g.FillPath(b, p);
    }

    public static GraphicsPath RoundRect(RectangleF r, float rad)
    {
        var p = new GraphicsPath();
        float d = Math.Min(rad * 2, Math.Min(r.Width, r.Height));
        p.AddArc(r.Left, r.Top, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Top, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    /// <summary>Crescent with a small four-point star, fitted into a square.</summary>
    public static void Crescent(Graphics g, RectangleF box, Color c, bool star = true)
    {
        float n = Math.Min(box.Width, box.Height);
        float ox = box.Left + (box.Width - n) / 2, oy = box.Top + (box.Height - n) / 2;
        float cx = ox + n * 0.46f, cy = oy + n * 0.52f, r = n * 0.40f;
        float r2 = r * 0.84f;
        using var path = CrescentPath(new PointF(cx, cy), r, new PointF(cx + n * 0.17f, cy - n * 0.09f), r2);
        var old = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var b = new SolidBrush(c);
        g.FillPath(b, path);
        if (star) Star(g, new PointF(ox + n * 0.80f, oy + n * 0.30f), n * 0.15f, n * 0.05f, c);
        g.SmoothingMode = old;
    }

    /// <summary>
    /// Outer circle minus an overlapping inner circle, as one antialiasable path of two arcs
    /// (a Region would be pixel-snapped and jagged).
    /// </summary>
    static GraphicsPath CrescentPath(PointF c1, float r1, PointF c2, float r2)
    {
        double dx = c2.X - c1.X, dy = c2.Y - c1.Y, d = Math.Sqrt(dx * dx + dy * dy);
        double a = (r1 * r1 - r2 * r2 + d * d) / (2 * d);
        double phi = Math.Atan2(dy, dx) * 180 / Math.PI;
        double alpha = Math.Acos(Math.Clamp(a / r1, -1, 1)) * 180 / Math.PI;

        PointF On(PointF c, float r, double deg) =>
            new(c.X + r * (float)Math.Cos(deg * Math.PI / 180), c.Y + r * (float)Math.Sin(deg * Math.PI / 180));
        double Ang(PointF c, PointF p) => Math.Atan2(p.Y - c.Y, p.X - c.X) * 180 / Math.PI;
        static double Norm(double x) => ((x % 360) + 360) % 360;

        var pMinus = On(c1, r1, phi - alpha);
        var pPlus = On(c1, r1, phi + alpha);

        var path = new GraphicsPath();
        // Outer arc: the long way round, away from the inner circle.
        path.AddArc(c1.X - r1, c1.Y - r1, r1 * 2, r1 * 2, (float)(phi + alpha), (float)(360 - 2 * alpha));
        // Inner arc: back from pMinus to pPlus through the side facing the outer centre.
        double start = Ang(c2, pMinus), end = Ang(c2, pPlus), facing = Ang(c2, c1);
        double sweep = Norm(end - start);
        if (Norm(facing - start) > sweep) sweep -= 360;
        path.AddArc(c2.X - r2, c2.Y - r2, r2 * 2, r2 * 2, (float)start, (float)sweep);
        path.CloseFigure();
        return path;
    }

    public static void Star(Graphics g, PointF center, float outer, float inner, Color c, int points = 4)
    {
        var pts = new PointF[points * 2];
        for (int i = 0; i < pts.Length; i++)
        {
            double a = Math.PI / points * i - Math.PI / 2;
            float rad = i % 2 == 0 ? outer : inner;
            pts[i] = new PointF(center.X + rad * (float)Math.Cos(a), center.Y + rad * (float)Math.Sin(a));
        }
        using var b = new SolidBrush(c);
        g.FillPolygon(b, pts);
    }

    /// <summary>Tiled eight-point stars (Rub el Hizb outlines) as a faint background pattern.</summary>
    public static void Pattern(Graphics g, Rectangle area, float cell, Color c)
    {
        using var pen = new Pen(c, Math.Max(1f, cell / 28f));
        var state = g.Save();
        g.SetClip(area);
        float half = cell * 0.36f;
        for (float y = area.Top - cell; y < area.Bottom + cell; y += cell)
            for (float x = area.Left - cell; x < area.Right + cell; x += cell)
            {
                float cx = x + ((int)((y - area.Top) / cell) % 2 == 0 ? 0 : cell / 2), cy = y;
                g.DrawPolygon(pen, Square(cx, cy, half, 0));
                g.DrawPolygon(pen, Square(cx, cy, half, 45));
            }
        g.Restore(state);
    }

    static PointF[] Square(float cx, float cy, float r, double rotDeg)
    {
        var pts = new PointF[4];
        for (int i = 0; i < 4; i++)
        {
            double a = (rotDeg + 45 + 90 * i) * Math.PI / 180;
            pts[i] = new PointF(cx + r * (float)Math.Cos(a), cy + r * (float)Math.Sin(a));
        }
        return pts;
    }

    // Prayer pictures in full colour, like Sky Glance's weather icons: gradient suns, a pale moon, a gold star.
    static readonly Color SunYellow = Color.FromArgb(255, 196, 46), SunYellowCore = Color.FromArgb(255, 228, 124);
    static readonly Color SunOrange = Color.FromArgb(255, 146, 48), SunOrangeCore = Color.FromArgb(255, 208, 112);
    static readonly Color SunAmber = Color.FromArgb(255, 172, 36), SunAmberCore = Color.FromArgb(255, 220, 116);
    static readonly Color SunRed = Color.FromArgb(246, 92, 62), SunRedCore = Color.FromArgb(255, 164, 98);
    static readonly Color MoonLight = Color.FromArgb(236, 240, 252), MoonShade = Color.FromArgb(198, 206, 230);
    static readonly Color DawnMoon = Color.FromArgb(184, 194, 255), DawnMoonShade = Color.FromArgb(124, 138, 238);
    static readonly Color StarGold = Color.FromArgb(255, 205, 60);
    /// <summary>Thin outline that keeps pale shapes (the moon, a yellow sun) visible on the light theme.</summary>
    static readonly Color Rim = Color.FromArgb(90, 60, 80, 110);

    /// <summary>
    /// Small coloured picture per prayer: the sun's height over a horizon, or the moon at night.
    /// <paramref name="faded"/> draws it see-through for a prayer that has passed.
    /// </summary>
    public static void PrayerGlyph(Graphics g, Prayer p, RectangleF box, bool dark, bool faded, Color line)
    {
        // At 20px, thin rays and small discs drawn straight onto the card come out jagged, and a gradient
        // fill doesn't antialias its own edge. Drawing at 4x and scaling down smooths every edge.
        const int K = 4, Pad = 2;
        var dest = Rectangle.Round(new RectangleF(box.X - Pad, box.Y - Pad, box.Width + Pad * 2, box.Height + Pad * 2));
        using var bmp = new Bitmap(dest.Width * K, dest.Height * K, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        using (var bg = Graphics.FromImage(bmp))
            DrawPrayer(bg, p, new RectangleF((box.X - dest.X) * K, (box.Y - dest.Y) * K, box.Width * K, box.Height * K), dark, line, K);
        var st = g.Save();
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.CompositingQuality = CompositingQuality.HighQuality;
        using var ia = new System.Drawing.Imaging.ImageAttributes();
        // Passed prayers: the whole picture fades, gradients and outlines together.
        if (faded) ia.SetColorMatrix(new System.Drawing.Imaging.ColorMatrix { Matrix33 = dark ? 0.38f : 0.45f });
        ia.SetWrapMode(WrapMode.TileFlipXY); // no dark fringe from sampling past the bitmap's edge
        g.DrawImage(bmp, dest, 0, 0, bmp.Width, bmp.Height, GraphicsUnit.Pixel, ia);
        g.Restore(st);
    }

    /// <summary>Draws one prayer picture; <paramref name="k"/> is the supersampling factor, for stroke widths.</summary>
    static void DrawPrayer(Graphics g, Prayer p, RectangleF box, bool dark, Color line, float k)
    {
        var st = g.Save();
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        float n = Math.Min(box.Width, box.Height);
        float ox = box.Left + (box.Width - n) / 2, oy = box.Top + (box.Height - n) / 2;
        float stroke = Math.Max(1.2f * k, n / 11f);
        using var linePen = new Pen(line, stroke) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        float horizon = oy + n * 0.78f;
        void Horizon() => g.DrawLine(linePen, ox + n * 0.05f, horizon, ox + n * 0.95f, horizon);

        void Sun(float cx, float cy, float r, bool rays, bool clipToHorizon, Color edge, Color core)
        {
            var s2 = g.Save();
            if (clipToHorizon) g.SetClip(new RectangleF(ox - 2 * k, oy - 2 * k, n + 4 * k, horizon - oy + k));
            if (rays)
            {
                // Thin yellow rays wash out on white; the light theme draws them a shade deeper.
                var ray = dark ? edge : Color.FromArgb(edge.R * 92 / 100, edge.G * 78 / 100, edge.B * 55 / 100);
                using var pen = new Pen(ray, stroke) { StartCap = LineCap.Round, EndCap = LineCap.Round };
                for (int i = 0; i < 8; i++)
                {
                    double a = Math.PI / 4 * i;
                    float r1 = r * 1.45f, r2 = r * 1.95f;
                    g.DrawLine(pen, cx + r1 * (float)Math.Cos(a), cy + r1 * (float)Math.Sin(a),
                                    cx + r2 * (float)Math.Cos(a), cy + r2 * (float)Math.Sin(a));
                }
            }
            var disc = new RectangleF(cx - r, cy - r, r * 2, r * 2);
            using var path = new GraphicsPath();
            path.AddEllipse(disc);
            if (!dark) { using var rim = new Pen(Color.FromArgb(80, 150, 90, 0), Math.Max(1f * k, n * 0.05f)); g.DrawPath(rim, path); }
            using (var pb = new PathGradientBrush(path) { CenterColor = core, SurroundColors = [edge], CenterPoint = new PointF(cx - r * 0.25f, cy - r * 0.25f) })
                g.FillPath(pb, path);
            g.Restore(s2);
        }

        void Moon(RectangleF mbox, Color light, Color shade, bool star)
        {
            float m = Math.Min(mbox.Width, mbox.Height);
            float cx = mbox.Left + m * 0.46f, cy = mbox.Top + m * 0.52f, r = m * 0.40f;
            using var path = CrescentPath(new PointF(cx, cy), r, new PointF(cx + m * 0.17f, cy - m * 0.09f), r * 0.84f);
            using (var rim = new Pen(Rim, Math.Max(1.2f * k, m * 0.06f)) { LineJoin = LineJoin.Round }) g.DrawPath(rim, path);
            using (var fill = new LinearGradientBrush(new RectangleF(cx - r, cy - r - 1, r * 2, r * 2 + 2), light, shade, LinearGradientMode.ForwardDiagonal))
                g.FillPath(fill, path);
            if (star) Star(g, new PointF(mbox.Left + m * 0.80f, mbox.Top + m * 0.30f), m * 0.16f, m * 0.055f, StarGold);
        }

        switch (p)
        {
            case Prayer.Fajr:
                Moon(new RectangleF(ox, oy, n * 0.8f, n * 0.8f), DawnMoon, DawnMoonShade, false);
                Horizon();
                break;
            case Prayer.Sunrise:
                Sun(ox + n / 2, horizon, n * 0.24f, true, true, SunOrange, SunOrangeCore);
                Horizon();
                break;
            case Prayer.Dhuhr:
                Sun(ox + n / 2, oy + n / 2, n * 0.2f, true, false, SunYellow, SunYellowCore);
                break;
            case Prayer.Asr:
                Sun(ox + n * 0.62f, oy + n * 0.40f, n * 0.17f, true, false, SunAmber, SunAmberCore);
                Horizon();
                break;
            case Prayer.Maghrib:
                Sun(ox + n / 2, horizon + n * 0.06f, n * 0.28f, false, true, SunRed, SunRedCore);
                Horizon();
                break;
            case Prayer.Isha:
                Moon(box, MoonLight, MoonShade, true);
                break;
        }
        g.Restore(st);
    }
}
