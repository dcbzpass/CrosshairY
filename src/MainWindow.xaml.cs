using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace CrosshairY;

public partial class MainWindow : Window
{
    private readonly AppState       _s = new();
    private GlobalKeyboardHook?     _hook;

    private bool            _bindingMode;
    private Action<string>? _bindingCallback;
    private Button?         _bindingBtn;

    private double           _scrollTarget;
    private DispatcherTimer? _scrollTimer;

    private Button? _activeNavBtn;

    private CrosshairOverlay? _crOverlay;

    private static readonly string AppDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CrosshairY");

    private static readonly string ProfilesDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CrosshairY", "Configs");

    private static readonly string LastUsedFile =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CrosshairY", "Configs", ".lastused");

    private static readonly string LaunchesFile =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CrosshairY", "launches.dat");

    private static readonly (string id, string name)[] CrTemplates =
    {
        ("dot",              "Dot"),
        ("ring",             "Ring"),
        ("sq_dot",           "Square"),
        ("thin_cross",       "Thin +"),
        ("thick_cross",      "Thick +"),
        ("cross_dot_c",      "Cross·"),
        ("t_shape",          "T-Shape"),
        ("cross_circle",     "Circle+"),
        ("small_plus",       "S.Plus"),
        ("large_plus",       "L.Plus"),
        ("sniper",           "Sniper"),
        ("x_cross",          "X Cross"),
        ("x_dot",            "X·"),
        ("inward_arrows",    "Arrows"),
        ("outward_chevrons", "Chevrons"),
        ("triangle",         "Triangle"),
        ("diamond",          "Diamond")
    };

    private static readonly string[] CrColors =
    {
        "#ffffff", "#ff3333", "#33ff66", "#00ffff",
        "#ffff00", "#ff00ff", "#ff8800", "#000000"
    };

