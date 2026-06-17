using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RepeatSegment.App;

/// <summary>
/// Text-to-Speech provider.
/// Primary: Deepgram Aura TTS (uses existing Deepgram API key).
/// Fallback: Google Translate TTS (free, no key required).
/// Caches results on disk.
/// </summary>
public class TtsProvider
{
    private readonly string? _deepgramApiKey;
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };
    private static readonly string _cacheDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "RepeatSegment", "tts_cache");

    public TtsProvider(string? deepgramApiKey = null)
    {
        _deepgramApiKey = deepgramApiKey;
        Directory.CreateDirectory(_cacheDir);
    }

    /// <summary>
    /// Download TTS MP3 for an English word/phrase.
    /// Returns file path to cached MP3, or null on failure.
    /// </summary>
    public async Task<string?> DownloadTtsMp3(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length < 2)
            return null;

        string cacheKey = Md5Hash(text.Trim().ToLowerInvariant());
        string cachePath = Path.Combine(_cacheDir, cacheKey + ".mp3");

        if (File.Exists(cachePath) && new FileInfo(cachePath).Length > 100)
            return cachePath;

        byte[]? mp3 = null;

        // Primary: Deepgram TTS (if key available)
        if (!string.IsNullOrEmpty(_deepgramApiKey))
        {
            mp3 = await DownloadDeepgramTts(text);
            if (mp3 != null && mp3.Length > 100)
            {
                File.WriteAllBytes(cachePath, mp3);
                return cachePath;
            }
        }

        // Fallback: Google TTS
        mp3 = await DownloadGoogleTts(text);
        if (mp3 != null && mp3.Length > 100)
        {
            File.WriteAllBytes(cachePath, mp3);
            return cachePath;
        }

        return null;
    }

    /// <summary>
    /// Deepgram Aura TTS — https://developers.deepgram.com/docs/text-to-speech
    /// Returns raw audio bytes (MP3 or WAV depending on header).
    /// </summary>
    private async Task<byte[]?> DownloadDeepgramTts(string text)
    {
        if (string.IsNullOrEmpty(_deepgramApiKey)) return null;

        for (int attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                if (attempt > 0) await Task.Delay(500);
                // Deepgram Aura: voice model, returns audio/mpeg
                string url = "https://api.deepgram.com/v1/speak?model=aura-asteria-en";
                var req = new HttpRequestMessage(HttpMethod.Post, url);
                req.Headers.Authorization = new AuthenticationHeaderValue("Token", _deepgramApiKey);
                req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("audio/mpeg"));
                req.Content = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(new { text }),
                    Encoding.UTF8, "application/json");

                var resp = await _http.SendAsync(req);
                if (!resp.IsSuccessStatusCode) continue;

                byte[] audio = await resp.Content.ReadAsByteArrayAsync();
                if (audio.Length > 100) return audio;
            }
            catch { /* retry */ }
        }
        return null;
    }

    /// <summary>Google Translate TTS — free, no API key.</summary>
    private async Task<byte[]?> DownloadGoogleTts(string text)
    {
        for (int attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                if (attempt > 0) await Task.Delay(500 * attempt);
                string encoded = Uri.EscapeDataString(text);
                string url = $"https://translate.google.com/translate_tts?ie=UTF-8&client=gtx&tl=en&q={encoded}";
                var resp = await _http.GetByteArrayAsync(url);
                if (resp.Length > 100) return resp;
            }
            catch { }
        }
        return null;
    }

    private static string Md5Hash(string input)
    {
        byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
