using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace RepeatSegment.App;

/// <summary>
/// Word/phrase translation provider.
/// Uses Google Translate (MyMemory endpoint — no API key, free) with Yandex.Translate as fallback.
/// </summary>
public class TranslationProvider
{
    private readonly string? _yandexKey;
    private readonly string? _yandexFolderId;

    public TranslationProvider(string? yandexKey, string? yandexFolderId)
    {
        _yandexKey = yandexKey;
        _yandexFolderId = yandexFolderId;
    }

    /// <summary>
    /// Translate text from English to Russian. Tries free Google endpoint first, falls back to Yandex.
    /// </summary>
    public async Task<string> TranslateEnRu(string text)
    {
        // Try free Google Translate with retry (rate-limit resilience)
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
            // For multi-sentence text, Google splits into sentence-level arrays — join all
            using var doc = JsonDocument.Parse(response);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
            {
                var first = root[0]; // array of sentence arrays
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
            catch { /* fall through to Yandex */ }
        }

        // Fallback: Yandex.Translate
        if (!string.IsNullOrEmpty(_yandexKey) && !string.IsNullOrEmpty(_yandexFolderId))
        {
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
        }

        return $"⚠ Translation unavailable";
    }
}
