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
                        IconPrev = "\uE76B", IconNext = "\uE76C";

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

    /// <summary>Small icon per prayer: the sun's height over a horizon, or the moon at night.</summary>
    public static void PrayerGlyph(Graphics g, Prayer p, RectangleF box, Color c)
    {
        float n = Math.Min(box.Width, box.Height);
        float ox = box.Left + (box.Width - n) / 2, oy = box.Top + (box.Height - n) / 2;
        using var pen = new Pen(c, Math.Max(1.2f, n / 11f)) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var brush = new SolidBrush(c);
        float horizon = oy + n * 0.78f;

        void Sun(float cx, float cy, float r, bool rays, bool clipToHorizon)
        {
            var st = g.Save();
            if (clipToHorizon) g.SetClip(new RectangleF(ox - 2, oy - 2, n + 4, horizon - oy + 1));
            g.FillEllipse(brush, cx - r, cy - r, r * 2, r * 2);
            if (rays)
                for (int i = 0; i < 8; i++)
                {
                    double a = Math.PI / 4 * i;
                    float r1 = r * 1.45f, r2 = r * 1.95f;
                    g.DrawLine(pen, cx + r1 * (float)Math.Cos(a), cy + r1 * (float)Math.Sin(a),
                                    cx + r2 * (float)Math.Cos(a), cy + r2 * (float)Math.Sin(a));
                }
            g.Restore(st);
        }

        switch (p)
        {
            case Prayer.Fajr:
                Crescent(g, new RectangleF(ox, oy, n * 0.8f, n * 0.8f), c, star: false);
                g.DrawLine(pen, ox + n * 0.05f, horizon, ox + n * 0.95f, horizon);
                break;
            case Prayer.Sunrise:
                Sun(ox + n / 2, horizon, n * 0.24f, true, true);
                g.DrawLine(pen, ox + n * 0.05f, horizon, ox + n * 0.95f, horizon);
                break;
            case Prayer.Dhuhr:
                Sun(ox + n / 2, oy + n / 2, n * 0.2f, true, false);
                break;
            case Prayer.Asr:
                Sun(ox + n * 0.62f, oy + n * 0.40f, n * 0.17f, true, false);
                g.DrawLine(pen, ox + n * 0.05f, horizon, ox + n * 0.95f, horizon);
                break;
            case Prayer.Maghrib:
                Sun(ox + n / 2, horizon + n * 0.06f, n * 0.28f, false, true);
                g.DrawLine(pen, ox + n * 0.05f, horizon, ox + n * 0.95f, horizon);
                break;
            case Prayer.Isha:
                Crescent(g, box, c, star: true);
                break;
        }
    }
}
