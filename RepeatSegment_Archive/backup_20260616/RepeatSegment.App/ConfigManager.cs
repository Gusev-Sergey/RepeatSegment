using System.IO;

namespace RepeatSegment.App;

/// <summary>
/// Configuration manager — loads settings from config.ini and .env files.
/// Mirrors Python's settings/config_manager.py.
/// Uses ConfigurationManager (System.Configuration) for INI parsing.
/// </summary>
public class ConfigManager
{
    public string SourcePath { get; }
    public string ConfigPath { get; }
    public string EnvPath { get; }

    // ---- Settings section ----
    public string Path { get; set; }
    public string FileName { get; set; } = "";
    public double Position { get; set; }
    public int Counter { get; set; }
    public int MinSilenceLenMs { get; set; } = 400;

    // ---- Transcription section ----
    public List<string> ProvidersEnabled { get; set; } = new() { "yandex" };
    public string VkCloudToken { get; set; } = "";
    public string YandexApiKey { get; set; } = "";
    public string YandexFolderId { get; set; } = "";
    public string SaluteClientId { get; set; } = "";
    public string SaluteClientSecret { get; set; } = "";
    public string SaluteScope { get; set; } = "SALUTE_SPEECH_PERS";
    public string SaluteLang { get; set; } = "ru-RU";
    public string SaluteProfileId { get; set; } = "";
    public string AssemblyAiApiKey { get; set; } = "";
    public string DeepgramApiKey { get; set; } = "";
    public string YandexTranslateApiKey { get; set; } = "";
    public string YandexTranslateFolderId { get; set; } = "";
    public int ChunkMinutes { get; set; } = 10;
    public double PlaybackLatency { get; set; } = 0.32;

    // Deprecated
    public string Provider { get; set; } = "";

    public ConfigManager(string sourcePath)
    {
        SourcePath = sourcePath;
        ConfigPath = System.IO.Path.Combine(sourcePath, "config.ini");
        EnvPath = System.IO.Path.Combine(sourcePath, ".env");
        Path = sourcePath;
    }

