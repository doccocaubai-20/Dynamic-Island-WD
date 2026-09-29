using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using DynamicIslandApp.Models;
using DynamicIslandApp.Services;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using MouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using ContextMenu = System.Windows.Controls.ContextMenu;
using MenuItem = System.Windows.Controls.MenuItem;
using Separator = System.Windows.Controls.Separator;

namespace DynamicIslandApp;

public partial class IslandWindow : Window
{
    // Win32 Constants
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WM_NCHITTEST = 0x0084;
    private const int HTTRANSPARENT = -1;
    private const int HTCLIENT = 1;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

    private readonly IslandSettings _settings;
    private readonly WindowsMediaService _mediaService;
    private readonly DispatcherTimer _clockTimer;
    private readonly DispatcherTimer _eqTimer;
    private readonly DispatcherTimer _hoverTimer;
    private readonly DispatcherTimer _collapseTimer;
    private readonly Random _random = new();

    private bool _isExpanded = false;
    private bool _isDraggingSlider = false;
    private bool _isAnimating = false;
    private double _eqPhase = 0;

    // Cached physical screen bounds of PillBorder for instantaneous, zero-cost WM_NCHITTEST
    private double _screenPillLeft = 0;
    private double _screenPillTop = 0;
    private double _screenPillRight = 0;
    private double _screenPillBottom = 0;

    private static readonly Geometry PlayIconData = Geometry.Parse("M8,5.14V19.14L19,12.14L8,5.14Z");
    private static readonly Geometry PauseIconData = Geometry.Parse("M6,19h4V5H6V19z M14,5v14h4V5H14z");

    public IslandWindow(IslandSettings settings, WindowsMediaService mediaService)
    {
        InitializeComponent();

        _settings = settings;
        _mediaService = mediaService;

        _clockTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (s, e) => UpdateClock();

        _eqTimer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(70) };
        _eqTimer.Tick += (s, e) => UpdateEqualizer();

        _hoverTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _hoverTimer.Tick += (s, e) =>
        {
            _hoverTimer.Stop();
            if (PillBorder.IsMouseOver && !_isExpanded)
            {
                Expand(animate: true);
            }
        };

