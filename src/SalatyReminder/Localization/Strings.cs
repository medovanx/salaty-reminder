using System.Globalization;

namespace SalatyReminder.Localization;

/// <summary>English / Arabic UI text and formatting.</summary>
public static class L
{
    public static bool Ar { get; set; }

    static readonly Dictionary<string, (string en, string ar)> D = new()
    {
        ["AppName"] = ("Salaty Reminder", "صلاتي"),
        ["Fajr"] = ("Fajr", "الفجر"),
        ["Sunrise"] = ("Sunrise", "الشروق"),
        ["Dhuhr"] = ("Dhuhr", "الظهر"),
        ["Jumuah"] = ("Jumu'ah", "الجمعة"),
        ["Asr"] = ("Asr", "العصر"),
        ["Maghrib"] = ("Maghrib", "المغرب"),
        ["Isha"] = ("Isha", "العشاء"),
        ["Now"] = ("now", "الآن"),
        ["TimeToPray"] = ("Time to pray", "حان وقت الصلاة"),
        ["NotifTitle"] = ("It's time for {0}", "حان الآن موعد صلاة {0}"),
        ["EarlyTitle"] = ("{0} in {1} minutes", "صلاة {0} بعد {1} دقيقة"),
        ["NextPrayer"] = ("Next prayer", "الصلاة القادمة"),
        ["Tomorrow"] = ("Tomorrow", "غدًا"),
        ["BackToToday"] = ("Back to today", "العودة إلى اليوم"),
        ["Today"] = ("Today", "اليوم"),
        ["WidgetPosition"] = ("Taskbar position", "الموضع في شريط المهام"),
        ["WidgetOffset"] = ("Shift (pixels)", "الإزاحة (بكسل)"),
        ["SideRight"] = ("Right, next to the tray", "يمين، بجوار منطقة الإشعارات"),
        ["SideLeft"] = ("Left side", "الجانب الأيسر"),
        ["ShowSunnah"] = ("Show sunnah prayers (rawatib)", "عرض السنن الرواتب"),
        ["BuiltInAdhan"] = ("Built-in adhan (or choose a file)", "الأذان المدمج (أو اختر ملفًا)"),
        ["Updates"] = ("Updates", "التحديثات"),
        ["AutoCheck"] = ("Check for updates automatically", "التحقق من التحديثات تلقائيًا"),
        ["AutoInstall"] = ("Install updates automatically", "تثبيت التحديثات تلقائيًا"),
        ["CheckNow"] = ("Check now", "تحقق الآن"),
        ["SourceCode"] = ("Source code and releases", "الكود المصدري والإصدارات"),
        ["CheckForUpdates"] = ("Check for updates", "التحقق من التحديثات"),
        ["Version"] = ("Version {0}", "الإصدار {0}"),
        ["UpToDate"] = ("You're on the latest version ({0}).", "لديك أحدث إصدار ({0})."),
        ["UpdateAvailable"] = ("Version {0} is available. Click to install.", "الإصدار {0} متاح. انقر للتثبيت."),
        ["Updating"] = ("Updating to {0}…", "جارٍ التحديث إلى {0}…"),
        ["UpdateFailed"] = ("Couldn't install the update. Opening the download page.", "تعذر تثبيت التحديث. سيتم فتح صفحة التنزيل."),
        ["UpdateCheckFailed"] = ("Couldn't check for updates.", "تعذر التحقق من التحديثات."),
        ["Updated"] = ("Updated to version {0}.", "تم التحديث إلى الإصدار {0}."),
        ["ShowTimes"] = ("Show prayer times", "عرض مواقيت الصلاة"),
        ["StopAdhan"] = ("Stop adhan", "إيقاف الأذان"),
        ["RefreshLocation"] = ("Refresh location", "تحديث الموقع"),
        ["Settings"] = ("Settings", "الإعدادات"),
        ["StartWithWindows"] = ("Start with Windows", "التشغيل مع ويندوز"),
        ["Exit"] = ("Exit", "خروج"),
        ["General"] = ("General", "عام"),
        ["Language"] = ("Language", "اللغة"),
        ["Location"] = ("Location", "الموقع"),
        ["Auto"] = ("Detect automatically", "تحديد تلقائي"),
        ["AutoNote"] = ("Uses Windows location; if that is off, your IP address is looked up online.",
                        "يستخدم موقع ويندوز، وإذا كان معطلاً يتم تحديد الموقع من عنوان IP عبر الإنترنت."),
        ["Manual"] = ("Choose a city", "اختيار مدينة"),
        ["Search"] = ("Search", "بحث"),
        ["City"] = ("City", "المدينة"),
        ["Latitude"] = ("Latitude", "خط العرض"),
        ["Longitude"] = ("Longitude", "خط الطول"),
        ["NoResults"] = ("No cities found", "لم يتم العثور على مدن"),
        ["Calculation"] = ("Calculation", "الحساب"),
        ["Method"] = ("Method", "طريقة الحساب"),
        ["AsrMethod"] = ("Asr", "العصر"),
        ["AsrStd"] = ("Standard (Shafi'i, Maliki, Hanbali)", "الجمهور (الشافعي والمالكي والحنبلي)"),
        ["AsrHanafi"] = ("Hanafi", "الحنفي"),
        ["TimeFormat"] = ("Time format", "صيغة الوقت"),
        ["H12"] = ("12-hour", "12 ساعة"),
        ["H24"] = ("24-hour", "24 ساعة"),
        ["HijriAdjust"] = ("Hijri date adjustment (days)", "تعديل التاريخ الهجري (أيام)"),
        ["Reminders"] = ("Reminders", "التنبيهات"),
        ["NotifyAtTime"] = ("Notify me at prayer time", "تنبيهي عند دخول وقت الصلاة"),
        ["EarlyNotify"] = ("Remind me before (minutes)", "تذكيري قبل الصلاة (بالدقائق)"),
        ["Flash"] = ("Flash the taskbar widget", "وميض الأداة في شريط المهام"),
        ["PlayAdhan"] = ("Play adhan sound", "تشغيل صوت الأذان"),
        ["AdhanFile"] = ("Adhan audio file", "ملف صوت الأذان"),
        ["Browse"] = ("Browse…", "استعراض…"),
        ["Test"] = ("Test", "تجربة"),
        ["Stop"] = ("Stop", "إيقاف"),
        ["Save"] = ("Save", "حفظ"),
        ["Cancel"] = ("Cancel", "إلغاء"),
        ["AdhanMissing"] = ("Choose an adhan audio file first.", "اختر ملف صوت الأذان أولاً."),
        ["LocFailed"] = ("Couldn't detect your location. Still using {0}.", "تعذر تحديد موقعك. ما زال يتم استخدام {0}."),
        ["LocUpdated"] = ("Location: {0}", "الموقع: {0}"),
        ["Locating"] = ("Locating…", "جارٍ تحديد الموقع…"),
    };

