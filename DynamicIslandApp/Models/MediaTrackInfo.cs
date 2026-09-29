using System;
using System.Windows.Media;

namespace DynamicIslandApp.Models;

public class MediaTrackInfo
{
    public bool HasMedia { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string Album { get; set; } = string.Empty;
    public ImageSource? AlbumArt { get; set; }
    public bool IsPlaying { get; set; }
    public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(39);
    public TimeSpan Position { get; set; } = TimeSpan.Zero;
    public string SourceApp { get; set; } = string.Empty;
}
