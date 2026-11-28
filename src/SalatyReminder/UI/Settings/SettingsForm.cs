using System.Drawing.Drawing2D;
using System.Globalization;

namespace SalatyReminder.UI.Settings;

/// <summary>Settings in the Windows 11 Settings style: a sidebar of pages, each a rounded card.</summary>
sealed class SettingsForm : Form
{
    /// <summary>The page the card's "Manage cities" opens.</summary>
    public const int CitiesPage = 2;

    readonly SalatyApp app;
    readonly AppSettings s;
    readonly Theme t = Theme.Current;

    // General
    readonly ThemedCombo cmbLang = Combo(), cmbClock = Combo(), cmbSide = Combo();
    readonly ThemedNumber numOffset = Num(-2000, 2000, 0);
    readonly ToggleSwitch tglStartup = new(), tglSunnah = new();
    // Location
    readonly Segmented segLocation = new(L.T("Auto"), L.T("Manual")) { Width = 300 };
    readonly ThemedText txtSearch = new() { Width = 300 }, txtCity = new() { Width = 300 };
    readonly ThemedButton btnSearch = new() { AutoSize = true, Height = 27 };
    readonly ThemedCombo cmbResults = Combo(400);
    readonly ThemedNumber numLat = Num(-90, 90, 4), numLon = Num(-180, 180, 4);
    readonly Panel manualPanel = new() { AutoSize = true, Margin = Padding.Empty };
    // My cities
    readonly ThemedText txtCitySearch = new() { Width = 300 };
    readonly ThemedButton btnCitySearch = new() { AutoSize = true, Height = 27 }, btnAddCity = new() { AutoSize = true, Height = 27 };
    readonly ThemedCombo cmbCityResults = Combo(300);
    readonly FlowLayoutPanel citiesPanel = new() { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, Margin = Padding.Empty };
    readonly Label lblNoCities = new() { AutoSize = true, MaximumSize = new Size(490, 0), Tag = "dim", Margin = new Padding(0, 0, 0, 6) };
    List<LocResult> cityResults = [];
    Control? cityResultsRow;
    // Calculation
    readonly ThemedCombo cmbMethod = Combo(300), cmbAsr = Combo(300);
    readonly ThemedNumber numHijri = Num(-2, 2, 0);
    // Reminders
    readonly ToggleSwitch tglNotify = new(), tglEarly = new(), tglFlash = new(), tglAdhan = new();
    readonly ThemedNumber numEarly = Num(1, 120, 0);
    readonly ThemedText txtAdhan = new() { Width = 260 };
    readonly ThemedButton btnBrowse = new() { AutoSize = true, Height = 27 }, btnTest = new() { AutoSize = true, Height = 27 };
    // Ramadan and sunnah
    readonly ToggleSwitch tglRamadan = new(), tglOccasions = new(), tglFasting = new(), tglFriday = new(), tglAdhkar = new(), tglNight = new();
    // Updates
    readonly ToggleSwitch tglAutoCheck = new(), tglAutoInstall = new();
    readonly ThemedButton btnCheck = new() { AutoSize = true, Height = 30 };
    /// <summary>Takes the Check now button's place while an update is checked for or installed.</summary>
    readonly ProgressLine updateBar = new() { Width = 200, Height = 30, Visible = false };
    readonly IconButton btnGitHub = new() { Width = 150, Height = 32, Glyph = Art.IconOpen }, btnNotes = new() { Width = 150, Height = 32, Glyph = Art.IconDocument };
    /// <summary>Fixed to the button height and centred, so the status lines up with Check now and the progress bar.</summary>
    readonly Label lblUpdate = new() { AutoSize = false, Size = new Size(400, 30), TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true, Tag = "dim" };

