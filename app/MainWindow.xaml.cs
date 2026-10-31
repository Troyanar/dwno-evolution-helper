using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;

namespace DwnoEvoHelper;

public partial class MainWindow : Window
{
    [DataContract]
    sealed class Settings
    {
        [DataMember] public double Left { get; set; } = double.NaN;
        [DataMember] public double Top { get; set; } = double.NaN;
        [DataMember] public double Width { get; set; } = 440;
        [DataMember] public double Height { get; set; } = 760;
        [DataMember] public bool Topmost { get; set; } = true;
        [DataMember] public double Opacity { get; set; } = 1;
        [DataMember] public string? Goal { get; set; }      // alt (v1.1): Ziel für Partner 1
        [DataMember] public string?[]? Goals { get; set; }  // Ziel je Partner
        [DataMember] public int Partner { get; set; }        // 0, 1 oder 2 = beide
        [DataMember] public string? GameDir { get; set; }
    }

    static readonly string SettingsPath = Path.Combine(GameFiles.Dir, "app-settings.json");
    static readonly double[] OpacitySteps = { 1, 0.85, 0.7, 0.55 };

    readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    Settings _settings = new();
    GameData? _data;
    DateTime _dataStamp;
    LiveState? _live;
    int _partner;
    string _lastRender = "";
    string? _gameDir;
    bool _setupForced, _setupBusy;

    public MainWindow()
    {
        InitializeComponent();
        LoadSettings();
        _gameDir = Setup.FindGameDir(_settings.GameDir);
        HideBepInExConsole();
        ApplyTexts();
        _timer.Tick += (_, _) => Refresh();
        Loaded += (_, _) =>
        {
            Refresh();
            // Beim ersten Start oder veraltetem Plugin direkt die Einrichtung zeigen
            if (NeedsSetup()) ShowSetup(true);
            _timer.Start();
        };
        Closing += (_, _) => SaveSettings();
        SourceInitialized += (_, _) => InitHotkeys();
        Loaded += async (_, _) =>
        {
            // Neuere Version auf GitHub?
            if (await Updates.CheckAsync() is Version v)
            {
                UpdateButton.Content = Loc.F("update.available", $"{v.Major}.{v.Minor}.{Math.Max(v.Build, 0)}");
                UpdateButton.Visibility = Visibility.Visible;
            }
        };
    }

    // ---------- Daten laden ----------

    void Refresh()
    {
        var stamp = File.Exists(GameFiles.GameDataPath) ? File.GetLastWriteTimeUtc(GameFiles.GameDataPath) : default;
        if (_data == null || stamp != _dataStamp)
        {
            var before = _data;
            _data = GameData.TryLoad(GameFiles.GameDataPath) ?? _data ?? GameData.LoadEmbedded();
            _dataStamp = stamp;
            if (_data != before)
            {
                // Neue Tabelle (z. B. andere Spielsprache) → Digidex neu aufbauen
                BuildStageFilters();
                RenderDexList();
            }
        }
        _live = LiveState.TryLoad(GameFiles.LivePath) ?? _live;
        if (_live != null) Timing.Observe(_live.World, _live.Time);
        if (_live != null && GameRunning() && GameFresh()) Trainer.Observe(_live, DateTime.Now);
        if (_logMode) RenderLog();

        // Sprache folgt dem Spiel
        if (Loc.Set(_live?.Lang ?? _data?.Lang)) ApplyTexts();
        Render();
    }

    bool GameRunning()
    {
        if (_live == null) return false;
        try { return !Process.GetProcessById(_live.Pid).HasExited; }
        catch { return false; }
    }

    /// Das Spiel hält im Hintergrund an – dann kommen keine neuen Werte, es läuft aber noch.
    bool GameFresh() => _live != null && DateTime.Now - _live.Time < TimeSpan.FromSeconds(6);

    /// Ältere Installationen: schwarzes BepInEx-Konsolenfenster nachträglich ausschalten
    /// (wirkt ab dem nächsten Spielstart).
    void HideBepInExConsole()
    {
        try
        {
            if (_gameDir != null && Setup.BepInExState(_gameDir) == PartState.Installed &&
                !Setup.ConsoleHidden(_gameDir) && Setup.CanWrite(_gameDir))
                Setup.HideConsole(_gameDir);
        }
        catch
        {
            // nicht kritisch
        }
    }

    bool NeedsSetup() =>
        _gameDir == null || Setup.BepInExState(_gameDir) != PartState.Installed || Setup.PluginState(_gameDir) != PartState.Installed;

    // ---------- Einrichtung ----------

    void ShowSetup(bool show)
    {
        _setupForced = show;
        SetupView.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        ApplyMode();
        if (show)
        {
            SetupMessage.Text = "";
            RenderSetup();
        }
    }

    void RenderSetup()
    {
        Chip Check(string label, string text, bool ok) => new()
        {
            Icon = ok ? "✓" : "✗",
            Label = label,
            Text = text,
            Foreground = (Brush)FindResource(ok ? "Good" : "Bad"),
            Background = (Brush)FindResource(ok ? "GoodBg" : "BadBg"),
        };
        string State(PartState s) => Loc.T(s switch
        {
            PartState.Installed => "setup.installed", PartState.Outdated => "setup.outdated", _ => "setup.missing",
        });

        var checks = new List<Chip> { Check(Loc.T("setup.game"), _gameDir != null ? "Digimon World Next Order" : Loc.T("setup.notfound"), _gameDir != null) };
        if (_gameDir != null)
        {
            var bep = Setup.BepInExState(_gameDir);
            var plugin = Setup.PluginState(_gameDir);
            checks.Add(Check("BepInEx", State(bep), bep == PartState.Installed));
            checks.Add(Check("Plugin", State(plugin), plugin == PartState.Installed));
            InstallButton.Content = Loc.T(bep == PartState.Installed && plugin == PartState.Outdated ? "setup.update" : "setup.install");
            InstallButton.Visibility = bep == PartState.Installed && plugin == PartState.Installed ? Visibility.Collapsed : Visibility.Visible;
            RemoveButton.Visibility = plugin != PartState.Missing ? Visibility.Visible : Visibility.Collapsed;
        }
        else
        {
            InstallButton.Visibility = RemoveButton.Visibility = Visibility.Collapsed;
        }
        SetupChecks.ItemsSource = checks;
        SetupPath.Text = _gameDir ?? "";
        InstallButton.IsEnabled = PickButton.IsEnabled = RemoveButton.IsEnabled = !_setupBusy;
    }