    /// <summary>
    /// Load configuration from config.ini. Returns true on success.
    /// </summary>
    public bool Load()
    {
        if (!File.Exists(ConfigPath))
        {
            Log.Info("[INFO] config.ini not found — using defaults");
            return false;
        }

        try
        {
            // Read INI file manually (ConfigurationManager doesn't easily support
            // custom paths for .NET Core — we parse manually)
            var ini = ParseIniFile(ConfigPath);

            // Settings section
            if (ini.TryGetValue("Settings", out var settings))
            {
                Path = GetValue(settings, "path", SourcePath);
                FileName = GetValue(settings, "file", "");
                Position = GetDouble(settings, "position", 0.0);
                Counter = GetInt(settings, "counter", 0);
                MinSilenceLenMs = GetInt(settings, "split_interval", 400);

                Log.Info($"[INFO] Config loaded: file={FileName}, position={Position:F1}, counter={Counter}");
            }
            else
            {
                Log.Info("[WARN] [Settings] section not found in config.ini");
            }

            // Transcription section
            if (ini.TryGetValue("Transcription", out var trans))
            {
                var rawProviders = GetValue(trans, "providers_enabled", "yandex");
                ProvidersEnabled = rawProviders
                    .Split(',')
                    .Select(p => p.Trim())
                    .Where(p => !string.IsNullOrEmpty(p))
                    .ToList();

                // Backward compatibility
                if (ProvidersEnabled.Count == 0)
                {
                    var oldProvider = GetValue(trans, "provider", "");
                    if (oldProvider == "auto")
                        ProvidersEnabled = new List<string> { "vkcloud", "yandex" };
                    else if (oldProvider is "vkcloud" or "yandex")
                        ProvidersEnabled = new List<string> { oldProvider };
                }

                Provider = GetValue(trans, "provider", "");
                VkCloudToken = GetValue(trans, "vkcloud_token", "");
                YandexApiKey = GetValue(trans, "yandex_api_key", "");
                YandexFolderId = GetValue(trans, "yandex_folder_id", "");
                SaluteClientId = GetValue(trans, "salute_client_id", "");
                SaluteClientSecret = GetValue(trans, "salute_client_secret", "");
                SaluteScope = string.IsNullOrEmpty(GetValue(trans, "salute_scope", "SALUTE_SPEECH_PERS"))
                    ? "SALUTE_SPEECH_PERS"
                    : GetValue(trans, "salute_scope", "SALUTE_SPEECH_PERS");
                SaluteLang = string.IsNullOrEmpty(GetValue(trans, "salute_lang", "ru-RU"))
                    ? "ru-RU"
                    : GetValue(trans, "salute_lang", "ru-RU");
                SaluteProfileId = GetValue(trans, "salute_profile_id", "");
                AssemblyAiApiKey = GetValue(trans, "assemblyai_api_key", "");
                DeepgramApiKey = GetValue(trans, "deepgram_api_key", "");
                YandexTranslateApiKey = GetValue(trans, "yandex_translate_api_key", "");
                YandexTranslateFolderId = GetValue(trans, "yandex_translate_folder_id", "");
                ChunkMinutes = GetInt(trans, "chunk_minutes", 10);
                PlaybackLatency = GetDouble(trans, "playback_latency", 0.32);

                Log.Info($"[INFO] Transcription config: providers={string.Join(",", ProvidersEnabled)}, chunk_minutes={ChunkMinutes}");
            }

            // Override with .env file (if exists)
            var env = LoadEnvFile(EnvPath);
            if (env.Count > 0)
            {
                if (env.TryGetValue("VKCLOUD_TOKEN", out var vk)) VkCloudToken = vk;
                if (env.TryGetValue("YANDEX_API_KEY", out var yak)) YandexApiKey = yak;
                if (env.TryGetValue("YANDEX_FOLDER_ID", out var yfid)) YandexFolderId = yfid;
                if (env.TryGetValue("SALUTE_CLIENT_ID", out var scid)) SaluteClientId = scid;
                if (env.TryGetValue("SALUTE_CLIENT_SECRET", out var scs)) SaluteClientSecret = scs;
                if (env.TryGetValue("ASSEMBLYAI_API_KEY", out var aak)) AssemblyAiApiKey = aak;
                if (env.TryGetValue("DEEPGRAM_API_KEY", out var dk)) DeepgramApiKey = dk;

                Log.Info("[INFO] API keys overridden from .env");
            }

            return true;
        }
        catch (Exception ex)
        {
            Log.Info($"[ERROR] Error reading config.ini: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Save current state to config.ini, including API keys edited in SettingsWindow.
    /// </summary>
    public void Save(string path, string fileName, double position, int counter)
    {
        try
        {
            var lines = new List<string>
            {
                "[Settings]",
                $"path = {path}",
                $"file = {fileName}",
                $"position = {position}",
                $"counter = {counter}",
                $"split_interval = {MinSilenceLenMs}",
                "",
                "[Transcription]",
                $"providers_enabled = {string.Join(",", ProvidersEnabled)}",
                $"vkcloud_token = {VkCloudToken}",
                $"yandex_api_key = {YandexApiKey}",
                $"yandex_folder_id = {YandexFolderId}",
                $"salute_client_id = {SaluteClientId}",
                $"salute_client_secret = {SaluteClientSecret}",
                $"salute_scope = {SaluteScope}",
                $"salute_lang = {SaluteLang}",
                $"salute_profile_id = {SaluteProfileId}",
                $"assemblyai_api_key = {AssemblyAiApiKey}",
                $"deepgram_api_key = {DeepgramApiKey}",
                $"yandex_translate_api_key = {YandexTranslateApiKey}",
                $"yandex_translate_folder_id = {YandexTranslateFolderId}",
                $"chunk_minutes = {ChunkMinutes}",
                $"playback_latency = {PlaybackLatency}",
                ""
            };

            File.WriteAllLines(ConfigPath, lines, System.Text.Encoding.UTF8);
            Log.Info($"[INFO] Config saved: file={fileName}, position={position:F1}, counter={counter}");
        }
        catch (Exception ex)
        {
            Log.Info($"[ERROR] Error saving config.ini: {ex.Message}");
        }
    }

    // ---------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------

    /// <summary>Parse a simple INI file into a dictionary of sections.</summary>
    private static Dictionary<string, Dictionary<string, string>> ParseIniFile(string filePath)
    {
        var result = new Dictionary<string, Dictionary<string, string>>();
        string? currentSection = null;

        foreach (var line in File.ReadAllLines(filePath, System.Text.Encoding.UTF8))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#') || trimmed.StartsWith(';'))
                continue;

            if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
            {
                currentSection = trimmed[1..^1];
                if (!result.ContainsKey(currentSection))
                    result[currentSection] = new Dictionary<string, string>();
                continue;
            }

            var eqIdx = trimmed.IndexOf('=');
            if (eqIdx < 0 || currentSection == null)
                continue;

            var key = trimmed[..eqIdx].Trim();
            var val = trimmed[(eqIdx + 1)..].Trim();
            result[currentSection][key] = val;
        }

        return result;
    }

    /// <summary>Load .env file into a dictionary (no python-dotnet dependency).</summary>
    private static Dictionary<string, string> LoadEnvFile(string envPath)
    {
        var result = new Dictionary<string, string>();
        if (!File.Exists(envPath))
            return result;

        try
        {
            foreach (var line in File.ReadAllLines(envPath, System.Text.Encoding.UTF8))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#') || !trimmed.Contains('='))
                    continue;

                var eqIdx = trimmed.IndexOf('=');
                var key = trimmed[..eqIdx].Trim();
                var val = trimmed[(eqIdx + 1)..].Trim().Trim('"').Trim('\'');

                if (!string.IsNullOrEmpty(key))
                    result[key] = val;
            }

            if (result.Count > 0)
                Log.Info($"[INFO] Loaded {result.Count} variables from {envPath}");
        }
        catch (Exception ex)
        {
            Log.Info($"[WARN] Error loading .env: {ex.Message}");
        }

        return result;
    }

    private static string GetValue(Dictionary<string, string> section, string key, string defaultValue)
    {
        return section.TryGetValue(key, out var val) ? val : defaultValue;
    }

    private static int GetInt(Dictionary<string, string> section, string key, int defaultValue)
    {
        if (section.TryGetValue(key, out var val) && int.TryParse(val, out var result))
            return result;
        return defaultValue;
    }

    private static double GetDouble(Dictionary<string, string> section, string key, double defaultValue)
    {
        if (section.TryGetValue(key, out var val) &&
            double.TryParse(val, System.Globalization.NumberStyles.Float,
                           System.Globalization.CultureInfo.InvariantCulture, out var result))
            return result;
        return defaultValue;
    }
}