    readonly ThemedButton btnSave = new() { Size = new Size(110, 34) }, btnCancel = new() { Size = new Size(110, 34) };
    readonly List<(string glyph, string title, Control page)> pages = [];
    readonly NavList nav;
    readonly Panel content = new() { Dock = DockStyle.Fill, Padding = new Padding(20, 20, 20, 0) };
    List<LocResult> results = [];
    Control? resultsRow;

    public SettingsForm(SalatyApp app)
    {
        this.app = app;
        s = app.S.Clone();
        Text = $"{L.T("Settings")} - {L.T("AppName")}";
        Icon = app.AppIcon;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleDimensions = new SizeF(96f, 96f);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(780, 580);
        Font = new Font("Segoe UI", 9.75f);
        BackColor = t.Window;
        ForeColor = t.Text;
        if (L.Ar) RightToLeft = RightToLeft.Yes;

        BuildGeneral();
        BuildLocation();
        BuildCities();
        BuildCalculation();
        BuildReminders();
        BuildExtras();
        BuildUpdates();

        nav = new NavList(pages.Select(p => (p.glyph, p.title)).ToArray(), t) { Dock = L.Ar ? DockStyle.Right : DockStyle.Left, Width = 210 };
        nav.SelectedChanged += ShowPage;

        var footer = new Panel { Dock = DockStyle.Bottom, Height = 62, Padding = new Padding(20, 12, 20, 16) };
        btnSave.Text = L.T("Save");
        btnCancel.Text = L.T("Cancel");
        btnSave.Click += (_, _) => Save();
        btnCancel.Click += (_, _) => Close();
        AcceptButton = btnSave;
        CancelButton = btnCancel;
        var buttons = new FlowLayoutPanel { Dock = L.Ar ? DockStyle.Left : DockStyle.Right, AutoSize = true, FlowDirection = L.Ar ? FlowDirection.LeftToRight : FlowDirection.RightToLeft, WrapContents = false };
        buttons.Controls.AddRange([btnSave, btnCancel]);
        footer.Controls.Add(buttons);

        foreach (var p in pages) { p.page.Visible = false; content.Controls.Add(p.page); }
        Controls.Add(content);
        Controls.Add(footer);
        Controls.Add(nav);

        Styler.Apply(this, t);
        Styler.Primary(btnSave, t);
        ShowPage(0);
        SyncEnabled();
        FormClosed += (_, _) => app.StopAdhan();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Native.RoundCorners(Handle, t.Dark);
        int caption = ColorTranslator.ToWin32(t.Window);
        Native.DwmSetWindowAttribute(Handle, 35, ref caption, sizeof(int)); // DWMWA_CAPTION_COLOR
    }

    public int PageCount => pages.Count;
    public void ShowPageForShot(int i) => ShowPage(i);
    public void ShowUpdatesForShot() => ShowPage(pages.Count - 1);

    void ShowPage(int i)
    {
        for (int k = 0; k < pages.Count; k++) pages[k].page.Visible = k == i;
        nav.Selected = i;
    }

    // ---------- pages ----------

