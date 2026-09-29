using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Windows.Media.Control;
using Windows.Storage.Streams;
using DynamicIslandApp.Models;

namespace DynamicIslandApp.Services;

public class WindowsMediaService
{
    private GlobalSystemMediaTransportControlsSessionManager? _manager;
    private GlobalSystemMediaTransportControlsSession? _currentSession;
    private readonly DispatcherTimer _pollTimer;
    private readonly DispatcherTimer _demoTimer;
    private bool _isDemoMode = false;
    private TimeSpan _demoPosition = TimeSpan.Zero;
    private bool _demoIsPlaying = true;
    private ImageSource? _defaultAlbumArt;

    public event Action<MediaTrackInfo>? MediaChanged;

    public MediaTrackInfo CurrentTrack { get; private set; } = new();

    public bool IsDemoMode
    {
        get => _isDemoMode;
        set
        {
            _isDemoMode = value;
            if (_isDemoMode)
            {
                LoadDemoTrack();
            }
            else
            {
                _ = RefreshRealMediaAsync();
            }
        }
    }

    public WindowsMediaService()
    {
        CreateDefaultAlbumArt();

        _pollTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _pollTimer.Tick += async (s, e) =>
        {
            if (!_isDemoMode)
            {
                await RefreshRealMediaAsync();
            }
        };

        _demoTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _demoTimer.Tick += (s, e) =>
        {
            if (_isDemoMode && _demoIsPlaying)
            {
                _demoPosition = _demoPosition.Add(TimeSpan.FromSeconds(1));
                if (_demoPosition > CurrentTrack.Duration)
                {
                    _demoPosition = TimeSpan.Zero;
                }
                CurrentTrack.Position = _demoPosition;
                MediaChanged?.Invoke(CurrentTrack);
            }
        };

        InitializeAsync();
    }

