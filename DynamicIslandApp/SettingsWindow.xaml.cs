using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using DynamicIslandApp.Models;
using DynamicIslandApp.Services;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using MouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;

namespace DynamicIslandApp;

public partial class SettingsWindow : Window
{
    private readonly IslandSettings _settings;
    private readonly IslandWindow _islandWindow;
    private readonly WindowsMediaService _mediaService;
    private bool _isLoaded = false;

    public SettingsWindow(IslandSettings settings, IslandWindow islandWindow, WindowsMediaService mediaService)
    {
        InitializeComponent();

        _settings = settings;
        _islandWindow = islandWindow;
        _mediaService = mediaService;

        LoadCurrentSettings();
        _isLoaded = true;
    }

    private void LoadCurrentSettings()
    {
        // Appearance
        UpdateAppearanceCards(_settings.Appearance);

        // Width Scale
        WidthSlider.Value = _settings.WidthScale * 100.0;
        WidthScaleText.Text = $"Scale: {(int)WidthSlider.Value}%";

        // Spring Bounce
        BounceSlider.Value = _settings.SpringBounce;
        BounceText.Text = $"Bounce: {(int)BounceSlider.Value}";

        // Startup
        _settings.StartWithWindows = StartupManager.IsStartupEnabled();
        UpdateStartupToggleUI(_settings.StartWithWindows);
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void CloseBtn_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    private void FloatingCard_Click(object sender, MouseButtonEventArgs e)
    {
        _settings.Appearance = IslandAppearance.FloatingPill;
        UpdateAppearanceCards(IslandAppearance.FloatingPill);
        _settings.Save();
        _islandWindow.ApplySettings();
    }

    private void NotchCard_Click(object sender, MouseButtonEventArgs e)
    {
        _settings.Appearance = IslandAppearance.FullNotch;
        UpdateAppearanceCards(IslandAppearance.FullNotch);
        _settings.Save();
        _islandWindow.ApplySettings();
    }

    private void UpdateAppearanceCards(IslandAppearance mode)
    {
        var purpleBorder = (Brush)new BrushConverter().ConvertFrom("#7C5BF6")!;
        var darkBorder = (Brush)new BrushConverter().ConvertFrom("#2A2838")!;
        var activeBg = (Brush)new BrushConverter().ConvertFrom("#241F38")!;
        var inactiveBg = (Brush)new BrushConverter().ConvertFrom("#1B1925")!;
        var inactiveRadioBg = (Brush)new BrushConverter().ConvertFrom("#343245")!;

        if (mode == IslandAppearance.FullNotch)
        {
            NotchCard.Background = activeBg;
            NotchCard.BorderBrush = purpleBorder;
            NotchCard.BorderThickness = new Thickness(1.5);
            NotchRadio.Background = purpleBorder;
            NotchRadioDot.Visibility = Visibility.Visible;

            FloatingCard.Background = inactiveBg;
            FloatingCard.BorderBrush = darkBorder;
            FloatingCard.BorderThickness = new Thickness(1);
            FloatingRadio.Background = inactiveRadioBg;
            FloatingRadioDot.Visibility = Visibility.Collapsed;
        }
        else
        {
            FloatingCard.Background = activeBg;
            FloatingCard.BorderBrush = purpleBorder;
            FloatingCard.BorderThickness = new Thickness(1.5);
            FloatingRadio.Background = purpleBorder;
            FloatingRadioDot.Visibility = Visibility.Visible;

            NotchCard.Background = inactiveBg;
            NotchCard.BorderBrush = darkBorder;
            NotchCard.BorderThickness = new Thickness(1);
            NotchRadio.Background = inactiveRadioBg;
            NotchRadioDot.Visibility = Visibility.Collapsed;
        }
    }

    private void WidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_isLoaded) return;

        int percent = (int)WidthSlider.Value;
        WidthScaleText.Text = $"Scale: {percent}%";
        _settings.WidthScale = percent / 100.0;
        _settings.Save();
        _islandWindow.ApplySettings();
    }

    private void BounceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_isLoaded) return;

        int bounce = (int)BounceSlider.Value;
        BounceText.Text = $"Bounce: {bounce}";
        _settings.SpringBounce = bounce;
        _settings.Save();
    }

    private void StartupToggle_Click(object sender, MouseButtonEventArgs e)
    {
        _settings.StartWithWindows = !_settings.StartWithWindows;
        StartupManager.SetStartup(_settings.StartWithWindows);
        UpdateStartupToggleUI(_settings.StartWithWindows);
        _settings.Save();
    }

    private void UpdateStartupToggleUI(bool enabled)
    {
        var activeBg = (Brush)new BrushConverter().ConvertFrom("#7C5BF6")!;
        var inactiveBg = (Brush)new BrushConverter().ConvertFrom("#343245")!;

        if (enabled)
        {
            StartupToggleTrack.Background = activeBg;
            StartupToggleThumb.HorizontalAlignment = HorizontalAlignment.Right;
            StartupToggleThumb.Margin = new Thickness(0, 0, 3, 0);
        }
        else
        {
            StartupToggleTrack.Background = inactiveBg;
            StartupToggleThumb.HorizontalAlignment = HorizontalAlignment.Left;
            StartupToggleThumb.Margin = new Thickness(3, 0, 0, 0);
        }
    }

    // Bottom tab interactions
    private void HomeTab_Click(object sender, MouseButtonEventArgs e)
    {
        _islandWindow.Collapse(animate: true);
    }

    private void ShowHideTab_Click(object sender, MouseButtonEventArgs e)
    {
        _islandWindow.Visibility = _islandWindow.Visibility == Visibility.Visible
            ? Visibility.Hidden
            : Visibility.Visible;
    }

    private void ClickPillTab_Click(object sender, MouseButtonEventArgs e)
    {
        _islandWindow.Expand(animate: true);
    }

    private void ExpandTab_Click(object sender, MouseButtonEventArgs e)
    {
        _islandWindow.Expand(animate: true);
    }

    private void DemoMusicTab_Click(object sender, MouseButtonEventArgs e)
    {
        _mediaService.IsDemoMode = !_mediaService.IsDemoMode;
        DemoMusicTab.Text = _mediaService.IsDemoMode ? "⏹ Stop Demo" : "🎵 Demo Song";
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        // Don't terminate the process when user clicks ✕, just hide window
        e.Cancel = true;
        Hide();
    }
}
