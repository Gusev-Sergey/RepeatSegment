using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RepeatSegment.App;

public class TranscriptionProvider
{
    private const string DeepgramApiUrl = "https://api.deepgram.com/v1/listen";
    private const string AssemblyAiUploadUrl = "https://api.assemblyai.com/v2/upload";
    private const string AssemblyAiTranscriptUrl = "https://api.assemblyai.com/v2/transcript";
    private const int MaxTranscribeWaitSec = 120;
    private const double AssemblyAiPollInterval = 2.0;
    private const int AssemblyAiMaxWait = 300;

    private readonly ConfigManager _cfg;
    private readonly AudioEngine _ae;
    private readonly double _chunkSec;
    private List<(double Start, double End)> _silenceZones = new();
    private readonly Dictionary<int, TranscriptionChunk> _loadedChunks = new();
    private readonly HashSet<int> _transcribing = new();
    private List<(double, double)> _speechFragments = new();
    private readonly object _lock = new();

    public string FullText { get; private set; } = "";
    public List<WordTiming> WordTimings { get; private set; } = new();
    public event Action<string>? StatusChanged;

    public TranscriptionProvider(ConfigManager cfg, AudioEngine ae) { _cfg = cfg; _ae = ae; _chunkSec = cfg.ChunkMinutes * 60; }

    /// <summary>Set silence zones for smart chunk boundary adjustment.</summary>
    public void SetSilenceZones(List<(double Start, double End)> zones) { _silenceZones = zones; }

    public async Task<string> Transcribe(string filePath, bool forceFresh = false)
    {
        lock (_lock) { FullText = ""; WordTimings.Clear(); _loadedChunks.Clear(); _transcribing.Clear(); }
        int totalChunks = (int)Math.Ceiling(_ae.Duration.TotalSeconds / _chunkSec);
        if (totalChunks < 1) totalChunks = 1;

        if (!forceFresh)
        {
            // Load from cache only — do NOT call APIs
            StatusChanged?.Invoke($"Loading {totalChunks} chunks from cache...");
            for (int ci = 0; ci < totalChunks; ci++)
                LoadChunkFromCache(ci);
            int wc; lock (_lock) { wc = WordTimings.Count; }
            if (wc == 0)
                StatusChanged?.Invoke("No cache — use Request from API");
            else
                StatusChanged?.Invoke($"Done — {wc} words from cache");
            return FullText;
        }

        StatusChanged?.Invoke($"Transcribing {totalChunks} chunks...");
        for (int ci = 0; ci < totalChunks; ci++)
        {
            StatusChanged?.Invoke($"Chunk {ci + 1}/{totalChunks}...");
            await TranscribeChunkAsync(ci);
            int w = 0;
            while (!_loadedChunks.ContainsKey(ci) && _transcribing.Contains(ci) && w++ < 360) await Task.Delay(500);
        }
        int wcApi; lock (_lock) { wcApi = WordTimings.Count; }
        StatusChanged?.Invoke($"Done — {wcApi} words");
        return FullText;
    }

    public int GetWordIndexAt(double t) { var w = WordTimings; if (w.Count == 0) return -1; int lo = 0, hi = w.Count - 1, first = -1; while (lo <= hi) { int mid = (lo + hi) / 2; if (w[mid].End > t) { first = mid; hi = mid - 1; } else lo = mid + 1; } if (first < 0) return w.Count - 1; if (t < w[first].Start) return first > 0 ? first - 1 : -1; return first; }

    private void LoadChunkFromCache(int ci)
    {
        var (t1, _) = ChunkRange(ci);
        string cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RepeatSegment", "output");
        if (!Directory.Exists(cacheDir)) return;

        string audioName = Path.GetFileNameWithoutExtension(_ae.FilePath);
        // Try known providers; pick first matching cache file
        foreach (var prov in new[] { "deepgram", "assemblyai" })
        {
            string glob = $"{audioName}_chunk{ci:D4}_{prov}_en.json";
            if (Directory.GetFiles(cacheDir, glob).Length == 0)
                glob = $"{audioName}_chunk{ci:D4}_{prov}_*.json"; // wildcard fallback
            var files = Directory.GetFiles(cacheDir, glob);
            if (files.Length == 0) continue;

            try
            {
                string json = File.ReadAllText(files[0]);
                var result = JsonSerializer.Deserialize<TranscriptionResult>(json);
                if (result?.Words?.Count > 0)
                {
                    MergeChunk(result, ci, t1);
                    return;
                }
            }
            catch { }
        }
    }