    private void CreateDefaultAlbumArt()
    {
        try
        {
            // Create a stylish dark album art placeholder bitmap
            int size = 120;
            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                var bgBrush = new LinearGradientBrush(
                    Color.FromRgb(30, 30, 45),
                    Color.FromRgb(15, 15, 20),
                    new Point(0, 0),
                    new Point(1, 1));
                dc.DrawRoundedRectangle(bgBrush, null, new Rect(0, 0, size, size), 12, 12);

                // Draw stylish vinyl / music icon in center
                var circleBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
                dc.DrawEllipse(null, new Pen(circleBrush, 3), new Point(size / 2.0, size / 2.0), 36, 36);
                dc.DrawEllipse(null, new Pen(circleBrush, 2), new Point(size / 2.0, size / 2.0), 24, 24);
                dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(120, 90, 255)), null, new Point(size / 2.0, size / 2.0), 12, 12);
            }

            var rtb = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(visual);
            rtb.Freeze();
            _defaultAlbumArt = rtb;
        }
        catch
        {
            _defaultAlbumArt = null;
        }
    }

    private async void InitializeAsync()
    {
        try
        {
            _manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            if (_manager != null)
            {
                _manager.CurrentSessionChanged += OnCurrentSessionChanged;
                await RefreshRealMediaAsync();
                _pollTimer.Start();
            }
            else
            {
                LoadDemoTrack();
            }
        }
        catch
        {
            LoadDemoTrack();
        }
    }

    private async void OnCurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
    {
        await Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            if (!_isDemoMode)
            {
                await RefreshRealMediaAsync();
            }
        });
    }

    public async Task RefreshRealMediaAsync()
    {
        if (_manager == null || _isDemoMode) return;

        try
        {
            var session = _manager.GetCurrentSession();
            if (session != null)
            {
                _currentSession = session;
                var mediaProps = await session.TryGetMediaPropertiesAsync();
                var playbackInfo = session.GetPlaybackInfo();
                var timeline = session.GetTimelineProperties();

                if (mediaProps != null && !string.IsNullOrEmpty(mediaProps.Title))
                {
                    ImageSource? thumbnail = null;
                    if (mediaProps.Thumbnail != null)
                    {
                        thumbnail = await LoadThumbnailAsync(mediaProps.Thumbnail);
                    }

                    CurrentTrack = new MediaTrackInfo
                    {
                        HasMedia = true,
                        Title = mediaProps.Title,
                        Artist = string.IsNullOrEmpty(mediaProps.Artist) ? "Windows Media" : mediaProps.Artist,
                        Album = mediaProps.AlbumTitle ?? string.Empty,
                        AlbumArt = thumbnail ?? _defaultAlbumArt,
                        IsPlaying = playbackInfo?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing,
                        Duration = timeline?.EndTime ?? TimeSpan.FromMinutes(4),
                        Position = timeline?.Position ?? TimeSpan.Zero,
                        SourceApp = session.SourceAppUserModelId ?? "System"
                    };

                    MediaChanged?.Invoke(CurrentTrack);
                    return;
                }
            }

            // No active session or paused/empty
            CurrentTrack = new MediaTrackInfo
            {
                HasMedia = false,
                Title = string.Empty,
                Artist = string.Empty,
                AlbumArt = _defaultAlbumArt,
                IsPlaying = false
            };
            MediaChanged?.Invoke(CurrentTrack);
        }
        catch
        {
            // On exception, keep track as no media
            CurrentTrack = new MediaTrackInfo { HasMedia = false };
            MediaChanged?.Invoke(CurrentTrack);
        }
    }

    private static async Task<BitmapImage?> LoadThumbnailAsync(IRandomAccessStreamReference thumbnailRef)
    {
        try
        {
            using var stream = await thumbnailRef.OpenReadAsync();
            using var netStream = stream.AsStreamForRead();
            using var memoryStream = new MemoryStream();
            await netStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = memoryStream;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    public void LoadDemoTrack()
    {
        _isDemoMode = true;
        _demoIsPlaying = true;
        _demoPosition = TimeSpan.FromMinutes(1).Add(TimeSpan.FromSeconds(24));

        CurrentTrack = new MediaTrackInfo
        {
            HasMedia = true,
            Title = "y2mate.com - Nhan Vi Tieng Chuong",
            Artist = "Khiem Phan",
            Album = "Nhạc Lofi Chill",
            AlbumArt = _defaultAlbumArt,
            IsPlaying = true,
            Duration = TimeSpan.FromMinutes(39),
            Position = _demoPosition,
            SourceApp = "Demo Player"
        };

        _demoTimer.Start();
        MediaChanged?.Invoke(CurrentTrack);
    }

    public async Task TogglePlayPauseAsync()
    {
        if (_isDemoMode)
        {
            _demoIsPlaying = !_demoIsPlaying;
            CurrentTrack.IsPlaying = _demoIsPlaying;
            MediaChanged?.Invoke(CurrentTrack);
            return;
        }

        if (_currentSession != null)
        {
            try
            {
                await _currentSession.TryTogglePlayPauseAsync();
                await RefreshRealMediaAsync();
            }
            catch { }
        }
    }

    public async Task SkipNextAsync()
    {
        if (_isDemoMode)
        {
            _demoPosition = TimeSpan.Zero;
            CurrentTrack.Position = _demoPosition;
            MediaChanged?.Invoke(CurrentTrack);
            return;
        }

        if (_currentSession != null)
        {
            try
            {
                await _currentSession.TrySkipNextAsync();
                await RefreshRealMediaAsync();
            }
            catch { }
        }
    }

    public async Task SkipPreviousAsync()
    {
        if (_isDemoMode)
        {
            _demoPosition = TimeSpan.Zero;
            CurrentTrack.Position = _demoPosition;
            MediaChanged?.Invoke(CurrentTrack);
            return;
        }

        if (_currentSession != null)
        {
            try
            {
                await _currentSession.TrySkipPreviousAsync();
                await RefreshRealMediaAsync();
            }
            catch { }
        }
    }

    public async Task SeekAsync(double ratio)
    {
        if (ratio < 0) ratio = 0;
        if (ratio > 1) ratio = 1;

        var targetTime = TimeSpan.FromSeconds(CurrentTrack.Duration.TotalSeconds * ratio);

        if (_isDemoMode)
        {
            _demoPosition = targetTime;
            CurrentTrack.Position = _demoPosition;
            MediaChanged?.Invoke(CurrentTrack);
            return;
        }

        if (_currentSession != null)
        {
            try
            {
                await _currentSession.TryChangePlaybackPositionAsync((long)(targetTime.TotalMilliseconds * 10000));
                await RefreshRealMediaAsync();
            }
            catch { }
        }
    }
}
