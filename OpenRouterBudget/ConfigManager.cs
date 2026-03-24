using System.Text.Json;

namespace OpenRouterBudget;

public static class ConfigManager
{
    public static string LoadApiKey(string appDir)
    {
        // Try config.json next to the exe
        string configPath = Path.Combine(appDir, "config.json");

        // Also try in AppData
        string appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OpenRouterBudget", "config.json");

        foreach (var path in new[] { configPath, appDataPath })
        {
            try
            {
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("api_key", out var keyProp))
                    {
                        string? key = keyProp.GetString();
                        if (!string.IsNullOrEmpty(key) && key != "sk-or-v1-PASTE_YOUR_KEY_HERE")
                            return key;
                    }
                }
            }
            catch { }
        }

        // Try environment variable
        string? envKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
        if (!string.IsNullOrEmpty(envKey))
            return envKey;

        return string.Empty;
    }

    public static void CreateDefaultConfig(string appDir)
    {
        string configPath = Path.Combine(appDir, "config.json");
        if (!File.Exists(configPath))
        {
            File.WriteAllText(configPath,
                JsonSerializer.Serialize(new { api_key = "sk-or-v1-PASTE_YOUR_KEY_HERE" },
                new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
