using System.Text.Json;
using System.Text.Json.Serialization;

namespace OrreForge.Colosseum;

public sealed class ColosseumSettings
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

    public static ColosseumSettings LoadOrCreate(string workspaceDirectory)
    {
        Directory.CreateDirectory(workspaceDirectory);
        var path = SettingsPath(workspaceDirectory);
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<ColosseumSettings>(json, JsonOptions) ?? new ColosseumSettings();
        }

        var settings = new ColosseumSettings();
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