        _collapseTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1200) };
        _collapseTimer.Tick += (s, e) =>
        {
            _collapseTimer.Stop();
            if (_isExpanded && !PillBorder.IsMouseOver)
            {
                Collapse(animate: true);
            }
        };

        _mediaService.MediaChanged += OnMediaChanged;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // 1. Center Window horizontally at top of primary screen
        var screenWidth = SystemParameters.PrimaryScreenWidth;
        Left = (screenWidth - Width) / 2.0;
        Top = 0;

        // 2. Set ToolWindow style so it doesn't show in Alt+Tab
        var helper = new WindowInteropHelper(this);
        var exStyle = GetWindowLong(helper.Handle, GWL_EXSTYLE);
        SetWindowLong(helper.Handle, GWL_EXSTYLE, exStyle | WS_EX_TOOLWINDOW);

        // 3. Register Win32 Hook for click-through outside the pill
        var source = HwndSource.FromHwnd(helper.Handle);
        source?.AddHook(WndProc);

        // 4. Apply Settings
        ApplySettings();

        // 5. Compute initial physical screen bounds
        UpdateScreenBounds();

        // 6. Start Timers
        UpdateClock();
        _clockTimer.Start();
        _eqTimer.Start();

        // 7. Initial Media State
        OnMediaChanged(_mediaService.CurrentTrack);
    }

    private void PillBorder_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateScreenBounds();
    }

    public void UpdateScreenBounds()
    {
        if (!IsLoaded) return;
        try
        {
            var topLeft = PillBorder.PointToScreen(new Point(0, 0));
            var bottomRight = PillBorder.PointToScreen(new Point(PillBorder.ActualWidth, PillBorder.ActualHeight));
            _screenPillLeft = topLeft.X;
            _screenPillTop = topLeft.Y;
            _screenPillRight = bottomRight.X;
            _screenPillBottom = bottomRight.Y;
        }
        catch { }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_NCHITTEST)
        {
            // Extract screen coordinate from lParam
            int x = (short)(lParam.ToInt32() & 0xFFFF);
            int y = (short)((lParam.ToInt32() >> 16) & 0xFFFF);

            // Instant integer boundary test - zero allocations, zero lag
            if (x >= _screenPillLeft && x <= _screenPillRight &&
                y >= _screenPillTop && y <= _screenPillBottom)
            {
                handled = true;
                return new IntPtr(HTCLIENT);
            }

            handled = true;
            return new IntPtr(HTTRANSPARENT);
        }

        return IntPtr.Zero;
    }

    public void ApplySettings()
    {
        if (_settings.Appearance == IslandAppearance.FullNotch)
        {
            PillBorder.Margin = new Thickness(0, 0, 0, 0);
            PillBorder.CornerRadius = _isExpanded ? new CornerRadius(0, 0, 22, 22) : new CornerRadius(0, 0, 16, 16);
        }
        else
        {
            PillBorder.Margin = new Thickness(0, 10, 0, 0);
            PillBorder.CornerRadius = _isExpanded ? new CornerRadius(24) : new CornerRadius(18);
        }

        if (_isExpanded)
        {
            Expand(animate: false);
        }
        else
        {
            Collapse(animate: false);
        }
        UpdateScreenBounds();
    }

    private void UpdateClock()
    {
        var now = DateTime.Now;
        var timeStr = now.ToString("HH:mm");
        CompactClockText.Text = timeStr;
        ExpandedClockText.Text = timeStr;

        // "Sunday, September 13" format
        ExpandedDateText.Text = now.ToString("dddd, MMMM d", CultureInfo.InvariantCulture);
    }

    private void UpdateEqualizer()
    {
        if (!_mediaService.CurrentTrack.IsPlaying)
        {
            SetEqScales(0.25, 0.35, 0.25, 0.35);
            return;
        }

        _eqPhase += 0.35;
        double s1 = Math.Clamp(0.3 + Math.Sin(_eqPhase * 1.3) * 0.35 + (_random.NextDouble() * 0.3), 0.2, 1.0);
        double s2 = Math.Clamp(0.4 + Math.Cos(_eqPhase * 1.7) * 0.45 + (_random.NextDouble() * 0.35), 0.25, 1.0);
        double s3 = Math.Clamp(0.3 + Math.Sin(_eqPhase * 2.1) * 0.3 + (_random.NextDouble() * 0.3), 0.2, 0.95);
        double s4 = Math.Clamp(0.4 + Math.Cos(_eqPhase * 1.1) * 0.4 + (_random.NextDouble() * 0.35), 0.25, 1.0);

        SetEqScales(s1, s2, s3, s4);
    }

    private void SetEqScales(double s1, double s2, double s3, double s4)
    {
        // Update GPU ScaleTransforms directly - NO layout invalidation!
        CBar1Scale.ScaleY = s1;
        CBar2Scale.ScaleY = s2;
        CBar3Scale.ScaleY = s3;
        CBar4Scale.ScaleY = s4;

        EBar1Scale.ScaleY = s1;
        EBar2Scale.ScaleY = s2;
        EBar3Scale.ScaleY = s3;
        EBar4Scale.ScaleY = s4;
    }

    private void OnMediaChanged(MediaTrackInfo track)
    {
        Dispatcher.Invoke(() =>
        {
            if (track.HasMedia)
            {
                // Compact view updates
                CompactAlbumArt.Source = track.AlbumArt;
                CompactTitleText.Text = string.IsNullOrEmpty(track.Artist)
                    ? track.Title
                    : $"{track.Title} - {track.Artist}";

                // Expanded view updates
                ExpandedAlbumArt.Source = track.AlbumArt;
                ExpandedTitleText.Text = track.Title;
                ExpandedArtistText.Text = string.IsNullOrEmpty(track.Artist) ? "Unknown Artist" : track.Artist;

                // Times
                CurrentTimeText.Text = track.Position.ToString(@"m\:ss");
                TotalTimeText.Text = track.Duration.ToString(@"m\:ss");

                if (!_isDraggingSlider && track.Duration.TotalSeconds > 0)
                {
                    MediaSeekSlider.Value = (track.Position.TotalSeconds / track.Duration.TotalSeconds) * 100.0;
                }

                // Play / Pause Icon
                PlayPausePath.Data = track.IsPlaying ? PauseIconData : PlayIconData;
            }

            // Adapt view
            RefreshViewState(animate: true);
        });
    }

    public void Expand(bool animate = true)
    {
        _isExpanded = true;
        _hoverTimer.Stop();
        _collapseTimer.Stop();

        double scale = _settings.WidthScale;
        double targetWidth;
        double targetHeight;

        if (_mediaService.CurrentTrack.HasMedia)
        {
            targetWidth = 390 * scale;
            targetHeight = 168;
            SwitchToView(ExpandedMediaView, animate);
        }
        else
        {
            targetWidth = 340 * scale;
            targetHeight = 130;
            SwitchToView(ExpandedIdleView, animate);
        }

        if (_settings.Appearance == IslandAppearance.FullNotch)
        {
            PillBorder.Margin = new Thickness(0, 0, 0, 0);
            PillBorder.CornerRadius = new CornerRadius(0, 0, 22, 22);
        }
        else
        {
            PillBorder.Margin = new Thickness(0, 10, 0, 0);
            PillBorder.CornerRadius = new CornerRadius(24);
        }

        if (animate)
        {
            StartAnimation(targetWidth, targetHeight);
        }
        else
        {
            PillBorder.Width = targetWidth;
            PillBorder.Height = targetHeight;
            UpdateScreenBounds();
        }
    }

    public void Collapse(bool animate = true)
    {
        _isExpanded = false;
        _hoverTimer.Stop();
        _collapseTimer.Stop();

        double scale = _settings.WidthScale;
        double targetWidth;
        double targetHeight;

        if (_mediaService.CurrentTrack.HasMedia)
        {
            targetWidth = 310 * scale;
            targetHeight = 34;
            SwitchToView(CompactMediaView, animate);
        }
        else
        {
            targetWidth = 180 * scale;
            targetHeight = 32;
            SwitchToView(CompactIdleView, animate);
        }

        if (_settings.Appearance == IslandAppearance.FullNotch)
        {
            PillBorder.Margin = new Thickness(0, 0, 0, 0);
            PillBorder.CornerRadius = new CornerRadius(0, 0, 16, 16);
        }
        else
        {
            PillBorder.Margin = new Thickness(0, 10, 0, 0);
            PillBorder.CornerRadius = new CornerRadius(18);
        }

        if (animate)
        {
            StartAnimation(targetWidth, targetHeight);
        }
        else
        {
            PillBorder.Width = targetWidth;
            PillBorder.Height = targetHeight;
            UpdateScreenBounds();
        }
    }

    private void StartAnimation(double targetWidth, double targetHeight)
    {
        if (!_isAnimating)
        {
            _isAnimating = true;
            CompositionTarget.Rendering += OnCompositionRendering;
        }

        SpringAnimationHelper.AnimateDouble(PillBorder, WidthProperty, targetWidth, _settings.SpringBounce, 280, () =>
        {
            _isAnimating = false;
            CompositionTarget.Rendering -= OnCompositionRendering;
            UpdateScreenBounds();
        });

        SpringAnimationHelper.AnimateDouble(PillBorder, HeightProperty, targetHeight, _settings.SpringBounce, 280);
    }

    private void OnCompositionRendering(object? sender, EventArgs e)
    {
        UpdateScreenBounds();
    }

    private void RefreshViewState(bool animate)
    {
        if (_isExpanded)
        {
            Expand(animate);
        }
        else
        {
            Collapse(animate);
        }
    }

    private void SwitchToView(Grid targetView, bool animate)
    {
        var views = new[] { CompactIdleView, ExpandedIdleView, CompactMediaView, ExpandedMediaView };

        foreach (var view in views)
        {
            if (view == targetView)
            {
                view.Visibility = Visibility.Visible;
                if (animate)
                {
                    SpringAnimationHelper.AnimateOpacity(view, 1.0, 150);
                }
                else
                {
                    view.Opacity = 1.0;
                }
            }
            else if (view.Visibility == Visibility.Visible)
            {
                if (animate)
                {
                    SpringAnimationHelper.AnimateOpacity(view, 0.0, 100, () => view.Visibility = Visibility.Collapsed);
                }
                else
                {
                    view.Opacity = 0.0;
                    view.Visibility = Visibility.Collapsed;
                }
            }
        }
    }

    private void PillBorder_MouseEnter(object sender, MouseEventArgs e)
    {
        _collapseTimer.Stop();
        if (!_isExpanded)
        {
            _hoverTimer.Start();
        }
    }

    private void PillBorder_MouseLeave(object sender, MouseEventArgs e)
    {
        _hoverTimer.Stop();
        if (_isExpanded)
        {
            _collapseTimer.Start();
        }
    }

    private void PillBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _hoverTimer.Stop();
        if (_isExpanded)
        {
            Collapse(animate: true);
        }
        else
        {
            Expand(animate: true);
        }
    }

    private void PillBorder_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        var menu = new ContextMenu();

        var settingsItem = new MenuItem { Header = "⚙ Cài đặt (Settings)" };
        settingsItem.Click += (s, args) => App.ShowSettings();

        var toggleAppearanceItem = new MenuItem
        {
            Header = _settings.Appearance == IslandAppearance.FullNotch
                ? "Switch to Floating pill"
                : "Switch to Full notch"
        };
        toggleAppearanceItem.Click += (s, args) =>
        {
            _settings.Appearance = _settings.Appearance == IslandAppearance.FullNotch
                ? IslandAppearance.FloatingPill
                : IslandAppearance.FullNotch;
            _settings.Save();
            ApplySettings();
        };

        var demoMusicItem = new MenuItem
        {
            Header = _mediaService.IsDemoMode ? "⏹ Tắt Nhạc Demo" : "▶ Bật Nhạc Demo"
        };
        demoMusicItem.Click += (s, args) =>
        {
            _mediaService.IsDemoMode = !_mediaService.IsDemoMode;
        };

        var exitItem = new MenuItem { Header = "❌ Thoát ứng dụng" };
        exitItem.Click += (s, args) => Application.Current.Shutdown();

        menu.Items.Add(settingsItem);
        menu.Items.Add(toggleAppearanceItem);
        menu.Items.Add(demoMusicItem);
        menu.Items.Add(new Separator());
        menu.Items.Add(exitItem);

        menu.IsOpen = true;
    }

    // Media Control Buttons
    private async void PlayPauseBtn_Click(object sender, RoutedEventArgs e)
    {
        await _mediaService.TogglePlayPauseAsync();
    }

    private async void PrevBtn_Click(object sender, RoutedEventArgs e)
    {
        await _mediaService.SkipPreviousAsync();
    }

    private async void NextBtn_Click(object sender, RoutedEventArgs e)
    {
        await _mediaService.SkipNextAsync();
    }

    private void MediaSeekSlider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        _isDraggingSlider = true;
    }

    private async void MediaSeekSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        _isDraggingSlider = false;
        double ratio = MediaSeekSlider.Value / 100.0;
        await _mediaService.SeekAsync(ratio);
    }

    private void MediaSeekSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isDraggingSlider)
        {
            var seconds = (_mediaService.CurrentTrack.Duration.TotalSeconds * MediaSeekSlider.Value) / 100.0;
            CurrentTimeText.Text = TimeSpan.FromSeconds(seconds).ToString(@"m\:ss");
        }
    }

    private void VolumeBtn_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("ms-settings:sound") { UseShellExecute = true });
        }
        catch { }
    }

    private void SearchBtn_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("ms-search:") { UseShellExecute = true });
        }
        catch { }
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        _clockTimer.Stop();
        _eqTimer.Stop();
        _hoverTimer.Stop();
        _collapseTimer.Stop();
        CompositionTarget.Rendering -= OnCompositionRendering;
    }
}
