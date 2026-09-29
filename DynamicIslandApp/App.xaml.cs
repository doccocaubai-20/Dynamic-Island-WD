using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using DynamicIslandApp.Models;
using DynamicIslandApp.Services;
using Application = System.Windows.Application;

namespace DynamicIslandApp;

public partial class App : Application
{
    private static App? _currentApp;
    private NotifyIcon? _notifyIcon;
    private IslandSettings? _settings;
    private WindowsMediaService? _mediaService;
    private IslandWindow? _islandWindow;
    private SettingsWindow? _settingsWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _currentApp = this;

        // 1. Load Settings
        _settings = IslandSettings.Load();

        // 2. Start Media Service
        _mediaService = new WindowsMediaService();

        // 3. Create Island Window
        _islandWindow = new IslandWindow(_settings, _mediaService);
        _islandWindow.Show();

        // 4. Create Settings Window (hidden initially)
        _settingsWindow = new SettingsWindow(_settings, _islandWindow, _mediaService);

        // 5. Setup System Tray Icon
        SetupNotifyIcon();
    }

    private void SetupNotifyIcon()
    {
        try
        {
            // Create a small pill icon procedurally for the tray
            var bmp = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                using var brush = new SolidBrush(Color.FromArgb(124, 91, 246));
                g.FillRoundedRectangle(brush, 4, 10, 24, 12, 6);
                using var innerBrush = new SolidBrush(Color.White);
                g.FillEllipse(innerBrush, 8, 13, 6, 6);
            }
            var iconHandle = bmp.GetHicon();
            var icon = Icon.FromHandle(iconHandle);

            _notifyIcon = new NotifyIcon
            {
                Icon = icon,
                Text = "Dynamic Island cho Windows",
                Visible = true
            };

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("🏝 Dynamic Island cho Windows", null, (s, e) => _islandWindow?.Expand(true));
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("⚙ Cài đặt (Settings)", null, (s, e) => ShowSettings());
            contextMenu.Items.Add("🎵 Bật/Tắt Nhạc Demo", null, (s, e) =>
            {
                if (_mediaService != null)
                {
                    _mediaService.IsDemoMode = !_mediaService.IsDemoMode;
                }
            });
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("❌ Thoát ứng dụng", null, (s, e) => Shutdown());

            _notifyIcon.ContextMenuStrip = contextMenu;
            _notifyIcon.DoubleClick += (s, e) => ShowSettings();
        }
        catch { }
    }

    public static void ShowSettings()
    {
        if (_currentApp?._settingsWindow != null)
        {
            _currentApp._settingsWindow.Show();
            _currentApp._settingsWindow.WindowState = WindowState.Normal;
            _currentApp._settingsWindow.Activate();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }
        _settings?.Save();
        base.OnExit(e);
    }
}

public static class GraphicsExtensions
{
    public static void FillRoundedRectangle(this Graphics g, Brush brush, float x, float y, float width, float height, float radius)
    {
        using var path = new System.Drawing.Drawing2D.GraphicsPath();
        path.AddArc(x, y, radius * 2, radius * 2, 180, 90);
        path.AddArc(x + width - 2 * radius, y, radius * 2, radius * 2, 270, 90);
        path.AddArc(x + width - 2 * radius, y + height - 2 * radius, radius * 2, radius * 2, 0, 90);
        path.AddArc(x, y + height - 2 * radius, radius * 2, radius * 2, 90, 90);
        path.CloseFigure();
        g.FillPath(brush, path);
    }
}