    Control AddPage(string glyph, string title, params Control[] sections)
    {
        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = false };
        var heading = new Label { Text = title, AutoSize = true, Font = new Font("Segoe UI Semibold", 17f), Margin = new Padding(2, 0, 0, 12) };
        flow.Controls.Add(heading);
        foreach (var sec in sections) { sec.Width = 526; sec.Margin = new Padding(0, 0, 0, 12); flow.Controls.Add(sec); }
        pages.Add((glyph, title, flow));
        return flow;
    }

    void BuildGeneral()
    {
        cmbLang.Items.AddRange(["English", "العربية"]);
        cmbLang.SelectedIndex = s.Language == "ar" ? 1 : 0;
        cmbClock.Items.AddRange([L.T("H12"), L.T("H24")]);
        cmbClock.SelectedIndex = s.Use24h ? 1 : 0;
        tglStartup.Checked = AppSettings.StartWithWindows;
        tglSunnah.Checked = s.ShowSunnah;
        cmbSide.Items.AddRange([L.T("SideRight"), L.T("SideLeft")]);
        cmbSide.SelectedIndex = s.WidgetSide == "Left" ? 1 : 0;
        numOffset.Value = Math.Clamp(s.WidgetOffset, -2000, 2000);
        numOffset.Increment = 10;
        AddPage("\uE713", L.T("General"), Card(L.T("General"), "\uE713",
            Row(L.T("Language"), cmbLang),
            Row(L.T("TimeFormat"), cmbClock),
            Row(L.T("WidgetPosition"), cmbSide),
            Row(L.T("WidgetOffset"), numOffset),
            Row(L.T("ShowSunnah"), tglSunnah),
            Row(L.T("StartWithWindows"), tglStartup)));
    }

    void BuildLocation()
    {
        segLocation.Selected = s.AutoLocation ? 0 : 1;
        segLocation.SelectedChanged += (_, _) => SyncEnabled();
        btnSearch.Text = L.T("Search");
        numLat.Value = (decimal)Math.Round(s.Latitude, 4);
        numLon.Value = (decimal)Math.Round(s.Longitude, 4);
        txtCity.Text = s.LocationName;
        txtSearch.PlaceholderText = L.T("City") + "…";

        var note = new Label { Text = L.T("AutoNote"), AutoSize = true, MaximumSize = new Size(490, 0), Tag = "dim", Margin = new Padding(0, 6, 0, 4) };
        var searchRow = Flow(txtSearch, btnSearch);
        var grid = Grid(
            Row(L.T("Search"), searchRow),
            resultsRow = Row("", cmbResults),
            Row(L.T("City"), txtCity),
            Row(L.T("Latitude"), numLat),
            Row(L.T("Longitude"), numLon));
        manualPanel.Controls.Add(grid);
        resultsRow!.Visible = false;
        manualPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;

        var current = new Label { Text = "📍 " + s.LocationName, AutoSize = true, Margin = new Padding(0, 0, 0, 8) };
        AddPage("\uE707", L.T("Location"), Card(L.T("Location"), "\uE707", current, segLocation, note, manualPanel));

        btnSearch.Click += async (_, _) => await SearchAsync();
        txtSearch.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; await SearchAsync(); } };
        cmbResults.SelectedIndexChanged += (_, _) =>
        {
            if (cmbResults.SelectedIndex < 0 || cmbResults.SelectedIndex >= results.Count) return;
            var r = results[cmbResults.SelectedIndex];
            txtCity.Text = r.Name;
            numLat.Value = (decimal)Math.Round(r.Lat, 4);
            numLon.Value = (decimal)Math.Round(r.Lon, 4);
        };
    }

    void BuildCities()
    {
        btnCitySearch.Text = L.T("Search");
        btnAddCity.Text = L.T("AddCity");
        txtCitySearch.PlaceholderText = L.T("City") + "…";
        lblNoCities.Text = L.T("NoCities");
        var note = new Label { Text = L.T("CitiesNote"), AutoSize = true, MaximumSize = new Size(490, 0), Tag = "dim", Margin = new Padding(0, 6, 0, 4) };
        cityResultsRow = Flow(cmbCityResults, btnAddCity);
        cityResultsRow.Visible = false;
        FillCities();
        AddPage("", L.T("MyCities"), Card(L.T("MyCities"), "",
            lblNoCities, citiesPanel, Flow(txtCitySearch, btnCitySearch), cityResultsRow, note));

        btnCitySearch.Click += async (_, _) => await SearchCitiesAsync();
        txtCitySearch.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; await SearchCitiesAsync(); } };
        btnAddCity.Click += (_, _) =>
        {
            int i = cmbCityResults.SelectedIndex;
            if (i < 0 || i >= cityResults.Count) return;
            var r = cityResults[i];
            // Nearby duplicates (the same city picked twice) are skipped.
            if (!s.Cities.Any(c => Math.Abs(c.Latitude - r.Lat) < 0.01 && Math.Abs(c.Longitude - r.Lon) < 0.01))
                s.Cities.Add(new SavedCity(r.Name, r.Lat, r.Lon, r.TimeZone));
            cityResultsRow.Visible = false;
            txtCitySearch.Text = "";
            FillCities();
        };
    }

    /// <summary>One row per saved city, each with a Remove button.</summary>
    void FillCities()
    {
        citiesPanel.SuspendLayout();
        foreach (Control c in citiesPanel.Controls.Cast<Control>().ToArray()) c.Dispose();
        for (int i = 0; i < s.Cities.Count; i++)
        {
            int index = i;
            var remove = new ThemedButton { Text = L.T("Remove"), AutoSize = true, Height = 27 };
            remove.Click += (_, _) =>
            {
                s.Cities.RemoveAt(index);
                // The card keeps showing the same city, or goes back to the default if that one was removed.
                s.ActiveCity = s.ActiveCity == index ? -1 : s.ActiveCity > index ? s.ActiveCity - 1 : s.ActiveCity;
                BeginInvoke(FillCities); // not while this button's own click is running
            };
            var row = Row(s.Cities[i].Name, remove);
            Styler.Apply(row, t);
            citiesPanel.Controls.Add(row);
        }
        citiesPanel.ResumeLayout();
        lblNoCities.Visible = s.Cities.Count == 0;
    }

    async Task SearchCitiesAsync()
    {
        if (string.IsNullOrWhiteSpace(txtCitySearch.Text)) return;
        btnCitySearch.Text = L.T("Searching");
        btnCitySearch.Busy = true;
        try { cityResults = await LocationService.SearchAsync(txtCitySearch.Text.Trim(), cmbLang.SelectedIndex == 1 ? "ar" : "en"); }
        finally { btnCitySearch.Busy = false; btnCitySearch.Text = L.T("Search"); }
        cmbCityResults.Items.Clear();
        cityResultsRow!.Visible = true;
        btnAddCity.Enabled = cityResults.Count > 0;
        if (cityResults.Count == 0) { cmbCityResults.Items.Add(L.T("NoResults")); cmbCityResults.SelectedIndex = 0; return; }
        foreach (var r in cityResults) cmbCityResults.Items.Add(r.Name);
        cmbCityResults.SelectedIndex = 0;
        cmbCityResults.DroppedDown = cityResults.Count > 1;
    }

    void BuildCalculation()
    {
        foreach (var m in Methods.All) cmbMethod.Items.Add(L.MethodName(m));
        cmbMethod.SelectedIndex = Math.Max(0, Array.FindIndex(Methods.All, m => m.Key == s.Method));
        cmbAsr.Items.AddRange([L.T("AsrStd"), L.T("AsrHanafi")]);
        cmbAsr.SelectedIndex = s.HanafiAsr ? 1 : 0;
        numHijri.Value = s.HijriAdjust;
        AddPage("\uE8EF", L.T("Calculation"), Card(L.T("Calculation"), "\uE8EF",
            Stacked(L.T("Method"), cmbMethod),
            Stacked(L.T("AsrMethod"), cmbAsr),
            Row(L.T("HijriAdjust"), numHijri)));
    }

    void BuildReminders()
    {
        tglNotify.Checked = s.NotifyAtTime;
        tglEarly.Checked = s.EarlyNotify;
        numEarly.Value = Math.Clamp(s.EarlyMinutes, 1, 120);
        tglFlash.Checked = s.FlashWidget;
        tglAdhan.Checked = s.PlayAdhan;
        txtAdhan.Text = s.AdhanFile;
        txtAdhan.PlaceholderText = File.Exists(SalatyApp.BundledAdhanPath) || SalatyApp.HasBundledAdhan ? L.T("BuiltInAdhan") : L.T("AdhanFile");
        btnBrowse.Text = L.T("Browse");
        btnTest.Text = L.T("Test");

        AddPage("\uEA8F", L.T("Reminders"), Card(L.T("Reminders"), "\uEA8F",
            Row(L.T("NotifyAtTime"), tglNotify),
            Row(L.T("EarlyNotify"), Flow(numEarly, tglEarly)),
            Row(L.T("Flash"), tglFlash),
            Row(L.T("PlayAdhan"), tglAdhan),
            Row("", Flow(txtAdhan, btnBrowse, btnTest))));

        tglAdhan.CheckedChanged += (_, _) => SyncEnabled();
        tglEarly.CheckedChanged += (_, _) => SyncEnabled();
        btnBrowse.Click += (_, _) =>
        {
            using var dlg = new OpenFileDialog { Filter = "Audio|*.mp3;*.wav;*.m4a;*.wma;*.aac;*.ogg|*.*|*.*" };
            if (File.Exists(txtAdhan.Text)) dlg.InitialDirectory = Path.GetDirectoryName(txtAdhan.Text);
            if (dlg.ShowDialog(this) == DialogResult.OK) txtAdhan.Text = dlg.FileName;
        };
        btnTest.Click += (_, _) =>
        {
            if (app.AdhanPlaying) { app.StopAdhan(); btnTest.Text = L.T("Test"); return; }
            if (!app.PlayAdhan(txtAdhan.Text.Trim()))
            {
                MessageBox.Show(this, L.T("AdhanMissing"), Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            btnTest.Text = L.T("Stop");
        };
    }

    void BuildExtras()
    {
        tglRamadan.Checked = s.RamadanMode;
        tglOccasions.Checked = s.ShowOccasions;
        tglFasting.Checked = s.FastingReminders;
        tglFriday.Checked = s.FridayReminders;
        tglAdhkar.Checked = s.AdhkarReminders;
        tglNight.Checked = s.ShowNightTimes;
        AddPage("", L.T("Extras"), Card(L.T("ExtrasTitle"), "",
            Row(L.T("RamadanMode"), tglRamadan),
            Row(L.T("ShowOccasions"), tglOccasions),
            Row(L.T("FastingReminders"), tglFasting),
            Row(L.T("FridayReminders"), tglFriday),
            Row(L.T("AdhkarReminders"), tglAdhkar),
            Row(L.T("ShowNightTimes"), tglNight)));
    }

    void BuildUpdates()
    {
        tglAutoCheck.Checked = s.AutoCheckUpdates;
        tglAutoInstall.Checked = s.AutoInstallUpdates;
        btnCheck.Text = L.T("CheckNow");
        var version = new Label { Text = L.F("Version", Updater.CurrentText), AutoSize = true, Font = new Font("Segoe UI Semibold", 11f), Margin = new Padding(0, 0, 0, 8) };
        AddPage("\uE895", L.T("Updates"), Card(L.T("Updates"), "\uE895",
            version,
            Row(L.T("AutoCheck"), tglAutoCheck),
            Row(L.T("AutoInstall"), tglAutoInstall),
            Flow(btnCheck, updateBar, lblUpdate),
            Flow(btnGitHub, btnNotes)));
        btnGitHub.Text = L.T("GitHubRepo");
        btnNotes.Text = L.T("ReleaseNotes");
        btnGitHub.Click += (_, _) => Updater.OpenPage(Updater.RepoPage);
        btnNotes.Click += (_, _) => Updater.OpenPage(Updater.ReleasesPage);
        btnGitHub.Theme = btnNotes.Theme = updateBar.Theme = t;
        tglAutoCheck.CheckedChanged += (_, _) => SyncEnabled();
        btnCheck.Click += async (_, _) =>
        {
            string msg = await app.CheckForUpdatesAsync(userInitiated: true);
            if (!app.UpdateBusy) lblUpdate.Text = msg;
        };
        app.UpdateChanged += SyncUpdate;
        FormClosed += (_, _) => app.UpdateChanged -= SyncUpdate;
        if (app.UpdateBusy) SyncUpdate();
    }

    /// <summary>While busy, a progress bar stands where Check now was and the line beside it says what is happening.</summary>
    void SyncUpdate()
    {
        if (IsDisposed) return;
        updateBar.Visible = app.UpdateBusy;
        btnCheck.Visible = !app.UpdateBusy;
        updateBar.Value = app.UpdateFraction;
        if (app.UpdateStatus.Length > 0) lblUpdate.Text = app.UpdateStatus;
    }

    void SyncEnabled()
    {
        bool manual = segLocation.Selected == 1;
        manualPanel.Visible = manual;
        numEarly.Enabled = tglEarly.Checked;
        txtAdhan.Enabled = btnBrowse.Enabled = tglAdhan.Checked;
        tglAutoInstall.Enabled = tglAutoCheck.Checked;
    }

    async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(txtSearch.Text)) return;
        btnSearch.Text = L.T("Searching");
        btnSearch.Busy = true;
        try { results = await LocationService.SearchAsync(txtSearch.Text.Trim(), cmbLang.SelectedIndex == 1 ? "ar" : "en"); }
        finally { btnSearch.Busy = false; btnSearch.Text = L.T("Search"); }
        cmbResults.Items.Clear();
        resultsRow!.Visible = true;
        if (results.Count == 0) { cmbResults.Items.Add(L.T("NoResults")); cmbResults.SelectedIndex = 0; return; }
        foreach (var r in results) cmbResults.Items.Add(r.Name);
        cmbResults.SelectedIndex = 0;
        cmbResults.DroppedDown = results.Count > 1;
    }

    void Save()
    {
        string oldLang = s.Language;
        s.Language = cmbLang.SelectedIndex == 1 ? "ar" : "en";
        s.Use24h = cmbClock.SelectedIndex == 1;
        bool wasAuto = s.AutoLocation;
        s.AutoLocation = segLocation.Selected == 0;
        if (!s.AutoLocation)
        {
            s.Latitude = (double)numLat.Value;
            s.Longitude = (double)numLon.Value;
            s.LocationName = string.IsNullOrWhiteSpace(txtCity.Text)
                ? string.Create(CultureInfo.InvariantCulture, $"{s.Latitude:F3}, {s.Longitude:F3}")
                : txtCity.Text.Trim();
            s.LocationUpdatedUtc = DateTime.UtcNow;
        }
        s.Method = Methods.All[Math.Max(0, cmbMethod.SelectedIndex)].Key;
        s.HanafiAsr = cmbAsr.SelectedIndex == 1;
        s.HijriAdjust = (int)numHijri.Value;
        s.NotifyAtTime = tglNotify.Checked;
        s.EarlyNotify = tglEarly.Checked;
        s.EarlyMinutes = (int)numEarly.Value;
        s.FlashWidget = tglFlash.Checked;
        s.PlayAdhan = tglAdhan.Checked;
        s.AdhanFile = txtAdhan.Text.Trim();
        s.ShowSunnah = tglSunnah.Checked;
        s.WidgetSide = cmbSide.SelectedIndex == 1 ? "Left" : "Right";
        s.WidgetOffset = (int)numOffset.Value;
        s.RamadanMode = tglRamadan.Checked;
        s.ShowOccasions = tglOccasions.Checked;
        s.FastingReminders = tglFasting.Checked;
        s.FridayReminders = tglFriday.Checked;
        s.AdhkarReminders = tglAdhkar.Checked;
        s.ShowNightTimes = tglNight.Checked;
        s.AutoCheckUpdates = tglAutoCheck.Checked;
        s.AutoInstallUpdates = tglAutoInstall.Checked;
        try { AppSettings.StartWithWindows = tglStartup.Checked; } catch { /* registry blocked */ }

        bool relocate = s.AutoLocation && (!wasAuto || oldLang != s.Language);
        app.ApplySettings(s, relocate);
        Close();
    }

    // ---------- layout helpers ----------

    static ThemedCombo Combo(int width = 220) => new() { Width = width };
    static ThemedNumber Num(int min, int max, int dec) => new() { Minimum = min, Maximum = max, DecimalPlaces = dec, Width = dec > 0 ? 150 : 120, Increment = dec > 0 ? 0.01m : 1 };

    Section Card(string title, string glyph, params Control[] rows)
    {
        var sec = new Section(title) { Glyph = glyph, Theme = t, RightToLeft = RightToLeft };
        sec.MinimumSize = new Size(526, 0);
        sec.MaximumSize = new Size(526, 0);
        var stack = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, Location = new Point(16, 44), MinimumSize = new Size(494, 0), MaximumSize = new Size(494, 0), RightToLeft = RightToLeft };
        foreach (var r in rows) stack.Controls.Add(r);
        sec.Controls.Add(stack);
        return sec;
    }

    /// <summary>Label on the near side, control on the far side.</summary>
    static TableLayoutPanel Row(string label, Control c)
    {
        var tl = new TableLayoutPanel { ColumnCount = 2, RowCount = 1, Width = 494, Height = Math.Max(36, (c.AutoSize ? c.PreferredSize.Height : c.Height) + 8), Margin = new Padding(0) };
        tl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        tl.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tl.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, MaximumSize = new Size(300, 0) }, 0, 0);
        c.Anchor = AnchorStyles.Right;
        c.Margin = new Padding(0, c.Margin.Top, 0, c.Margin.Bottom); // same edge for every control, in both directions
        tl.Controls.Add(c, 1, 0);
        return tl;
    }

    /// <summary>Label above a wide control.</summary>
    static FlowLayoutPanel Stacked(string label, Control c)
    {
        var f = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false, Margin = new Padding(0, 0, 0, 8) };
        f.Controls.Add(new Label { Text = label, AutoSize = true, Margin = new Padding(0, 4, 0, 4) });
        c.Width = 490;
        f.Controls.Add(c);
        return f;
    }

    static FlowLayoutPanel Grid(params Control[] rows)
    {
        var stack = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, Margin = new Padding(0) };
        foreach (var r in rows) stack.Controls.Add(r);
        return stack;
    }

    static FlowLayoutPanel Flow(params Control[] cs)
    {
        // Children centred vertically; spacing only between them, so the last one lines up with
        // single controls (toggles, dropdowns) in other rows.
        var p = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0, 4, 0, 4) };
        int tallest = cs.Max(c => c.Height);
        for (int i = 0; i < cs.Length; i++)
        {
            int top = (tallest - cs[i].Height) / 2;
            cs[i].Margin = new Padding(0, top, i < cs.Length - 1 ? 8 : 0, 0);
            p.Controls.Add(cs[i]);
        }
        return p;
    }

    /// <summary>Sidebar: brand header and page list with a teal selection pill.</summary>
    sealed class NavList : Control
    {
        readonly (string glyph, string title)[] items;
        readonly Theme t;
        int selected, hover = -1;
        public event Action<int>? SelectedChanged;

        public NavList((string, string)[] items, Theme t)
        {
            this.items = items;
            this.t = t;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;
        }

        public int Selected { get => selected; set { selected = value; Invalidate(); } }

        float S => DeviceDpi / 96f;
        Rectangle Item(int i) => new((int)(10 * S), (int)((112 + i * 44) * S), Width - (int)(20 * S), (int)(40 * S));

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int h = Enumerable.Range(0, items.Length).FirstOrDefault(i => Item(i).Contains(e.Location), -1);
            if (h != hover) { hover = h; Invalidate(); }
        }

        protected override void OnMouseLeave(EventArgs e) { hover = -1; Invalidate(); }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            for (int i = 0; i < items.Length; i++)
                if (Item(i).Contains(e.Location)) { selected = i; Invalidate(); SelectedChanged?.Invoke(i); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            bool rtl = RightToLeft == RightToLeft.Yes;

            // Brand header: teal gradient with the star pattern.
            var head = new Rectangle(0, 0, Width, (int)(92 * S));
            using (var grad = new LinearGradientBrush(head, Art.TealLight, Art.TealDark, LinearGradientMode.ForwardDiagonal))
                g.FillRectangle(grad, head);
            Art.Pattern(g, head, 30 * S, Color.FromArgb(24, 255, 255, 255));
            using (var below = new SolidBrush(t.Dark ? Color.FromArgb(26, 28, 28) : Color.FromArgb(236, 239, 238)))
                g.FillRectangle(below, 0, head.Bottom, Width, Height - head.Bottom);

            int gs = (int)(34 * S), gx = rtl ? Width - (int)(16 * S) - gs : (int)(16 * S);
            Art.Crescent(g, new RectangleF(gx, (head.Height - gs) / 2f, gs, gs), Art.Gold);
            var textRect = rtl ? new Rectangle((int)(10 * S), 0, gx - (int)(20 * S), head.Height)
                               : new Rectangle(gx + gs + (int)(10 * S), 0, Width - gx - gs - (int)(20 * S), head.Height);
            var tf = TextFormatFlags.VerticalCenter | (rtl ? TextFormatFlags.Right | TextFormatFlags.RightToLeft : TextFormatFlags.Left);
            using var fName = new Font("Segoe UI Semibold", 12f);
            using var fVer = new Font("Segoe UI", 8.5f);
            TextRenderer.DrawText(g, L.T("AppName"), fName, new Rectangle(textRect.X, textRect.Y - (int)(9 * S), textRect.Width, textRect.Height), Color.White, tf);
            TextRenderer.DrawText(g, "v" + Updater.CurrentText, fVer, new Rectangle(textRect.X, textRect.Y + (int)(12 * S), textRect.Width, textRect.Height), Color.FromArgb(215, 235, 230), tf);

            using var fIcon = new Font(Art.IconFontName, 11f);
            using var fItem = new Font("Segoe UI", 10f);
            using var fItemB = new Font("Segoe UI Semibold", 10f);
            for (int i = 0; i < items.Length; i++)
            {
                var r = Item(i);
                if (i == selected || i == hover)
                {
                    using var p = Art.RoundRect(r, 6 * S);
                    using var b = new SolidBrush(i == selected ? Color.FromArgb(t.Dark ? 44 : 30, t.Accent) : Color.FromArgb(t.Dark ? 22 : 14, t.Text));
                    g.FillPath(b, p);
                }
                if (i == selected)
                {
                    var bar = new Rectangle(rtl ? r.Right - (int)(3 * S) : r.Left, r.Top + (int)(11 * S), (int)(3 * S), r.Height - (int)(22 * S));
                    using var bb = new SolidBrush(t.Accent);
                    g.FillRectangle(bb, bar);
                }
                int ix = rtl ? r.Right - (int)(40 * S) : r.Left + (int)(12 * S);
                var ir = new Rectangle(ix, r.Top, (int)(28 * S), r.Height);
                TextRenderer.DrawText(g, items[i].glyph, fIcon, ir, i == selected ? t.Accent : t.Dim, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                var lr = rtl ? new Rectangle(r.Left + (int)(8 * S), r.Top, r.Width - (int)(56 * S), r.Height)
                             : new Rectangle(r.Left + (int)(48 * S), r.Top, r.Width - (int)(56 * S), r.Height);
                TextRenderer.DrawText(g, items[i].title, i == selected ? fItemB : fItem, lr, t.Text, tf | TextFormatFlags.EndEllipsis);
            }
        }
    }
}
