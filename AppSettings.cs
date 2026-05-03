using System.IO;
using System.Text.Json;

namespace MultiStreamVlc;

public class AppSettings
{
    private static readonly string SettingsPath = Path.Combine(AppContext.BaseDirectory, "settings.json");

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = RandomPort();

    public static int RandomPort()
    {
        return Random.Shared.Next(49152, 65536);
    }

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch { }

        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch { }
    }
}
