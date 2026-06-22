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
    public double SegmentDurationSec { get; set; } = 5.0;
    public string Language { get; set; } = ""; // "en" or "ru" — empty = first run
    public string Theme { get; set; } = "light"; // "light" or "dark"

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
    public string TranslationProviderPreference { get; set; } = "google";
    public string TranscriptionLanguage { get; set; } = "en";
    public int ChunkMinutes { get; set; } = 10;
    public double PlaybackLatency { get; set; } = 0.32;
    public int Mp3BitrateKbps { get; set; } = 128;
    public string ImageSearchProvider { get; set; } = "google";

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
            Log.Info("[INFO] No config.ini found, using defaults");
            return true;
        }

        try
        {
            var ini = ParseIniFile(ConfigPath);
            if (ini.TryGetValue("Settings", out var settings))
            {
                Path = GetValue(settings, "path", SourcePath);
                FileName = GetValue(settings, "file", "");
                Position = GetDouble(settings, "position", 0);
                Counter = GetInt(settings, "counter", 0);
                SegmentDurationSec = GetDouble(settings, "segment_duration_sec", 5.0);
                Language = GetValue(settings, "language", "");
                Theme = GetValue(settings, "theme", "light").ToLowerInvariant();
                if (Theme != "light" && Theme != "dark") Theme = "light";
                Log.Info($"[INFO] Config loaded: file={FileName}, position={Position:F1}, counter={Counter}");
            }

            if (ini.TryGetValue("Transcription", out var trans))
            {
                var prov = GetValue(trans, "providers_enabled", "deepgram");
                ProvidersEnabled = prov.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Select(p => p.ToLowerInvariant()).ToList();
                if (ProvidersEnabled.Count == 0) ProvidersEnabled = new() { "deepgram" };
                VkCloudToken = GetValue(trans, "vkcloud_token", "");
                YandexApiKey = GetValue(trans, "yandex_api_key", "");
                YandexFolderId = GetValue(trans, "yandex_folder_id", "");
                SaluteClientId = GetValue(trans, "salute_client_id", "");
                SaluteClientSecret = GetValue(trans, "salute_client_secret", "");
                SaluteScope = GetValue(trans, "salute_scope", "SALUTE_SPEECH_PERS");
                SaluteLang = GetValue(trans, "salute_lang", "ru-RU");
                SaluteProfileId = GetValue(trans, "salute_profile_id", "");
                AssemblyAiApiKey = GetValue(trans, "assemblyai_api_key", "");
                DeepgramApiKey = GetValue(trans, "deepgram_api_key", "");
                YandexTranslateApiKey = GetValue(trans, "yandex_translate_api_key", "");
                YandexTranslateFolderId = GetValue(trans, "yandex_translate_folder_id", "");
                TranslationProviderPreference = GetValue(trans, "translation_provider", "google").ToLowerInvariant();
                TranscriptionLanguage = GetValue(trans, "transcription_language", "en").ToLowerInvariant();
                if (TranslationProviderPreference != "google" && TranslationProviderPreference != "yandex")
                    TranslationProviderPreference = "google";
                ChunkMinutes = GetInt(trans, "chunk_minutes", 10);
                PlaybackLatency = GetDouble(trans, "playback_latency", 0.32);
                Mp3BitrateKbps = GetInt(trans, "mp3_bitrate", 128);
                ImageSearchProvider = GetValue(trans, "image_search_provider", "google").ToLowerInvariant();
                if (ImageSearchProvider != "google" && ImageSearchProvider != "yandex")
                    ImageSearchProvider = "google";

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
            Log.Error($"[ERROR] Failed to load config: {ex.Message}");
            return false;
        }
    }

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
                $"segment_duration_sec = {SegmentDurationSec:F1}",
                $"language = {Language}",
                $"theme = {Theme}",
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
                $"translation_provider = {TranslationProviderPreference}",
                $"transcription_language = {TranscriptionLanguage}",
                $"chunk_minutes = {ChunkMinutes}",
                $"playback_latency = {PlaybackLatency}",
                $"mp3_bitrate = {Mp3BitrateKbps}",
                $"image_search_provider = {ImageSearchProvider}",
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

    private static Dictionary<string, Dictionary<string, string>> ParseIniFile(string filePath)
    {
        var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        string? currentSection = null;
        foreach (var rawLine in File.ReadAllLines(filePath, System.Text.Encoding.UTF8))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith(';') || line.StartsWith('#'))
                continue;
            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                currentSection = line[1..^1].Trim();
                if (!result.ContainsKey(currentSection))
                    result[currentSection] = new(StringComparer.OrdinalIgnoreCase);
                continue;
            }
            int eq = line.IndexOf('=');
            if (eq < 0 || currentSection == null) continue;
            string key = line[..eq].Trim();
            string value = line[(eq + 1)..].Trim();
            result[currentSection][key] = value;
        }
        return result;
    }

    private static Dictionary<string, string> LoadEnvFile(string envPath)
    {
        var result = new Dictionary<string, string>();
        if (!File.Exists(envPath)) return result;
        try
        {
            foreach (var line in File.ReadAllLines(envPath, System.Text.Encoding.UTF8))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#')) continue;
                int eq = trimmed.IndexOf('=');
                if (eq < 0) continue;
                string key = trimmed[..eq].Trim();
                string value = trimmed[(eq + 1)..].Trim();
                if (value.StartsWith('"') && value.EndsWith('"'))
                    value = value[1..^1];
                result[key] = value;
            }
        }
        catch (Exception ex) { Log.Warn($"[WARN] Failed to read .env: {ex.Message}"); }
        return result;
    }

    private static string GetValue(Dictionary<string, string> section, string key, string defaultValue)
    {
        return section.TryGetValue(key, out var v) ? v : defaultValue;
    }

    private static int GetInt(Dictionary<string, string> section, string key, int defaultValue)
    {
        if (section.TryGetValue(key, out var v) && int.TryParse(v, out var r)) return r;
        return defaultValue;
    }

    private static double GetDouble(Dictionary<string, string> section, string key, double defaultValue)
    {
        if (section.TryGetValue(key, out var v) && double.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var r)) return r;
        return defaultValue;
    }
}
