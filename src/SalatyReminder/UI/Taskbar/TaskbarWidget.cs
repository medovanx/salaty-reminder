using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using static SalatyReminder.Interop.Native;

namespace SalatyReminder.UI.Taskbar;

/// <summary>
/// A per-pixel-alpha child window inside the Windows taskbar (Shell_TrayWnd), placed just left of
/// the notification area and of any other app embedded there (e.g. TrafficMonitor).
/// Being a child of the taskbar, it hides with it in fullscreen and stays above the taskbar background.
/// </summary>
sealed class TaskbarWidget : NativeWindow
{
    const int WS_CHILD = 0x40000000, WS_CLIPSIBLINGS = 0x04000000;
    const int WS_EX_LAYERED = 0x80000, WS_EX_TOOLWINDOW = 0x80, WS_EX_NOACTIVATE = 0x08000000;
    const int WM_MOUSEMOVE = 0x200, WM_LBUTTONUP = 0x202, WM_RBUTTONUP = 0x205, WM_MOUSELEAVE = 0x2A3, WM_MOUSEACTIVATE = 0x21;

    public event Action? Clicked;
    public event Action? RightClicked;

    IntPtr tray;
    string line1 = "", line2 = "";
    bool highlight, hover, rtl;
    Size size;
    Point lastPos = new(int.MinValue, 0);

    /// <summary>Left end of the taskbar instead of beside the tray.</summary>
    public bool LeftSide { get; set; }
    /// <summary>Shift towards the taskbar centre, in pixels at 100% scale.</summary>
    public int Offset { get; set; }

    public void SetPlacement(bool left, int offset)
    {
        if (left == LeftSide && offset == Offset) return;
        LeftSide = left;
        Offset = offset;
        lastPos = new(int.MinValue, 0);
        if (Alive) Place();
    }

    public bool Alive => Handle != IntPtr.Zero && IsWindow(Handle);

    public Rectangle? ScreenBounds => Alive && GetWindowRect(Handle, out var r)
        ? Rectangle.FromLTRB(r.Left, r.Top, r.Right, r.Bottom) : null;

    /// <summary>Creates the window if needed (first run, Explorer restart) and keeps it positioned.</summary>
    public void Ensure()
    {
        var t = FindWindow("Shell_TrayWnd", null);
        if (t == IntPtr.Zero) return;
        if (Handle != IntPtr.Zero && (t != tray || !IsWindow(Handle)))
        {
            try { DestroyHandle(); } catch { ReleaseHandle(); }
        }
        if (Handle == IntPtr.Zero)
        {
            tray = t;
            lastPos = new(int.MinValue, 0);
            CreateHandle(new CreateParams
            {
                Caption = "SalatyReminderTaskbarWindow",
                Parent = t,
                Style = WS_CHILD | WS_CLIPSIBLINGS,
                ExStyle = WS_EX_LAYERED | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE,
                Width = 1, Height = 1,
            });
            Render();
        }
        Place();
    }

    public void SetContent(string l1, string l2, bool hl, bool isRtl)
    {
        if (l1 == line1 && l2 == line2 && hl == highlight && isRtl == rtl) return;
        line1 = l1; line2 = l2; highlight = hl; rtl = isRtl;
        if (Alive) { Render(); Place(); }
    }

    public void Invalidate() { if (Alive) { Render(); Place(); } }

    float Scale => tray != IntPtr.Zero && GetDpiForWindow(tray) is var d and > 0 ? d / 96f : 1f;