    async void Install_Click(object sender, RoutedEventArgs e) => await RunSetupStep("install");

    async void Remove_Click(object sender, RoutedEventArgs e) => await RunSetupStep("remove");

    async System.Threading.Tasks.Task RunSetupStep(string action)
    {
        if (_gameDir == null || _setupBusy) return;
        if (Setup.GameRunning())
        {
            ShowMessage(Loc.T("setup.closegame"), "Bad");
            return;
        }

        _setupBusy = true;
        RenderSetup();
        try
        {
            if (Setup.CanWrite(_gameDir))
            {
                if (action == "install") await Setup.Install(_gameDir, m => ShowMessage(m, "Muted"));
                else Setup.RemovePlugin(_gameDir);
            }
            else if (!await Setup.RunElevated(action, _gameDir))
            {
                throw new UnauthorizedAccessException(_gameDir);
            }
            ShowMessage(Loc.T(action == "install" ? "setup.done" : "setup.removed"), "Good");
        }
        catch (Exception ex)
        {
            ShowMessage(Loc.F("setup.failed", ex.Message), "Bad");
        }
        finally
        {
            _setupBusy = false;
            RenderSetup();
        }
    }

    void ShowMessage(string text, string brush)
    {
        SetupMessage.Text = text;
        SetupMessage.Foreground = (Brush)FindResource(brush == "Muted" ? "Muted" : brush);
    }

    void Pick_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "Digimon World Next Order.exe|Digimon World Next Order.exe", Title = Loc.T("setup.pickexe") };
        if (dlg.ShowDialog(this) != true) return;
        var dir = Path.GetDirectoryName(dlg.FileName);
        if (!Setup.IsGameDir(dir)) return;
        _gameDir = _settings.GameDir = dir;
        RenderSetup();
    }

    void Update_Click(object sender, RoutedEventArgs e)
    {
        try { Process.Start(Updates.ReleasesPage); } catch { /* kein Browser */ }
    }

    void Settings_Click(object sender, RoutedEventArgs e) => ShowSetup(!_setupForced);

    void SetupClose_Click(object sender, RoutedEventArgs e) => ShowSetup(false);

    // ---------- Fenster ----------

    void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed) DragMove();
    }

    void Pin_Click(object sender, RoutedEventArgs e)
    {
        Topmost = !Topmost;
        PinButton.Opacity = Topmost ? 1 : 0.4;
    }

    void Opacity_Click(object sender, RoutedEventArgs e)
    {
        var i = Array.FindIndex(OpacitySteps, s => Math.Abs(s - Root.Opacity) < 0.01);
        SetOpacity(OpacitySteps[(i + 1) % OpacitySteps.Length]);
    }

    void SetOpacity(double value)
    {
        Root.Opacity = value;
        OpacityButton.Content = $"{value * 100:0}%";
    }

    void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    void Close_Click(object sender, RoutedEventArgs e) => Close();

    void LoadSettings()
    {
        _settings = Json.Read<Settings>(SettingsPath) ?? new Settings();
        if (_settings.Width < MinWidth || _settings.Height < MinHeight) { _settings.Width = 440; _settings.Height = 760; }

        Width = _settings.Width;
        Height = _settings.Height;
        if (!double.IsNaN(_settings.Left) && !double.IsNaN(_settings.Top) &&
            _settings.Left > SystemParameters.VirtualScreenLeft - 50 && _settings.Top > SystemParameters.VirtualScreenTop - 50 &&
            _settings.Left < SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - 50 &&
            _settings.Top < SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - 50)
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = _settings.Left;
            Top = _settings.Top;
        }
        else
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
        Topmost = _settings.Topmost;
        PinButton.Opacity = Topmost ? 1 : 0.4;
        SetOpacity(_settings.Opacity is > 0.3 and <= 1 ? _settings.Opacity : 1);
        if (_settings.Goals == null && _settings.Goal != null) _settings.Goals = new[] { _settings.Goal, null };
        _settings.Goal = null;
        PartnerPanels.ItemsSource = _panels;
        var tab = _settings.Partner switch { 1 => Tab1, 2 => TabBoth, _ => Tab0 };
        if (_settings.Partner == 2) _widthBeforeBoth = 440; // „Beide“ war aktiv → beim Zurückschalten schmal
        tab.IsChecked = true;
    }

    void SaveSettings()
    {
        try
        {
            _settings.Left = Left;
            _settings.Top = Top;
            _settings.Width = _partner == BothPartners && _widthBeforeBoth > 0 ? _widthBeforeBoth : Width;
            _settings.Partner = _partner;
            _settings.Height = Height;
            _settings.Topmost = Topmost;
            _settings.Opacity = Root.Opacity;
            Json.Write(SettingsPath, _settings);
        }
        catch
        {
            // Einstellungen sind nicht kritisch
        }
    }
}