    [DllImport("user32.dll")] private static extern bool ReleaseCapture();
    [DllImport("user32.dll")] private static extern int  SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
    private const int WM_NCLBUTTONDOWN = 0xA1;
    private const int HT_CAPTION       = 0x2;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);
    private const uint WDA_NONE               = 0x00000000;
    private const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    private const int GWL_EXSTYLE      = -20;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_APPWINDOW  = 0x00040000;

    public MainWindow()
    {
        RenderOptions.ProcessRenderMode = RenderMode.Default;
        InitializeComponent();

        SourceInitialized += (_, _) => ApplyCaptureAffinity();

        Loaded += (_, _) =>
        {
            _hook = new GlobalKeyboardHook();
            _hook.KeyDown += OnGlobalKeyDown;

            if (OuterBorder != null)
            {
                var clip = new RectangleGeometry { RadiusX = 0, RadiusY = 0, Rect = new Rect(0, 0, ActualWidth, ActualHeight) };
                OuterBorder.Clip = clip;
                SizeChanged += (_, e) => { clip.Rect = new Rect(0, 0, e.NewSize.Width, e.NewSize.Height); };
            }
        };

        Closed += (_, _) =>
        {
            _hook?.Dispose();
            _crOverlay?.Close();
        };

        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
        {
            var windowFade = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(350)))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            windowFade.Completed += (_, _) =>
            {
                var t = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
                t.Tick += (_, _) => { t.Stop(); StartTyping(); };
                t.Start();
            };
            BeginAnimation(OpacityProperty, windowFade);
        });
    }

    private void ApplyCaptureAffinity()
    {
        bool active = _s.CaptureHidden;
        uint aff    = active ? WDA_EXCLUDEFROMCAPTURE : WDA_NONE;
        var  hwnd   = new WindowInteropHelper(this).Handle;

        SetWindowDisplayAffinity(hwnd, aff);
        _crOverlay?.SetProof(active);

        int ex = GetWindowLong(hwnd, GWL_EXSTYLE);
        if (active) { ex |= WS_EX_TOOLWINDOW; ex &= ~WS_EX_APPWINDOW; }
        else        { ex &= ~WS_EX_TOOLWINDOW; ex |= WS_EX_APPWINDOW; }
        SetWindowLong(hwnd, GWL_EXSTYLE, ex);
    }

    private void ToggleCaptureHide()
    {
        _s.CaptureHidden = !_s.CaptureHidden;
        ApplyCaptureAffinity();
    }

    private void StartTyping()
    {
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

        var fadeIn = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(600))) { EasingFunction = ease };
        AsciiText.BeginAnimation(OpacityProperty, fadeIn);

        var scaleX = new DoubleAnimation(0.92, 1, new Duration(TimeSpan.FromMilliseconds(600))) { EasingFunction = ease };
        var scaleY = new DoubleAnimation(0.92, 1, new Duration(TimeSpan.FromMilliseconds(600))) { EasingFunction = ease };
        var st = (ScaleTransform)AsciiText.RenderTransform;
        st.BeginAnimation(ScaleTransform.ScaleXProperty, scaleX);
        st.BeginAnimation(ScaleTransform.ScaleYProperty, scaleY);

        var lineExpand = new DoubleAnimation(0, 120, new Duration(TimeSpan.FromMilliseconds(500)))
        {
            BeginTime      = TimeSpan.FromMilliseconds(400),
            EasingFunction = ease
        };
        AccentLine.BeginAnimation(FrameworkElement.WidthProperty, lineExpand);

        var t2 = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1600) };
        t2.Tick += (_, _) =>
        {
            t2.Stop();
            StatusText.Text = "starting CrosshairY...";
            var fade = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(400)))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            StatusText.BeginAnimation(OpacityProperty, fade);
        };
        t2.Start();

        var t3 = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(3000) };
        t3.Tick += (_, _) => { t3.Stop(); CheckAndShowSurvey(); };
        t3.Start();
    }

    // increments launch count, shows a pending survey if one is due, then continues to the main ui
    private void CheckAndShowSurvey()
    {
        int launchCount   = IncrementLaunchCount(out var completedIds);
        var pendingSurvey = GetPendingSurvey(launchCount, completedIds);

        if (pendingSurvey.HasValue)
        {
            var (surveyId, question, options) = pendingSurvey.Value;
            var win = new SurveyWindow(question, options, launchCount) { Owner = this };
            win.ShowDialog();
            if (win.Submitted)
                MarkSurveyCompleted(surveyId);
        }

        TransitionToMain();
    }

    private static int IncrementLaunchCount(out HashSet<string> completedIds)
    {
        Directory.CreateDirectory(AppDir);

        int count    = 1;
        completedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            if (File.Exists(LaunchesFile))
            {
                var parts = File.ReadAllText(LaunchesFile).Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0 && int.TryParse(parts[0].Trim(), out int stored))
                    count = stored + 1;
                foreach (var part in parts.Skip(1))
                    completedIds.Add(part.Trim());
            }
        }
        catch { }

        WriteLaunchesFile(count, completedIds);
        return count;
    }

    private static void MarkSurveyCompleted(string surveyId)
    {
        try
        {
            int count = 1;
            HashSet<string> completedIds;

            if (File.Exists(LaunchesFile))
            {
                var parts = File.ReadAllText(LaunchesFile).Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0 && int.TryParse(parts[0].Trim(), out int stored))
                    count = stored;
                completedIds = new HashSet<string>(parts.Skip(1).Select(p => p.Trim()), StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                completedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            completedIds.Add(surveyId);
            WriteLaunchesFile(count, completedIds);
        }
        catch { }
    }

    private static void WriteLaunchesFile(int count, HashSet<string> completedIds)
    {
        try
        {
            var parts = new List<string> { count.ToString() };
            parts.AddRange(completedIds);
            File.WriteAllText(LaunchesFile, string.Join(",", parts));
        }
        catch { }
    }

    private static readonly (string id, string question, string[] options)[] Surveys =
    {
        (
            "survey_3",
            "How did you find us?",
            new[] { "TikTok", "GitHub", "Discord", "Friend", "Other" }
        ),
        (
            "survey_7",
            "What game do you mainly use CrosshairY for?",
            new[] { "Fortnite", "Blood Strike", "CS2", "Apex Legends", "Valorant", "Other" }
        ),
        (
            "survey_15",
            "What would you like to see added next?",
            new[] { "More crosshair templates", "Custom colors (hex input)", "Hotkey to toggle crosshair", "Multiple crosshair profiles active at once", "Other" }
        ),
        (
            "survey_30",
            "How would you rate CrosshairY?",
            new[] { "1 star", "2 stars", "3 stars", "4 stars", "5 stars" }
        )
    };

    private static readonly int[] SurveyTriggers = { 3, 7, 15, 30 };

    private static (string id, string question, string[] options)? GetPendingSurvey(int launchCount, HashSet<string> completedIds)
    {
        for (int i = 0; i < SurveyTriggers.Length; i++)
            if (launchCount == SurveyTriggers[i] && !completedIds.Contains(Surveys[i].id))
                return Surveys[i];
        return null;
    }

    private void TransitionToMain()
    {
        var fade = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromMilliseconds(350)))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        fade.Completed += (_, _) =>
        {
            StartupGrid.Visibility = Visibility.Collapsed;
            MainGrid.Visibility    = Visibility.Visible;
            MainGrid.Opacity       = 0;
            var fadeIn = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(300)))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            MainGrid.BeginAnimation(OpacityProperty, fadeIn);
            InitSettingsPanel();
            SetupSmoothScroll(MainScrollViewer);
            AnimateNavSelect(BtnCrosshairs);
            _crOverlay = new CrosshairOverlay();
            TryLoadLastUsed();
            InitCrosshairsPanel();
            RefreshCrosshairOverlay();
        };
        StartupGrid.BeginAnimation(OpacityProperty, fade);
    }

    private void SetupSmoothScroll(ScrollViewer sv)
    {
        _scrollTarget = 0;
        sv.PreviewMouseWheel += (s, e) =>
        {
            e.Handled     = true;
            _scrollTarget = Math.Clamp(_scrollTarget - e.Delta * 0.45, 0, sv.ScrollableHeight);
            if (_scrollTimer == null || !_scrollTimer.IsEnabled)
            {
                _scrollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(7) };
                _scrollTimer.Tick += (_, _) =>
                {
                    double current = sv.VerticalOffset;
                    double diff    = _scrollTarget - current;
                    if (Math.Abs(diff) < 0.3) { sv.ScrollToVerticalOffset(_scrollTarget); _scrollTimer.Stop(); }
                    else sv.ScrollToVerticalOffset(current + diff * 0.18);
                };
                _scrollTimer.Start();
            }
        };
    }

    private void FadeInPanel(StackPanel panel)
    {
        panel.Opacity = 0;
        var anim = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(160)))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        panel.BeginAnimation(OpacityProperty, anim);
    }

    private void InitSettingsPanel()
    {
        ProofKeyBtn.Content = DisplayKey(_s.ProofKey);
    }

    private void InitCrosshairsPanel()
    {
        CrTemplatePanel.Children.Clear();
        foreach (var (id, name) in CrTemplates)
            CrTemplatePanel.Children.Add(BuildTemplateTile(id, name));

        CrColorPanel.Children.Clear();
        foreach (var hex in CrColors)
            CrColorPanel.Children.Add(BuildColorSwatch(hex));

        UpdateTemplateTileSelection();
        UpdateColorSwatchSelection();

        CrOutlineToggle.IsChecked = _s.CrOutline;
        CrOutlineSizeSlider.Value = _s.CrOutlineSize;
        CrSizeSlider.Value        = _s.CrSize;
        CrOutlineSizeLabel.Text   = _s.CrOutlineSize.ToString();
        CrSizeLabel.Text          = $"{_s.CrSize}%";
    }

    private UIElement BuildTemplateTile(string id, string name)
    {
        var canvas = new Canvas
        {
            Width      = 64,
            Height     = 64,
            Background = new SolidColorBrush(Color.FromRgb(0x14, 0x14, 0x14))
        };
        CrDraw.Draw(canvas, 32, 32, 0.5, _s.CrColor, _s.CrOutline, _s.CrOutlineSize, id);

        var label = new TextBlock
        {
            Text                = name,
            FontFamily          = (FontFamily)FindResource("IBMPlexMono"),
            FontSize            = 8,
            Foreground          = new SolidColorBrush(Color.FromRgb(0x5a, 0x5a, 0x5a)),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin              = new Thickness(0, 4, 0, 0)
        };

        var inner = new StackPanel();
        inner.Children.Add(canvas);
        inner.Children.Add(label);

        var border = new Border
        {
            Width           = 74,
            Height          = 88,
            Padding         = new Thickness(4),
            BorderThickness = new Thickness(1),
            BorderBrush     = new SolidColorBrush(Color.FromRgb(0x1e, 0x1e, 0x1e)),
            Margin          = new Thickness(0, 0, 6, 6),
            Cursor          = Cursors.Hand,
            Tag             = id,
            Child           = inner
        };

        border.MouseLeftButtonDown += (_, _) =>
        {
            _s.CrTemplate = _s.CrTemplate == id ? "" : id;
            UpdateTemplateTileSelection();
            RefreshCrosshairOverlay();
        };

        return border;
    }

    private UIElement BuildColorSwatch(string hex)
    {
        Color col;
        try   { col = (Color)ColorConverter.ConvertFromString(hex); }
        catch { col = Colors.White; }

        var border = new Border
        {
            Width           = 24,
            Height          = 24,
            Margin          = new Thickness(0, 0, 6, 0),
            BorderThickness = new Thickness(2),
            BorderBrush     = new SolidColorBrush(Colors.Transparent),
            Background      = new SolidColorBrush(col),
            Cursor          = Cursors.Hand,
            Tag             = hex
        };

        border.MouseLeftButtonDown += (_, _) =>
        {
            _s.CrColor = hex;
            UpdateColorSwatchSelection();
            RebuildTemplatePreviews();
            RefreshCrosshairOverlay();
        };

        return border;
    }

    private void UpdateTemplateTileSelection()
    {
        foreach (UIElement el in CrTemplatePanel.Children)
            if (el is Border b)
                b.BorderBrush = b.Tag as string == _s.CrTemplate
                    ? new SolidColorBrush(Color.FromRgb(0xf5, 0xf5, 0xf5))
                    : new SolidColorBrush(Color.FromRgb(0x1e, 0x1e, 0x1e));
    }

    private void UpdateColorSwatchSelection()
    {
        foreach (UIElement el in CrColorPanel.Children)
            if (el is Border b)
                b.BorderBrush = b.Tag as string == _s.CrColor
                    ? new SolidColorBrush(Color.FromRgb(0xf5, 0xf5, 0xf5))
                    : new SolidColorBrush(Colors.Transparent);
    }

    private void RebuildTemplatePreviews()
    {
        foreach (UIElement el in CrTemplatePanel.Children)
            if (el is Border b && b.Child is StackPanel sp && sp.Children.Count > 0 && sp.Children[0] is Canvas canvas)
                CrDraw.Draw(canvas, 32, 32, 0.5, _s.CrColor, _s.CrOutline, _s.CrOutlineSize, b.Tag as string ?? "");
    }

    private void RefreshCrosshairOverlay()
    {
        _crOverlay?.UpdateCrosshair(_s.CrTemplate, _s.CrColor, _s.CrOutline, _s.CrOutlineSize, _s.CrSize);
    }

    private void CrToggle_Changed(object s, RoutedEventArgs e)
    {
        _s.CrOutline = CrOutlineToggle.IsChecked == true;
        if (CrTemplatePanel != null) RebuildTemplatePreviews();
        RefreshCrosshairOverlay();
    }

    private void CrOutlineSizeSlider_Changed(object s, RoutedPropertyChangedEventArgs<double> e)
    {
        _s.CrOutlineSize = (int)e.NewValue;
        if (CrOutlineSizeLabel != null) CrOutlineSizeLabel.Text = _s.CrOutlineSize.ToString();
        if (CrTemplatePanel   != null) RebuildTemplatePreviews();
        RefreshCrosshairOverlay();
    }

    private void CrSizeSlider_Changed(object s, RoutedPropertyChangedEventArgs<double> e)
    {
        _s.CrSize = (int)e.NewValue;
        if (CrSizeLabel != null) CrSizeLabel.Text = $"{_s.CrSize}%";
        RefreshCrosshairOverlay();
    }

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            ReleaseCapture();
            SendMessage(new WindowInteropHelper(this).Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
        }
    }

    private void Minimize_Click(object s, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void Close_Click(object s, RoutedEventArgs e)
    {
        GoodbyeOverlay.Visibility = Visibility.Visible;

        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

        var overlayFade = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(300))) { EasingFunction = ease };
        GoodbyeOverlay.BeginAnimation(OpacityProperty, overlayFade);

        var textFade = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(600))) { EasingFunction = ease };
        GoodbyeText.BeginAnimation(OpacityProperty, textFade);

        var st = (ScaleTransform)GoodbyeText.RenderTransform;
        st.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0.92, 1, new Duration(TimeSpan.FromMilliseconds(600))) { EasingFunction = ease });
        st.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.92, 1, new Duration(TimeSpan.FromMilliseconds(600))) { EasingFunction = ease });

        var lineExpand = new DoubleAnimation(0, 120, new Duration(TimeSpan.FromMilliseconds(500)))
        {
            BeginTime      = TimeSpan.FromMilliseconds(400),
            EasingFunction = ease
        };
        GoodbyeLine.BeginAnimation(FrameworkElement.WidthProperty, lineExpand);

        var subFade = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(400)))
        {
            BeginTime      = TimeSpan.FromMilliseconds(800),
            EasingFunction = ease
        };
        GoodbyeSubText.BeginAnimation(OpacityProperty, subFade);

        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(2800) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            var fadeOut = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromMilliseconds(350))) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } };
            fadeOut.Completed += (_, _) => Application.Current.Shutdown();
            BeginAnimation(OpacityProperty, fadeOut);
        };
        timer.Start();
    }

    private void AnimateNavSelect(Button activate)
    {
        if (_activeNavBtn != null && _activeNavBtn != activate)
        {
            var offBrush = new SolidColorBrush(Color.FromRgb(0x14, 0x14, 0x14));
            _activeNavBtn.Background = offBrush;
            offBrush.BeginAnimation(SolidColorBrush.ColorProperty,
                new ColorAnimation(Color.FromRgb(0x14, 0x14, 0x14), Colors.Transparent, new Duration(TimeSpan.FromMilliseconds(200)))
                { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
        }
        activate.Background = new SolidColorBrush(Color.FromRgb(0x14, 0x14, 0x14));
        _activeNavBtn = activate;
    }

    private void BtnCrosshairs_Click(object s, RoutedEventArgs e)
    {
        CrosshairsPanel.Visibility = Visibility.Visible;
        SettingsPanel.Visibility   = Visibility.Collapsed;
        ProfilesPanel.Visibility   = Visibility.Collapsed;
        SupportPanel.Visibility    = Visibility.Collapsed;
        FadeInPanel(CrosshairsPanel);
        AnimateNavSelect(BtnCrosshairs);
        _scrollTarget = 0;
        MainScrollViewer.ScrollToTop();
    }

    private void BtnSettings_Click(object s, RoutedEventArgs e)
    {
        SettingsPanel.Visibility   = Visibility.Visible;
        CrosshairsPanel.Visibility = Visibility.Collapsed;
        ProfilesPanel.Visibility   = Visibility.Collapsed;
        SupportPanel.Visibility    = Visibility.Collapsed;
        FadeInPanel(SettingsPanel);
        AnimateNavSelect(BtnSettings);
        _scrollTarget = 0;
        MainScrollViewer.ScrollToTop();
    }

    private void BtnSupport_Click(object s, RoutedEventArgs e)
    {
        SupportPanel.Visibility    = Visibility.Visible;
        CrosshairsPanel.Visibility = Visibility.Collapsed;
        ProfilesPanel.Visibility   = Visibility.Collapsed;
        SettingsPanel.Visibility   = Visibility.Collapsed;
        FadeInPanel(SupportPanel);
        AnimateNavSelect(BtnSupport);
        _scrollTarget = 0;
        MainScrollViewer.ScrollToTop();
    }

    private void SocialLink_Click(object s, RoutedEventArgs e)
    {
        if (s is Button btn && btn.Tag is string url)
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
    }

    private void StartBinding(Button btn, Action<string> callback)
    {
        if (_bindingMode) return;
        _bindingMode     = true;
        _bindingCallback = callback;
        _bindingBtn      = btn;
        btn.Content      = "...";
        btn.Background   = (Brush)FindResource("Red");
    }

    private void FinishBinding(string key)
    {
        if (!_bindingMode) return;
        _bindingCallback?.Invoke(key);
        if (_bindingBtn is { } b)
        {
            b.Content    = DisplayKey(key);
            b.Background = (Brush)FindResource("BgBtn");
        }
        _bindingMode     = false;
        _bindingCallback = null;
        _bindingBtn      = null;
    }

    private void ProofKeyBtn_Click(object s, RoutedEventArgs e) => StartBinding(ProofKeyBtn, k => _s.ProofKey = k);

    private void OnGlobalKeyDown(string key)
    {
        if (_bindingMode)
        {
            Dispatcher.Invoke(() => FinishBinding(key));
            return;
        }

        if (key == _s.ProofKey)
            Dispatcher.Invoke(ToggleCaptureHide);
    }

    private void BtnProfiles_Click(object s, RoutedEventArgs e)
    {
        ProfilesPanel.Visibility   = Visibility.Visible;
        CrosshairsPanel.Visibility = Visibility.Collapsed;
        SettingsPanel.Visibility   = Visibility.Collapsed;
        SupportPanel.Visibility    = Visibility.Collapsed;
        FadeInPanel(ProfilesPanel);
        AnimateNavSelect(BtnProfiles);
        _scrollTarget = 0;
        MainScrollViewer.ScrollToTop();
        LoadProfileList();
    }

    private void LoadProfileList()
    {
        ProfileListPanel.Children.Clear();

        Directory.CreateDirectory(ProfilesDir);
        var files = Directory.GetFiles(ProfilesDir, "*.json");

        if (files.Length == 0)
        {
            ProfileListPanel.Children.Add(new TextBlock
            {
                Text         = "No configs found. Save one or drop a .json file into the folder.",
                FontFamily   = (FontFamily)FindResource("IBMPlexMono"),
                FontSize     = 9,
                Foreground   = new SolidColorBrush(Color.FromRgb(0x5a, 0x5a, 0x5a)),
                TextWrapping = TextWrapping.Wrap,
                Margin       = new Thickness(0, 4, 0, 4)
            });
            return;
        }

        string? lastUsed = null;
        try { if (File.Exists(LastUsedFile)) lastUsed = File.ReadAllText(LastUsedFile).Trim(); } catch { }

        foreach (var file in files.OrderBy(f => f))
        {
            var  name     = Path.GetFileNameWithoutExtension(file);
            var  path     = file;
            bool isActive = string.Equals(path, lastUsed, StringComparison.OrdinalIgnoreCase);

            var nameBlock = new TextBlock
            {
                Text              = isActive ? name + " ●" : name,
                FontFamily        = (FontFamily)FindResource("IBMPlexMono"),
                FontSize          = 11,
                FontWeight        = FontWeights.Bold,
                Foreground        = new SolidColorBrush(isActive
                    ? Color.FromRgb(0xf5, 0xf5, 0xf5)
                    : Color.FromRgb(0x8a, 0x8a, 0x8a)),
                VerticalAlignment = VerticalAlignment.Center
            };

            var loadBtn = new Button { Content = "LOAD", Style = (Style)FindResource("DarkBtn"), Width = 60, Tag = path };
            loadBtn.Click += (_, _) => { LoadProfile(path); LoadProfileList(); };

            var saveBtn = new Button { Content = "SAVE", Style = (Style)FindResource("DarkBtn"), Width = 60, Margin = new Thickness(6, 0, 0, 0), Tag = path };
            saveBtn.Click += (_, _) =>
            {
                var cfg = new
                {
                    proof_key       = _s.ProofKey,
                    cr_template     = _s.CrTemplate,
                    cr_color        = _s.CrColor,
                    cr_outline      = _s.CrOutline,
                    cr_outline_size = _s.CrOutlineSize,
                    cr_size         = _s.CrSize
                };
                File.WriteAllText(path, JsonSerializer.Serialize(cfg, new JsonSerializerOptions { WriteIndented = true }));
                LoadProfileList();
            };

            var deleteBtn = new Button { Content = "DEL", Style = (Style)FindResource("DarkBtn"), Width = 48, Margin = new Thickness(6, 0, 0, 0), Tag = path };
            deleteBtn.Click += (_, _) => { try { File.Delete(path); } catch { } LoadProfileList(); };

            var row = new Grid { Margin = new Thickness(0, 0, 0, 6) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetColumn(nameBlock, 0);
            Grid.SetColumn(loadBtn,   1);
            Grid.SetColumn(saveBtn,   2);
            Grid.SetColumn(deleteBtn, 3);
            row.Children.Add(nameBlock);
            row.Children.Add(loadBtn);
            row.Children.Add(saveBtn);
            row.Children.Add(deleteBtn);

            ProfileListPanel.Children.Add(new Border
            {
                BorderBrush     = new SolidColorBrush(Color.FromRgb(0x1e, 0x1e, 0x1e)),
                BorderThickness = new Thickness(1),
                Background      = new SolidColorBrush(Color.FromRgb(0x0a, 0x0a, 0x0a)),
                Padding         = new Thickness(12, 10, 12, 10),
                Margin          = new Thickness(0, 0, 0, 6),
                Child           = row
            });
        }
    }

    private void SaveProfile_Click(object s, RoutedEventArgs e)
    {
        var name = ProfileNameBox.Text.Trim();
        if (string.IsNullOrEmpty(name)) return;

        var invalid = Path.GetInvalidFileNameChars();
        name = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());

        Directory.CreateDirectory(ProfilesDir);
        var path = Path.Combine(ProfilesDir, name + ".json");

        var cfg = new
        {
            proof_key       = _s.ProofKey,
            cr_template     = _s.CrTemplate,
            cr_color        = _s.CrColor,
            cr_outline      = _s.CrOutline,
            cr_outline_size = _s.CrOutlineSize,
            cr_size         = _s.CrSize
        };

        File.WriteAllText(path, JsonSerializer.Serialize(cfg, new JsonSerializerOptions { WriteIndented = true }));
        LoadProfileList();
    }

    private void TryLoadLastUsed()
    {
        try
        {
            if (!File.Exists(LastUsedFile)) return;
            var path = File.ReadAllText(LastUsedFile).Trim();
            if (File.Exists(path)) LoadProfile(path, writeLastUsed: false);
        }
        catch { }
    }

    private void LoadProfile(string path, bool writeLastUsed = true)
    {
        try
        {
            var json = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);
            var r = doc.RootElement;

            if (r.TryGetProp("proof_key",   out var v)) _s.ProofKey   = v;
            if (r.TryGetProp("cr_template", out v))     _s.CrTemplate = v;
            if (r.TryGetProp("cr_color",    out v))     _s.CrColor    = v;
            if (r.TryGetProperty("cr_outline",      out var jv) && jv.ValueKind == JsonValueKind.True)  _s.CrOutline = true;
            if (r.TryGetProperty("cr_outline",      out jv)     && jv.ValueKind == JsonValueKind.False) _s.CrOutline = false;
            if (r.TryGetProperty("cr_outline_size", out jv) && jv.TryGetInt32(out var i)) _s.CrOutlineSize = i;
            if (r.TryGetProperty("cr_size",         out jv) && jv.TryGetInt32(out i))     _s.CrSize        = i;

            if (writeLastUsed)
            {
                Directory.CreateDirectory(ProfilesDir);
                File.WriteAllText(LastUsedFile, path);
            }

            InitSettingsPanel();
            InitCrosshairsPanel();
            RefreshCrosshairOverlay();
        }
        catch { }
    }

    private void ReloadProfiles_Click(object s, RoutedEventArgs e) => LoadProfileList();

    private static string DisplayKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return "—";
        var d = key.Replace("Key.", "").ToUpper();
        return d.Length > 8 ? d[..8] : d;
    }
}

internal static class JsonExt
{
    public static bool TryGetProp(this JsonElement el, string name, out string value)
    {
        if (el.TryGetProperty(name, out var prop) && prop.GetString() is { } s)
        { value = s; return true; }
        value = "";
        return false;
    }
}