    public static string T(string key) => D.TryGetValue(key, out var v) ? (Ar ? v.ar : v.en) : key;
    public static string F(string key, params object[] args) => string.Format(T(key), args);

    public static CultureInfo Culture => Ar ? CultureInfo.GetCultureInfo("ar-EG") : CultureInfo.GetCultureInfo("en-US");

    /// <summary>Sunan al-Rawatib (confirmed sunnah) around each prayer, plus Duha after sunrise.</summary>
    public static string Sunnah(Prayer p) => p switch
    {
        Prayer.Fajr => Ar ? "السنة: ركعتان قبل الفرض" : "Sunnah: 2 before",
        Prayer.Sunrise => Ar ? "الضحى: بعد الشروق بربع ساعة" : "Duha: from ~15 min after sunrise",
        Prayer.Dhuhr => Ar ? "السنة: ٤ قبل، ٢ بعد" : "Sunnah: 4 before, 2 after",
        Prayer.Asr => "",
        Prayer.Maghrib => Ar ? "السنة: ركعتان بعد الفرض" : "Sunnah: 2 after",
        Prayer.Isha => Ar ? "السنة: ركعتان بعد، ثم الوتر" : "Sunnah: 2 after, then Witr",
        _ => "",
    };

    public static string MethodName(MethodDef m) => Ar ? m.NameAr : m.NameEn;

    public static string PrayerName(Prayer p, DateTime when) =>
        p == Prayer.Dhuhr && when.DayOfWeek == DayOfWeek.Friday ? T("Jumuah") : T(p.ToString());

    public static string Time(DateTime t, bool use24h)
    {
        if (use24h) return t.ToString("HH:mm", CultureInfo.InvariantCulture);
        string hm = t.ToString("h:mm", CultureInfo.InvariantCulture);
        return Ar ? $"{hm} {(t.Hour < 12 ? "ص" : "م")}" : $"{hm} {(t.Hour < 12 ? "AM" : "PM")}";
    }

    public static string Countdown(TimeSpan r)
    {
        int mins = Math.Max(0, (int)Math.Ceiling(r.TotalMinutes));
        int h = mins / 60, m = mins % 60;
        if (Ar) return h > 0 ? $"بعد {h} س {m} د" : $"بعد {m} د";
        return h > 0 ? $"in {h}h {m:00}m" : $"in {m}m";
    }

    static readonly string[] HijriEn =
        ["Muharram", "Safar", "Rabi' al-Awwal", "Rabi' al-Thani", "Jumada al-Ula", "Jumada al-Akhirah",
         "Rajab", "Sha'ban", "Ramadan", "Shawwal", "Dhu al-Qi'dah", "Dhu al-Hijjah"];
    static readonly string[] HijriAr =
        ["محرم", "صفر", "ربيع الأول", "ربيع الآخر", "جمادى الأولى", "جمادى الآخرة",
         "رجب", "شعبان", "رمضان", "شوال", "ذو القعدة", "ذو الحجة"];

    public static string Hijri(DateTime d, int adjustDays)
    {
        var cal = new UmAlQuraCalendar();
        var x = d.Date.AddDays(adjustDays);
        if (x < cal.MinSupportedDateTime || x > cal.MaxSupportedDateTime) return "";
        int y = cal.GetYear(x), m = cal.GetMonth(x), day = cal.GetDayOfMonth(x);
        return Ar ? $"{day} {HijriAr[m - 1]} {y} هـ" : $"{day} {HijriEn[m - 1]} {y} AH";
    }

    public static string Gregorian(DateTime d) => d.ToString("dddd، d MMMM yyyy", Culture).Replace("،", Ar ? "،" : ",");
}