    void Place()
    {
        if (!GetWindowRect(tray, out var tr)) return;
        int anchor = TrayLeftEdge(tr);

        // Step left of any other app's window embedded in the right half of the taskbar.
        GetWindowThreadProcessId(tray, out uint explorerPid);
        int mid = tr.Left + tr.Width / 2;
        EnumChildWindows(tray, (h, _) =>
        {
            if (h == Handle || !IsWindowVisible(h)) return true;
            GetWindowThreadProcessId(h, out uint pid);
            if (pid == explorerPid || pid == Environment.ProcessId) return true;
            if (GetWindowRect(h, out var r) && r.Width > 0 && r.Left > mid && r.Left < anchor + 4)
                anchor = Math.Min(anchor, r.Left);
            return true;
        }, IntPtr.Zero);

        int gap = (int)(6 * Scale), shift = (int)(Offset * Scale);
        int y = Math.Max(0, (tr.Height - size.Height) / 2);
        Point pos;
        if (LeftSide)
            pos = new Point(Math.Clamp(gap + shift, 0, Math.Max(0, tr.Width - size.Width)), y);
        else
        {
            pos = new Point(Math.Max(0, anchor - size.Width - gap - shift - tr.Left), y);
            if (IsWin10) MakeRoomOnWin10(tr, pos.X + tr.Left - gap);
        }
        // Compare with where the window really is, so anything that moved it gets corrected.
        if (pos == lastPos && IsWindowVisible(Handle) && GetWindowRect(Handle, out var cur)
            && cur.Left - tr.Left == pos.X && cur.Top - tr.Top == pos.Y) return;
        lastPos = pos;
        SetWindowPos(Handle, IntPtr.Zero, pos.X, pos.Y, 0, 0, SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
    }

    volatile int uiaTrayLeft;   // last tray edge found through UI Automation
    DateTime uiaCheckedAt;

    /// <summary>
    /// Screen x where the notification area starts. TrayNotifyWnd is right when it has real bounds,
    /// but on some Windows 11 builds it stays 0 px wide at x=0 after boot (TrafficMonitor ends up
    /// on the left for the same reason). Then ask UI Automation for the leftmost tray button.
    /// </summary>
    int TrayLeftEdge(RECT tr)
    {
        var notify = FindWindowEx(tray, IntPtr.Zero, "TrayNotifyWnd", null);
        int mid = tr.Left + tr.Width / 2;
        if (notify != IntPtr.Zero && GetWindowRect(notify, out var nr) && nr.Width > 0 && nr.Left > mid && nr.Right <= tr.Right + 2)
            return nr.Left;

        // UI Automation runs on a background thread: this window is inside the taskbar, so querying
        // the taskbar from our own UI thread would call back into us and could deadlock.
        // Refresh at most every 5 s; the result is used from the next placement on.
        if (!uiaBusy && (DateTime.Now - uiaCheckedAt).TotalSeconds >= (uiaTrayLeft > mid ? 5 : 1))
        {
            uiaBusy = true;
            uiaCheckedAt = DateTime.Now;
            var taskbar = tray;
            Task.Run(() =>
            {
                int found = UiaTrayLeft(taskbar, mid);
                if (found > mid) uiaTrayLeft = found;
                uiaBusy = false;
            });
        }
        return uiaTrayLeft > mid ? uiaTrayLeft : tr.Right - (int)(300 * Scale); // until UIA answers: typical tray width
    }

    volatile bool uiaBusy;

    static int UiaTrayLeft(IntPtr taskbar, int mid)
    {
        try
        {
            var root = System.Windows.Automation.AutomationElement.FromHandle(taskbar);
            var cond = new System.Windows.Automation.PropertyCondition(
                System.Windows.Automation.AutomationElement.AutomationIdProperty, "SystemTrayIcon");
            int left = int.MaxValue;
            foreach (System.Windows.Automation.AutomationElement e in root.FindAll(System.Windows.Automation.TreeScope.Descendants, cond))
            {
                var r = e.Current.BoundingRectangle;
                if (r.Width > 0 && r.X > mid) left = Math.Min(left, (int)r.X);
            }
            return left == int.MaxValue ? 0 : left;
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            return 0;
        }
    }

    static readonly bool IsWin10 = Environment.OSVersion.Version.Build < 22000;

    /// <summary>
    /// Windows 10 task buttons run from the Start button to the tray, so shrink their strip
    /// (ReBarWindow32) to end before the widget, the way TrafficMonitor does.
    /// Explorer re-lays it out by itself when we exit.
    /// </summary>
    void MakeRoomOnWin10(RECT taskbar, int screenRight)
    {
        var rebar = FindWindowEx(tray, IntPtr.Zero, "ReBarWindow32", null);
        if (rebar == IntPtr.Zero || !GetWindowRect(rebar, out var rr)) return;
        if (taskbar.Height > taskbar.Width) return; // vertical taskbar: leave it alone
        if (rr.Right <= screenRight) return;
        int width = Math.Max(0, screenRight - rr.Left);
        SetWindowPos(rebar, IntPtr.Zero, rr.Left - taskbar.Left, rr.Top - taskbar.Top, width, rr.Height, SWP_NOACTIVATE | 0x4 /* NOZORDER */);
    }

    void Render()
    {
        if (!GetWindowRect(tray, out var tr)) return;
        using var bmp = Paint(line1, line2, highlight, hover, rtl, SystemDark(), Scale, tr.Height);
        size = bmp.Size;
        int w = size.Width, h = size.Height;
        var screen = GetDC(IntPtr.Zero);
        var mem = CreateCompatibleDC(screen);
        var hbmp = bmp.GetHbitmap(Color.FromArgb(0));
        var old = SelectObject(mem, hbmp);
        try
        {
            var sz = new SIZE { CX = w, CY = h };
            var src = new POINT();
            var blend = new BLENDFUNCTION { BlendOp = 0, SourceConstantAlpha = 255, AlphaFormat = 1 };
            UpdateLayeredWindow(Handle, screen, IntPtr.Zero, ref sz, mem, ref src, 0, ref blend, 2 /* ULW_ALPHA */);
        }
        finally
        {
            SelectObject(mem, old);
            DeleteObject(hbmp);
            DeleteDC(mem);
            ReleaseDC(IntPtr.Zero, screen);
        }
    }


    /// <summary>Draws the widget (pill, crescent, two text lines) at a given scale and height.</summary>
    public static Bitmap Paint(string line1, string line2, bool highlight, bool hover, bool rtl, bool dark, float s, int h)
    {
        using var f1 = new Font("Segoe UI Semibold", 12f * s, GraphicsUnit.Pixel);
        using var f2 = new Font("Segoe UI", 12f * s, GraphicsUnit.Pixel);
        SizeF m1, m2;
        using (var probe = new Bitmap(1, 1))
        using (var pg = Graphics.FromImage(probe))
        {
            pg.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            m1 = pg.MeasureString(line1, f1);
            m2 = pg.MeasureString(line2, f2);
        }
        int pad = (int)(9 * s), icon = (int)(22 * s), iconGap = (int)(7 * s);
        int textW = (int)Math.Ceiling(Math.Max(m1.Width, m2.Width)) + (int)(2 * s);
        int w = pad + icon + iconGap + textW + pad;

        var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit; // grayscale AA: correct on per-pixel alpha
            g.Clear(Color.FromArgb(1, 0, 0, 0)); // alpha 1 keeps the whole box clickable

            // Soft teal pill, brighter on hover, solid while flashing at prayer time.
            var box = new RectangleF(1, 4 * s, w - 2, h - 8 * s);
            int a1 = highlight ? 245 : hover ? (dark ? 95 : 70) : (dark ? 55 : 40);
            int a2 = highlight ? 245 : hover ? (dark ? 70 : 50) : (dark ? 30 : 22);
            using (var path = Art.RoundRect(box, 6 * s))
            using (var bg = new LinearGradientBrush(box, Color.FromArgb(a1, Art.TealLight), Color.FromArgb(a2, Art.TealDark), LinearGradientMode.ForwardDiagonal))
            {
                g.FillPath(bg, path);
                using var edge = new Pen(Color.FromArgb(highlight ? 120 : dark ? 40 : 30, dark || highlight ? Color.White : Art.TealDark), Math.Max(1f, s));
                g.DrawPath(edge, path);
            }

            int iconX = rtl ? w - pad - icon : pad;
            int textX = rtl ? pad : pad + icon + iconGap;
            Color glyph = highlight ? Color.White : dark ? Art.Gold : Color.FromArgb(176, 122, 10);
            Art.Crescent(g, new RectangleF(iconX, (h - icon) / 2f, icon, icon), glyph);

            Color fg = highlight || dark ? Color.White : Color.FromArgb(20, 20, 20);
            Color fg2 = Color.FromArgb(highlight ? 235 : 190, fg);
            float total = f1.GetHeight(g) + f2.GetHeight(g) - 2 * s;
            float y = (h - total) / 2;
            using var sf = new StringFormat { Alignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap };
            if (rtl) sf.FormatFlags |= StringFormatFlags.DirectionRightToLeft;
            var tr1 = new RectangleF(textX, y, textW, h);
            var tr2 = new RectangleF(textX, y + f1.GetHeight(g) - 2 * s, textW, h);
            using (var b1 = new SolidBrush(fg)) g.DrawString(line1, f1, b1, tr1, sf);
            using (var b2 = new SolidBrush(fg2)) g.DrawString(line2, f2, b2, tr2, sf);
        }
        return bmp;
    }

    protected override void WndProc(ref Message m)
    {
        switch (m.Msg)
        {
            case WM_MOUSEACTIVATE:
                m.Result = 3; // MA_NOACTIVATE
                return;
            case WM_MOUSEMOVE when !hover:
                hover = true;
                var tme = new TRACKMOUSEEVENT { cbSize = System.Runtime.InteropServices.Marshal.SizeOf<TRACKMOUSEEVENT>(), dwFlags = 2 /* TME_LEAVE */, hwndTrack = Handle };
                TrackMouseEvent(ref tme);
                Render();
                break;
            case WM_MOUSELEAVE:
                hover = false;
                Render();
                break;
            case WM_LBUTTONUP:
                Clicked?.Invoke();
                return;
            case WM_RBUTTONUP:
                RightClicked?.Invoke();
                return;
        }
        base.WndProc(ref m);
    }
}
