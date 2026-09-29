using System;
using System.IO;
using System.Text.Json;

namespace DynamicIslandApp.Models;

public enum IslandAppearance
{
    FloatingPill,
    FullNotch
}

public class IslandSettings
{
    public IslandAppearance Appearance { get; set; } = IslandAppearance.FullNotch;
    public double WidthScale { get; set; } = 1.0; // 100%
    public double SpringBounce { get; set; } = 100; // 100
    public bool StartWithWindows { get; set; } = false;
    public bool DemoModeEnabled { get; set; } = true;

    private static readonly string SettingsFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "DynamicIslandWindows");

    private static readonly string SettingsFile = Path.Combine(SettingsFolder, "settings.json");

    public static IslandSettings Load()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                var json = File.ReadAllText(SettingsFile);
                var settings = JsonSerializer.Deserialize<IslandSettings>(json);
                if (settings != null) return settings;
            }
        }
        catch { }
        return new IslandSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(SettingsFolder);
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFile, json);
        }
        catch { }
    }
}
