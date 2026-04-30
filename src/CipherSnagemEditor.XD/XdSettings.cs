using System.Text.Json;
using System.Text.Json.Serialization;

namespace CipherSnagemEditor.XD;

public sealed class XdSettings
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    [JsonPropertyName("The default duration (seconds) for button inputs when running Dolphin")]
    public double DppInputDuration { get; set; } = 0.18;

    [JsonPropertyName("Enable Experimental Features")]
    public bool EnableExperimentalFeatures { get; set; }

    [JsonPropertyName("Verbose Logs")]
    public bool VerboseLogs { get; set; }

    [JsonPropertyName("Increase File Sizes")]
    public bool IncreaseFileSizes { get; set; } = true;

    public static XdSettings LoadOrCreate(string workspaceDirectory)
    {
        Directory.CreateDirectory(workspaceDirectory);
        var path = SettingsPath(workspaceDirectory);
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<XdSettings>(json, JsonOptions) ?? new XdSettings();
        }

        var settings = new XdSettings();
        settings.Save(workspaceDirectory);
        return settings;
    }

    public void Save(string workspaceDirectory)
    {
        Directory.CreateDirectory(workspaceDirectory);
        File.WriteAllText(SettingsPath(workspaceDirectory), JsonSerializer.Serialize(this, JsonOptions));
    }

    public static string SettingsPath(string workspaceDirectory) => Path.Combine(workspaceDirectory, "Settings.json");
}
