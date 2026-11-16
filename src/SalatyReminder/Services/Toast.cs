using System.Runtime.InteropServices;
using System.Security;
using Microsoft.Win32;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace SalatyReminder.Services;

/// <summary>
/// Windows toast notifications without a tray icon. A portable exe has no Start menu shortcut, so the
/// app id is registered under HKCU\Software\Classes\AppUserModelId with a name and icon, which is
/// enough for Windows 10 and 11 to show toasts from an unpackaged app.
/// </summary>
static class Toast
{
    const string AppId = "medovanx.SalatyReminder";
    static ToastNotifier? notifier;
    static Action<Action>? post;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern int SetCurrentProcessExplicitAppUserModelID(string appId);

    /// <param name="icon">App icon, saved as the toast's PNG.</param>
    /// <param name="uiPost">Runs a click handler on the UI thread.</param>
    public static void Init(Icon icon, Action<Action> uiPost)
    {
        post = uiPost;
        try
        {
            SetCurrentProcessExplicitAppUserModelID(AppId);
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SalatyReminder");
            Directory.CreateDirectory(dir);
            var png = Path.Combine(dir, "toast-icon.png");
            using (var big = new Icon(icon, 256, 256)) using (var bmp = big.ToBitmap()) bmp.Save(png, System.Drawing.Imaging.ImageFormat.Png);
            using (var k = Registry.CurrentUser.CreateSubKey(@"Software\Classes\AppUserModelId\" + AppId))
            {
                k.SetValue("DisplayName", L.T("AppName"));
                k.SetValue("IconUri", png);
            }
            notifier = ToastNotificationManager.CreateToastNotifier(AppId);
        }
        catch (Exception ex) { Log.Error(ex); }
    }

    /// <summary>Shows a toast. <paramref name="silent"/> for when the adhan is about to play anyway.</summary>
    public static void Show(string title, string body, Action? onClick = null, bool silent = false)
    {
        if (notifier == null) return;
        try
        {
            var xml = new XmlDocument();
            xml.LoadXml($"""
                <toast>
                  <visual><binding template="ToastGeneric"><text>{SecurityElement.Escape(title)}</text><text>{SecurityElement.Escape(body)}</text></binding></visual>
                  {(silent ? "<audio silent=\"true\"/>" : "")}
                </toast>
                """);
            var toast = new ToastNotification(xml);
            if (onClick != null) toast.Activated += (_, _) => post?.Invoke(onClick);
            notifier.Show(toast);
        }
        catch (Exception ex) { Log.Error(ex); }
    }
}
