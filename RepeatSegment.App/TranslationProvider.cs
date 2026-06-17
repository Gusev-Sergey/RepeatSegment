using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace RepeatSegment.App;

/// <summary>
/// Word/phrase translation provider.
/// Supports Google Translate (free, no API key) and Yandex.Translate (API key required).
/// Provider choice is controlled via TranslationProviderPreference in config.ini.
/// </summary>
public class TranslationProvider
{
    private readonly string? _yandexKey;
    private readonly string? _yandexFolderId;
    private readonly string _preference; // "google" or "yandex"

    /// <summary>Preferred provider from config ("google" or "yandex").</summary>
    public string ActiveProvider => _preference;

    /// <summary>Which provider actually returned the last result (null if last call failed).</summary>
    public string? LastUsedProvider { get; private set; }

    public TranslationProvider(string? yandexKey, string? yandexFolderId, string preference = "google")
    {
        _yandexKey = yandexKey;
        _yandexFolderId = yandexFolderId;
        _preference = (preference == "yandex") ? "yandex" : "google";
    }

    /// <summary>
    /// Translate text from English to Russian.
    /// When preference is "google": tries Google first, falls back to Yandex.
    /// When preference is "yandex": tries Yandex first, falls back to Google.
    /// </summary>
    public async Task<string> TranslateEnRu(string text)
    {
        LastUsedProvider = null;

        if (_preference == "yandex")
        {
            // Prefer Yandex first, then fall back to Google
            string? yandexResult = await TryYandexTranslate(text);
            if (yandexResult != null)
            {
                LastUsedProvider = "yandex";
                return yandexResult;
            }

            string? googleResult = await TryGoogleTranslate(text);
            if (googleResult != null)
            {
                LastUsedProvider = "google (fallback)";
                return googleResult;
            }
        }
        else
        {
            // Prefer Google first (default), then fall back to Yandex
            string? googleResult = await TryGoogleTranslate(text);
            if (googleResult != null)
            {
                LastUsedProvider = "google";
                return googleResult;
            }

            string? yandexResult = await TryYandexTranslate(text);
            if (yandexResult != null)
            {
                LastUsedProvider = "yandex (fallback)";
                return yandexResult;
            }
        }

        return "⚠ Translation unavailable";
    }

    private async Task<string?> TryGoogleTranslate(string text)
    {
        for (int attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                if (attempt > 0) await Task.Delay(500 * attempt);
                string encoded = Uri.EscapeDataString(text);
                string url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl=en&tl=ru&dt=t&q={encoded}";
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                string response = await http.GetStringAsync(url);

                // Response: [[["sent1","orig1",...],["sent2","orig2",...],...],null,"en"]
                using var doc = JsonDocument.Parse(response);
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
                {
                    var first = root[0];
                    if (first.ValueKind == JsonValueKind.Array && first.GetArrayLength() > 0)
                    {
                        var parts = new System.Collections.Generic.List<string>();
                        foreach (var item in first.EnumerateArray())
                        {
                            if (item.ValueKind == JsonValueKind.Array && item.GetArrayLength() > 0)
                            {
                                string? part = item[0].GetString();
                                if (!string.IsNullOrEmpty(part))
                                    parts.Add(part);
                            }
                        }
                        if (parts.Count > 0)
                            return string.Join("", parts);
                    }
                }
            }
            catch { /* retry */ }
        }
        return null;
    }

    private async Task<string?> TryYandexTranslate(string text)
    {
        if (string.IsNullOrEmpty(_yandexKey) || string.IsNullOrEmpty(_yandexFolderId))
            return null;

        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var body = JsonSerializer.Serialize(new
            {
                sourceLanguageCode = "en",
                targetLanguageCode = "ru",
                texts = new[] { text },
                folderId = _yandexFolderId
            });
            var req = new HttpRequestMessage(HttpMethod.Post,
                "https://translate.api.cloud.yandex.net/translate/v2/translate")
            {
                Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
            };
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Api-Key", _yandexKey);
            var resp = await http.SendAsync(req);
            string respBody = await resp.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(respBody);
            var root = doc.RootElement;
            if (root.TryGetProperty("translations", out var trs) && trs.ValueKind == JsonValueKind.Array && trs.GetArrayLength() > 0)
            {
                string? translated = trs[0].TryGetProperty("text", out var tt) ? tt.GetString() : null;
                if (!string.IsNullOrEmpty(translated))
                    return translated;
            }
        }
        catch { }
        return null;
    }
}