    public void EnsureChunk(int ci) { if (_loadedChunks.ContainsKey(ci) || _transcribing.Contains(ci)) return; _transcribing.Add(ci); _ = TranscribeChunkAsync(ci); }

    private (double, double) ChunkRange(int ci)
    {
        double t1 = ci * _chunkSec;
        double t2 = (ci + 1) * _chunkSec;
        // Snap boundaries to nearest silence to avoid cutting words
        if (_silenceZones.Count > 0 && ci > 0)
            t1 = SnapToSilence(t1);
        double dur = _ae.Duration.TotalSeconds;
        if (_silenceZones.Count > 0 && t2 < dur)
            t2 = SnapToSilence(t2);
        return (Math.Max(0, t1 - 0.5), Math.Min(dur, t2 + 0.5)); // 0.5s padding for overlap
    }

    private double SnapToSilence(double boundary)
    {
        double best = boundary;
        double bestDist = 5.0; // max 5 second search radius
        foreach (var (s, e) in _silenceZones)
        {
            double mid = (s + e) / 2;
            double dist = Math.Abs(mid - boundary);
            if (dist < bestDist) { bestDist = dist; best = mid; }
        }
        return best;
    }

    private async Task TranscribeChunkAsync(int ci)
    {
        var (t1, t2) = ChunkRange(ci);
        double dur = _ae.Duration.TotalSeconds;
        if (t1 >= dur) { _transcribing.Remove(ci); return; }
        if (t2 > dur) t2 = dur;

        string? wav;
        try { wav = _ae.ExtractChunk(t1, t2); }
        catch (Exception ex) { StatusChanged?.Invoke($"Extract error: {ex.Message}"); _transcribing.Remove(ci); return; }

        var providers = _cfg.ProvidersEnabled;
        if (providers.Count == 0) providers = new List<string> { "deepgram" };
        StatusChanged?.Invoke($"Trying: {string.Join(",", providers)}");

        TranscriptionResult? result = null;
        string? usedProvider = null;
        foreach (var prov in providers.Select(p => p.ToLowerInvariant()))
        {
            StatusChanged?.Invoke($"Calling {prov}...");
            try
            {
                result = prov switch
                {
                    "deepgram" => await TranscribeDeepgram(wav),
                    "assemblyai" => await TranscribeAssemblyAi(wav),
                    _ => null
                };
                if (result?.Words?.Count > 0) { usedProvider = prov; StatusChanged?.Invoke($"{prov}: {result.Words.Count} words"); break; }
                StatusChanged?.Invoke($"{prov}: empty");
            }
            catch (Exception ex) { StatusChanged?.Invoke($"{prov}: {ex.Message}"); }
        }

        try { File.Delete(wav); } catch { }

        if (result == null) { StatusChanged?.Invoke("All providers failed"); _transcribing.Remove(ci); return; }
        MergeChunk(result, ci, t1);
        SaveChunkToCache(result, ci, usedProvider ?? "unknown");
        _transcribing.Remove(ci);
    }

    private void MergeChunk(TranscriptionResult data, int ci, double offset)
    {
        var words = data.Words ?? new();
        if (words.Count == 0) { lock (_lock) _loadedChunks[ci] = new TranscriptionChunk(data); return; }
        foreach (var w in words) { w.Start += offset; w.End += offset; }
        lock (_lock) { WordTimings.AddRange(words); _loadedChunks[ci] = new TranscriptionChunk(data); FullText = string.Join(" ", WordTimings.Select(w => w.Word)); }
    }

    private void SaveChunkToCache(TranscriptionResult data, int ci, string provider)
    {
        try
        {
            string cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RepeatSegment", "output");
            Directory.CreateDirectory(cacheDir);
            string audioName = Path.GetFileNameWithoutExtension(_ae.FilePath);
            string filePath = Path.Combine(cacheDir, $"{audioName}_chunk{ci:D4}_{provider}_en.json");
            string json = JsonSerializer.Serialize(data);
            File.WriteAllText(filePath, json);
        }
        catch { }
    }

