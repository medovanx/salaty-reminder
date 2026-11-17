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
        ["CheckingUpdates"] = ("Checking for updates…", "جارٍ البحث عن تحديثات…"),
        ["UpdateDownloading"] = ("Downloading {0}: {1}", "جارٍ تنزيل {0}: {1}"),
        ["UpdateVerifying"] = ("Checking the download…", "جارٍ التحقق من التنزيل…"),
        ["UpdateRestarting"] = ("Restarting into {0}…", "جارٍ إعادة التشغيل إلى {0}…"),
        ["WidgetChecking"] = ("Checking updates", "جارٍ التحقق"),
        ["WidgetUpdating"] = ("Updating {0}", "جارٍ التحديث {0}"),
        ["WidgetInstalling"] = ("Updating", "جارٍ التحديث"),
        ["GitHubRepo"] = ("GitHub page", "صفحة GitHub"),
        ["ReleaseNotes"] = ("Release notes", "ملاحظات الإصدار"),
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
        ["Searching"] = ("Searching", "جارٍ البحث"),
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
        // Ramadan
        ["Iftar"] = ("Iftar", "الإفطار"),
        ["SuhoorEnds"] = ("Suhoor ends", "نهاية السحور"),
        ["Imsak"] = ("Imsak {0}", "الإمساك {0}"),
        ["RamadanDay"] = ("Ramadan, day {0}", "رمضان، اليوم {0}"),
        ["FastingDay"] = ("Fasting", "الصيام"),
        ["IftarTitle"] = ("Iftar time: Maghrib", "حان موعد الإفطار: المغرب"),
        ["SuhoorTitle"] = ("Suhoor: Fajr in {0} minutes", "السحور: الفجر بعد {0} دقيقة"),
        ["SuhoorBody"] = ("Imsak at {0}, Fajr at {1}", "الإمساك {0}، الفجر {1}"),
        // Night
        // The middle of the night from Maghrib to Fajr, not 12:00.
        ["Midnight"] = ("Middle of the night {0}", "منتصف الليل الشرعي {0}"),
        ["LastThird"] = ("Last third {0}", "الثلث الأخير {0}"),
        ["MiddleOfNight"] = ("Middle of the night", "منتصف الليل الشرعي"),
        ["LastThirdOfNight"] = ("Last third of the night", "الثلث الأخير من الليل"),
        ["ImsakLabel"] = ("Imsak", "الإمساك"),
        // Friday and adhkar
        ["KahfTitle"] = ("It's Friday: read Surat Al-Kahf", "اليوم الجمعة: اقرأ سورة الكهف"),
        ["KahfBody"] = ("And send plenty of salawat on the Prophet ﷺ.", "وأكثر من الصلاة على النبي ﷺ."),
        ["JumuahTitle"] = ("Jumu'ah in {0} minutes", "صلاة الجمعة بعد {0} دقيقة"),
        ["JumuahBody"] = ("Go early. Dhuhr at {0}.", "بكّر إليها. الظهر {0}."),
        ["MorningAdhkar"] = ("Morning adhkar", "أذكار الصباح"),
        ["EveningAdhkar"] = ("Evening adhkar", "أذكار المساء"),
        ["AdhkarBody"] = ("A few minutes of remembrance.", "دقائق من ذكر الله."),
        ["FastTomorrowTitle"] = ("Tomorrow: {0}", "غدًا: {0}"),
        ["FastTomorrowBody"] = ("A sunnah fast. Intend it tonight and have suhoor.", "صيام سنة. انوِه الليلة وتسحّر."),
        // Settings page
        ["Extras"] = ("Extras", "إضافات"),
        ["ExtrasTitle"] = ("Ramadan and sunnah", "رمضان والسنن"),
        ["RamadanMode"] = ("Ramadan mode: Iftar and Suhoor countdowns", "وضع رمضان: العد التنازلي للإفطار والسحور"),
        ["ShowOccasions"] = ("Occasions and fasting days on the calendar", "المناسبات وأيام الصيام في التقويم"),
        ["FastingReminders"] = ("Remind me the evening before a sunnah fast", "تذكيري مساء اليوم السابق لصيام السنة"),
        ["FridayReminders"] = ("Friday: Surat Al-Kahf and Jumu'ah reminders", "الجمعة: تذكير بسورة الكهف وصلاة الجمعة"),
        ["AdhkarReminders"] = ("Morning and evening adhkar reminders", "تذكير بأذكار الصباح والمساء"),
        ["ShowNightTimes"] = ("Show the middle and last third of the night", "عرض منتصف الليل الشرعي والثلث الأخير"),
        // Occasions
        ["IslamicNewYear"] = ("Islamic New Year", "رأس السنة الهجرية"),
        ["Tasua"] = ("Tasu'a", "تاسوعاء"),
        ["Ashura"] = ("Ashura", "عاشوراء"),
        ["RamadanStart"] = ("First day of Ramadan", "أول أيام رمضان"),
        ["LastTenNights"] = ("Last ten nights", "العشر الأواخر"),
        ["EidAlFitr"] = ("Eid al-Fitr", "عيد الفطر"),
        ["FirstTenDhulHijjah"] = ("First ten days of Dhu al-Hijjah", "العشر الأوائل من ذي الحجة"),
        ["Arafah"] = ("Day of Arafah", "يوم عرفة"),
        ["EidAlAdha"] = ("Eid al-Adha", "عيد الأضحى"),
        ["Tashreeq"] = ("Days of Tashreeq", "أيام التشريق"),
        ["FastMonday"] = ("Monday fast", "صيام الاثنين"),
        ["FastThursday"] = ("Thursday fast", "صيام الخميس"),
        ["FastWhiteDays"] = ("White days fast", "صيام الأيام البيض"),
        ["FastAshura"] = ("Fast of Tasu'a and Ashura", "صيام تاسوعاء وعاشوراء"),
        ["FastArafah"] = ("Fast of Arafah", "صيام يوم عرفة"),
        ["FastSixOfShawwal"] = ("Six days of Shawwal", "صيام ست من شوال"),
    };

    public static string T(string key) => D.TryGetValue(key, out var v) ? (Ar ? v.ar : v.en) : key;
    public static string F(string key, params object[] args) => string.Format(T(key), args);

    /// <summary>Keeps numbers and units in left-to-right order inside Arabic text.</summary>
    public static string Ltr(string s) => "\u200E" + s + "\u200E";

    /// <summary>"512 B", "1.5 KB", "32.8 MB" in binary units.</summary>
    public static string Size(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        double v = Math.Max(0, bytes);
        int u = 0;
        while (v >= 1024 && u < units.Length - 1) { v /= 1024; u++; }
        return u == 0 ? $"{bytes} B" : $"{v.ToString(v < 10 ? "0.##" : v < 100 ? "0.#" : "0", CultureInfo.InvariantCulture)} {units[u]}";
    }

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

    /// <summary>The occasion's name, or the fast's when the day only has a fast; empty for an ordinary day.</summary>
    public static string DayLabel(DayInfo info, DayOfWeek dow) =>
        info.Occasion is { } o ? T(o.ToString()) : FastLabel(info.Fast, dow);

    public static string FastLabel(FastKind f, DayOfWeek dow) => f switch
    {
        FastKind.MondayThursday => T(dow == DayOfWeek.Monday ? "FastMonday" : "FastThursday"),
        FastKind.None => "",
        _ => T("Fast" + f),
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

    /// <summary>"9 Dhu al-Hijjah", without the year, for a line that also names the day's occasion.</summary>
    public static string HijriShort(DateTime d, int adjustDays)
    {
        if (HijriDate.From(DateOnly.FromDateTime(d), adjustDays) is not { } h) return "";
        return $"{h.Day} {(Ar ? HijriAr : HijriEn)[h.Month - 1]}";
    }

    public static string Gregorian(DateTime d) => d.ToString("dddd، d MMMM yyyy", Culture).Replace("،", Ar ? "،" : ",");
}