    // ── Deepgram ───────────────────────────────────────────────────
    async Task<TranscriptionResult?> TranscribeDeepgram(string wavPath)
    {
        var apiKey = _cfg.DeepgramApiKey;
        if (string.IsNullOrEmpty(apiKey)) { StatusChanged?.Invoke("No Deepgram key"); return null; }
        try
        {
            var http = new HttpClient { Timeout = TimeSpan.FromSeconds(MaxTranscribeWaitSec) };
            string url = $"{DeepgramApiUrl}?model=nova-2&smart_format=true&language=en";
            var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Token", apiKey);
            req.Content = new ByteArrayContent(await File.ReadAllBytesAsync(wavPath));
            req.Content.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");

            var resp = await http.SendAsync(req);
            string body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                string detail = body.Length > 200 ? body[..200] : body;
                StatusChanged?.Invoke($"DG error {(int)resp.StatusCode}: {detail}");
                Log.Error($"Deepgram {(int)resp.StatusCode}: {body}");
                return null;
            }

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (!root.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Object) { StatusChanged?.Invoke("DG: no results"); return new(); }
            if (!results.TryGetProperty("channels", out var ch) || ch.ValueKind != JsonValueKind.Array || ch.GetArrayLength() == 0) { StatusChanged?.Invoke("DG: no channels"); return new(); }
            var alt = ch[0].GetProperty("alternatives")[0];
            string text = alt.TryGetProperty("transcript", out var tr) ? tr.GetString() ?? "" : "";
            var words = new List<WordTiming>();
            if (alt.TryGetProperty("words", out var rw) && rw.ValueKind == JsonValueKind.Array)
            {
                foreach (var w in rw.EnumerateArray())
                {
                    string tok = "";
                    if (w.TryGetProperty("punctuated_word", out var pw)) tok = pw.GetString() ?? "";
                    if (string.IsNullOrEmpty(tok) && w.TryGetProperty("word", out var wd)) tok = wd.GetString() ?? "";
                    if (string.IsNullOrEmpty(tok)) continue;
                    double s = 0, e = 0;
                    if (w.TryGetProperty("start", out var js) && js.TryGetDouble(out double ds)) s = ds;
                    if (w.TryGetProperty("end", out var je) && je.TryGetDouble(out double de)) e = de;
                    words.Add(new WordTiming { Word = tok, Start = s, End = e });
                }
            }
            StatusChanged?.Invoke($"DG: {words.Count} words");
            return new TranscriptionResult { Text = text, Words = words };
        }
        catch (Exception ex) { StatusChanged?.Invoke($"DG ex: {ex.Message}"); Log.Error($"Deepgram exception: {ex}"); return null; }
    }

    // ── AssemblyAI ─────────────────────────────────────────────────
    async Task<TranscriptionResult?> TranscribeAssemblyAi(string wavPath)
    {
        var apiKey = _cfg.AssemblyAiApiKey;
        if (string.IsNullOrEmpty(apiKey)) { StatusChanged?.Invoke("No AssemblyAI key"); return null; }

        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(AssemblyAiMaxWait + 60) };

            // ── Step 1: Upload audio ──
            // NOTE: AssemblyAI may be blocked in some regions (RU). Use VPN if connection is reset.
            StatusChanged?.Invoke("AA uploading...");
            var upContent = new ByteArrayContent(await File.ReadAllBytesAsync(wavPath));
            upContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            var upReq = new HttpRequestMessage(HttpMethod.Post, AssemblyAiUploadUrl) { Content = upContent };
            upReq.Headers.Authorization = new AuthenticationHeaderValue(apiKey);
            var upResp = await http.SendAsync(upReq);
            string upBody = await upResp.Content.ReadAsStringAsync();
            if (!upResp.IsSuccessStatusCode) { StatusChanged?.Invoke($"AA upload fail {(int)upResp.StatusCode}"); return null; }
            var upJ = JsonDocument.Parse(upBody).RootElement;
            string? audioUrl = upJ.TryGetProperty("upload_url", out var uu) ? uu.GetString() : null;
            if (audioUrl == null) { StatusChanged?.Invoke("AA no upload_url"); return null; }
            StatusChanged?.Invoke("AA uploaded ok");

            // ── Step 2: Submit transcription ──
            StatusChanged?.Invoke("AA submitting...");
            var payload = JsonSerializer.Serialize(new { audio_url = audioUrl, language_code = "en" });
            var crReq = new HttpRequestMessage(HttpMethod.Post, AssemblyAiTranscriptUrl)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
            crReq.Headers.Authorization = new AuthenticationHeaderValue(apiKey);
            var crResp = await http.SendAsync(crReq);
            string crBody = await crResp.Content.ReadAsStringAsync();
            if (!crResp.IsSuccessStatusCode) { StatusChanged?.Invoke($"AA submit fail {(int)crResp.StatusCode}"); return null; }
            var crJ = JsonDocument.Parse(crBody).RootElement;
            string? trId = crJ.TryGetProperty("id", out var id) ? id.GetString() : null;
            if (trId == null) { StatusChanged?.Invoke("AA no id"); return null; }
            StatusChanged?.Invoke($"AA id={trId}");

            // ── Step 3: Poll ──
            double t0 = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            int pollCount = 0;
            while (true)
            {
                double elapsed = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - t0;
                if (elapsed > AssemblyAiMaxWait) { StatusChanged?.Invoke($"AA timeout after {AssemblyAiMaxWait}s"); return null; }
                pollCount++;
                var pollReq = new HttpRequestMessage(HttpMethod.Get, $"{AssemblyAiTranscriptUrl}/{trId}");
                pollReq.Headers.Authorization = new AuthenticationHeaderValue(apiKey);
                var pollResp = await http.SendAsync(pollReq);
                string pollBody = await pollResp.Content.ReadAsStringAsync();
                if (!pollResp.IsSuccessStatusCode) { StatusChanged?.Invoke($"AA poll #{pollCount} {(int)pollResp.StatusCode}"); return null; }
                var pollJ = JsonDocument.Parse(pollBody).RootElement;
                string? status = pollJ.TryGetProperty("status", out var st) ? st.GetString() : null;
                StatusChanged?.Invoke($"AA poll #{pollCount} status={status} elapsed={elapsed:F0}s");

                if (status == "completed")
                {
                    var result = ParseAssemblyAi(pollJ);
                    StatusChanged?.Invoke($"AA done: {result.Words?.Count ?? 0} words");
                    return result;
                }
                if (status == "error")
                {
                    string errMsg = "AA error";
                    if (pollJ.TryGetProperty("error", out var errEl) && errEl.TryGetProperty("message", out var msg))
                        errMsg = $"AA error: {msg.GetString()}";
                    StatusChanged?.Invoke(errMsg);
                    return null;
                }
                await Task.Delay((int)(AssemblyAiPollInterval * 1000));
            }
        }
        catch (Exception ex) { StatusChanged?.Invoke($"AA ex: {ex.Message}"); return null; }
    }
    
        /// <summary>Truncate string for status messages.</summary>
        static string Truncate(string s, int max) =>
            s.Length <= max ? s : s[..max] + "...";

    static TranscriptionResult ParseAssemblyAi(JsonElement r)
    {
        string text = r.TryGetProperty("text", out var t) ? (t.GetString() ?? "") : "";
        var words = new List<WordTiming>();
        if (r.TryGetProperty("words", out var rw) && rw.ValueKind == JsonValueKind.Array)
        {
            foreach (var w in rw.EnumerateArray())
            {
                string tok = w.TryGetProperty("text", out var wt) ? (wt.GetString() ?? "") : "";
                if (string.IsNullOrEmpty(tok)) continue;

                // AssemblyAI returns start/end in milliseconds as integers
                double s = 0, e = 0;
                if (w.TryGetProperty("start", out var js) && js.TryGetInt64(out long ls)) s = ls / 1000.0;
                if (w.TryGetProperty("end", out var je) && je.TryGetInt64(out long le)) e = le / 1000.0;
                words.Add(new WordTiming { Word = tok, Start = s, End = e });
            }
        }
        return new TranscriptionResult { Text = text, Words = words };
    }
}

public class WordTiming { public string Word { get; set; } = ""; public double Start { get; set; } public double End { get; set; } }
public class TranscriptionResult { public string? Text { get; set; } public List<WordTiming>? Words { get; set; } }
public class TranscriptionChunk { public string? Text; public List<WordTiming>? Words; public TranscriptionChunk() { } public TranscriptionChunk(TranscriptionResult r) { Text = r.Text; Words = r.Words; } }
