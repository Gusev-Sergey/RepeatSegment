using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RepeatSegment.App;

/// <summary>
/// Audio engine — load, analyse, playback for audio files (MP3/WAV).
/// Uses NAudio for decoding and playback, no ffmpeg dependency.
/// Property names match those expected by MainWindow.xaml.cs.
/// </summary>
public class AudioEngine : IDisposable
{
    // ── Properties (matching MainWindow references) ─────────────────
    
    /// <summary>Full-quality mono samples (float32, normalized to peak 1.0).</summary>
    public float[]? Samples { get; private set; }

    /// <summary>Downsampled samples for waveform display.</summary>
    public float[]? SamplesSmall { get; private set; }

    /// <summary>Original sampling rate (Hz). Referenced as SampleRate in MainWindow.</summary>
    public int SampleRate { get; private set; } = 44100;

    /// <summary>Downsampled rate for display (Hz). Referenced as SampleRateSmall in MainWindow.</summary>
    public int SampleRateSmall { get; private set; } = 1000;

    /// <summary>Duration as TimeSpan (referenced as _audio.Duration.TotalSeconds in MainWindow).</summary>
    public TimeSpan Duration { get; private set; } = TimeSpan.Zero;

    /// <summary>Path to the loaded audio file. Referenced as FilePath in MainWindow.</summary>
    public string FilePath { get; private set; } = "";

    /// <summary>Whether this instance has been disposed.</summary>
    public bool IsDisposed { get; private set; }

    /// <summary>Volume level (0.0 to 1.0).</summary>
    public float Volume
    {
        get => _volume;
        set
        {
            _volume = Math.Clamp(value, 0f, 1f);
            if (_waveOut != null)
                _waveOut.Volume = _volume;
        }
    }
    private float _volume = 1.0f;

    /// <summary>Playback speed multiplier (0.4 to 1.5). 1.0 = normal. Pitch preserved via SOLA.</summary>
    public double PlaybackSpeed { get; set; } = 1.0;

    // ── Playback internals ─────────────────────────────────────────
    private WaveOutEvent? _waveOut;
    private IWaveProvider? _playbackProvider;
    private double _playbackStartSample;  // sample index where playback started
    private long _playbackStartTick;      // Environment.TickCount64 when playback started
    private bool _isPaused;

    // ── Load ───────────────────────────────────────────────────────

    /// <summary>Load an MP3 or WAV file. Returns true on success.</summary>
    public bool Load(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Log.Error($"[ERROR] File not found: {filePath}");
            return false;
        }

        StopPlaybackInternal();

        try
        {
            using var reader = CreateReader(filePath);
            ISampleProvider sampleProvider = reader.ToSampleProvider();

            // Read all float samples
            var sampleList = new List<float>();
            float[] buffer = new float[4096];
            int n;
            while ((n = sampleProvider.Read(buffer, 0, buffer.Length)) > 0)
                sampleList.AddRange(buffer.Take(n));

            float[] allSamples = sampleList.ToArray();

            // Mono conversion: average channels if needed
            int channels = reader.WaveFormat.Channels;
            if (channels > 1)
            {
                int monoLen = allSamples.Length / channels;
                var monoSamples = new float[monoLen];
                for (int i = 0; i < monoLen; i++)
                {
                    float sum = 0;
                    for (int ch = 0; ch < channels; ch++)
                        sum += allSamples[i * channels + ch];
                    monoSamples[i] = sum / channels;
                }
                allSamples = monoSamples;
            }

            FilePath = filePath;
            SampleRate = reader.WaveFormat.SampleRate;
            Duration = TimeSpan.FromSeconds((double)allSamples.Length / SampleRate);

            // Normalize to peak 1.0
            float peak = 0;
            foreach (var s in allSamples)
            {
                var abs = Math.Abs(s);
                if (abs > peak) peak = abs;
            }
            if (peak > 0)
            {
                for (int i = 0; i < allSamples.Length; i++)
                    allSamples[i] /= peak;
            }
            Samples = allSamples;

            // Downsample for display (SamplesSmall)
            SampleRateSmall = Math.Min(SampleRate, 1000);
            if (SampleRate > 1000)
            {
                int factor = SampleRate / SampleRateSmall;
                int smallLen = (allSamples.Length + factor - 1) / factor;
                var small = new float[smallLen];
                for (int i = 0; i < smallLen; i++)
                    small[i] = allSamples[Math.Min(i * factor, allSamples.Length - 1)];
                SamplesSmall = small;
                SampleRateSmall = SampleRate / factor;
            }
            else
            {
                SamplesSmall = (float[])allSamples.Clone();
            }

            Log.Info($"[INFO] Loaded {filePath}, duration={Duration.TotalSeconds:F1}s, sr={SampleRate}, sr_small={SampleRateSmall}");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"[ERROR] Error loading {filePath}: {ex.Message}");
            return false;
        }
    }

    // ── Compatibility method: GetSamples() returns Samples ─────────

    /// <summary>Returns the full-quality sample array (called as _audio.GetSamples() in MainWindow).</summary>
    public float[]? GetSamples() => Samples;

    // ── Fast WSOLA time-stretch (pitch-preserving) ────────────────

    /// <summary>
    /// Waveform-Similarity Overlap-Add time stretch.
    /// Frame = 4096, output hop = 2048 (50% overlap). Bartlett window (perfect COLA).
    /// Simple cross-correlation align on 512 samples, ±64 radius.
    /// speed > 1 = faster (shorter), speed < 1 = slower (longer).
    /// </summary>
    public static float[] StretchSola(float[] input, double speed)
    {
        if (Math.Abs(speed - 1.0) < 0.005 || input.Length < 4096)
            return (float[])input.Clone();

        const int frameSize = 4096;
        const int outputHop = 2048; // 50% overlap
        const int corrLen = 2048; // full overlap zone
        const int searchRadius = 32;

        // Bartlett (triangle) window: w[n] = 1 - |2n/(N-1) - 1|
        // Perfect COLA at 50% overlap: w[n] + w[n+N/2] = 1 for all n
        var window = new float[frameSize];
        for (int i = 0; i < frameSize; i++)
            window[i] = 1f - Math.Abs(2f * i / (frameSize - 1f) - 1f);

        double inputHop = outputHop * speed;
        int maxFrames = (int)((input.Length - frameSize) / inputHop) + 2;
        if (maxFrames < 1) maxFrames = 1;
        int outputLength = maxFrames * outputHop + frameSize;
        var output = new float[outputLength];

        double inputPos = 0;
        for (int outFrame = 0; outFrame < maxFrames; outFrame++)
        {
            int outCenter = outFrame * outputHop + frameSize / 2;
            if (outCenter >= outputLength) break;

            int baseInCenter = (int)Math.Round(inputPos + frameSize / 2.0);
            int inCenter = baseInCenter;

            // WSOLA: align input frame with existing output via cross-correlation
            if (outFrame > 0)
            {
                int overlapOutStart = outCenter - frameSize / 2;
                float bestCorr = float.NegativeInfinity;
                int bestOffset = 0;

                for (int offset = -searchRadius; offset <= searchRadius; offset++)
                {
                    int trialCenter = baseInCenter + offset;
                    int trialStart = trialCenter - frameSize / 2;
                    if (trialStart + corrLen >= input.Length || trialStart < 0)
                        continue;

                    float corr = 0;
                    for (int s = 0; s < corrLen; s++)
                        corr += output[overlapOutStart + s] * input[trialStart + s];

                    if (corr > bestCorr)
                    {
                        bestCorr = corr;
                        bestOffset = offset;
                    }
                }
                inCenter = baseInCenter + bestOffset;
            }

            int inStart = inCenter - frameSize / 2;
            int outStart = outCenter - frameSize / 2;

            for (int k = 0; k < frameSize; k++)
            {
                int inIdx = inStart + k;
                int outIdx = outStart + k;
                if (outIdx < 0 || outIdx >= outputLength) continue;

                float sample = (inIdx >= 0 && inIdx < input.Length) ? input[inIdx] : 0f;
                output[outIdx] += sample * window[k];
            }

            inputPos += inputHop;
            if (inputPos + frameSize > input.Length) break;
        }

        // Clamp and trim trailing silence
        int validEnd = outputLength;
        for (int i = outputLength - 1; i >= 0; i--)
        {
            float v = output[i];
            if (v < -1f) v = -1f;
            if (v > 1f) v = 1f;
            output[i] = v;
            if (Math.Abs(v) > 0.0001f) { validEnd = i + 1; break; }
        }

        if (validEnd > 0 && validEnd < outputLength)
        {
            var trimmed = new float[validEnd];
            Array.Copy(output, trimmed, validEnd);
            return trimmed;
        }

        return output;
    }

    // ── Playback control ───────────────────────────────────────────

    /// <summary>Play a pre-extracted float[] segment directly (called from MainWindow).</summary>
    public void PlaySegment(float[] segmentSamples)
    {
        if (Samples == null || IsDisposed)
            return;

        StopPlaybackInternal();

        // Apply SOLA time-stretch if speed != 1.0
        float[] played = StretchSola(segmentSamples, PlaybackSpeed);

        var rawProvider = new RawSampleProvider(played, SampleRate);
        _playbackProvider = rawProvider;
        _playbackStartSample = 0;
        _playbackStartTick = Environment.TickCount64;
        _isPaused = false;

        _waveOut = new WaveOutEvent();
        _waveOut.Init(rawProvider);
        _waveOut.Volume = _volume;
        _waveOut.Play();

        Log.Info($"[INFO] PlaySegment started, {segmentSamples.Length} samples, speed={PlaybackSpeed:F2}, stretched={played.Length}");
    }

    /// <summary>Start playback from the given position in seconds.</summary>
    public void Play(double positionSeconds = 0.0)
    {
        if (Samples == null || IsDisposed)
            return;

        StopPlaybackInternal();

        int startSample = (int)(positionSeconds * SampleRate);
        if (startSample >= Samples.Length)
            startSample = 0;

        // Create a provider that gives us the segment from startSample to end
        var segmentSamples = new float[Samples.Length - startSample];
        Array.Copy(Samples, startSample, segmentSamples, 0, segmentSamples.Length);

        float[] played = StretchSola(segmentSamples, PlaybackSpeed);

        var rawProvider = new RawSampleProvider(played, SampleRate);
        _playbackProvider = rawProvider;
        _playbackStartSample = startSample;
        _playbackStartTick = Environment.TickCount64;
        _isPaused = false;

        _waveOut = new WaveOutEvent();
        _waveOut.PlaybackStopped += (s, e) =>
        {
            // Natural end of playback
            if (!_isPaused)
            {
                // Do nothing — let timer detect end
            }
        };
        _waveOut.Init(rawProvider);
        _waveOut.Play();

        Log.Info($"[INFO] Playback started from {positionSeconds:F1}s, speed={PlaybackSpeed:F2}");
    }

    /// <summary>Pause playback, keeping position.</summary>
    public void Pause()
    {
        if (_waveOut == null || IsDisposed)
            return;

        _isPaused = true;
        _waveOut.Pause();
        Log.Info("[INFO] Playback paused");
    }

    /// <summary>Stop playback and reset.</summary>
    public void Stop()
    {
        _isPaused = false;
        StopPlaybackInternal();
        Log.Info("[INFO] Playback stopped");
    }

    /// <summary>Seek to a new position without restarting playback.</summary>
    public void Seek(double positionSeconds)
    {
        if (Samples == null || IsDisposed)
            return;

        bool wasPlaying = _waveOut != null && _waveOut.PlaybackState == PlaybackState.Playing;

        StopPlaybackInternal();

        int startSample = (int)(positionSeconds * SampleRate);
        if (startSample < 0) startSample = 0;
        if (startSample >= Samples.Length) startSample = Samples.Length - 1;

        var segmentSamples = new float[Samples.Length - startSample];
        Array.Copy(Samples, startSample, segmentSamples, 0, segmentSamples.Length);

        float[] played = StretchSola(segmentSamples, PlaybackSpeed);

        var rawProvider = new RawSampleProvider(played, SampleRate);
        _playbackProvider = rawProvider;
        _playbackStartSample = startSample;
        _playbackStartTick = Environment.TickCount64;
        _isPaused = false;

        _waveOut = new WaveOutEvent();
        _waveOut.Init(rawProvider);

        if (wasPlaying)
            _waveOut.Play();

        Log.Info($"[INFO] Seek to {positionSeconds:F1}s, speed={PlaybackSpeed:F2}");
    }

    /// <summary>Get current playback position in seconds.</summary>
    public double GetCurrentPosition()
    {
        if (Samples == null || IsDisposed || _playbackProvider == null)
            return 0.0;

        if (_isPaused || _waveOut == null)
            return _playbackStartSample / (double)SampleRate;

        long elapsedMs = Environment.TickCount64 - _playbackStartTick;
        double currentSamplePos = _playbackStartSample + (elapsedMs / 1000.0 * SampleRate);
        double position = currentSamplePos / SampleRate;

        if (position >= Duration.TotalSeconds)
            position = Duration.TotalSeconds;

        return position;
    }

    // ── Segment extraction ─────────────────────────────────────────

    /// <summary>Extract segment from Samples from t1 to t2 seconds.</summary>
    public float[]? GetPlaySamples(double t1, double t2, int? samplingRate = null)
    {
        if (Samples == null) return null;
        int sr = samplingRate ?? SampleRate;
        int start = (int)(t1 * sr);
        int end = (int)(t2 * sr);
        end = Math.Min(end, Samples.Length);
        if (start >= end) return null;
        var result = new float[end - start];
        Array.Copy(Samples, start, result, 0, result.Length);
        return result;
    }

    /// <summary>Extract segment from t1 to end of file.</summary>
    public float[]? GetPlaySamplesToEnd(double t1, int? samplingRate = null)
    {
        if (Samples == null) return null;
        int sr = samplingRate ?? SampleRate;
        int start = (int)(t1 * sr);
        if (start >= Samples.Length) return null;
        var result = new float[Samples.Length - start];
        Array.Copy(Samples, start, result, 0, result.Length);
        return result;
    }

    /// <summary>Extract a chunk of audio to a temporary WAV file for transcription.</summary>
    public string ExtractChunk(double t1Sec, double t2Sec)
    {
        if (Samples == null)
            throw new InvalidOperationException("Audio not loaded");

        int start = (int)(t1Sec * SampleRate);
        int end = (int)(t2Sec * SampleRate);
        end = Math.Min(end, Samples.Length);
        int length = end - start;
        if (length <= 0)
            throw new ArgumentException("Invalid segment bounds");

        string tmpPath = Path.GetTempFileName() + ".wav";
        var waveFormat = new WaveFormat(48000, 16, 1);
        using var writer = new WaveFileWriter(tmpPath, waveFormat);

        double srcRate = SampleRate;
        double dstRate = 48000.0;
        double ratio = srcRate / dstRate;
        var chunkSamples = new float[length];
        Array.Copy(Samples, start, chunkSamples, 0, length);

        int dstLen = (int)(length / ratio);
        var dstSamples = new float[dstLen];
        for (int i = 0; i < dstLen; i++)
        {
            double srcIndex = i * ratio;
            int srcIdx = (int)srcIndex;
            float frac = (float)(srcIndex - srcIdx);
            float s0 = chunkSamples[srcIdx];
            float s1 = (srcIdx + 1 < chunkSamples.Length) ? chunkSamples[srcIdx + 1] : s0;
            dstSamples[i] = s0 + (s1 - s0) * frac;
        }

        var bytes = new byte[dstLen * 2];
        for (int i = 0; i < dstLen; i++)
        {
            short val = (short)Math.Max(-32768, Math.Min(32767, dstSamples[i] * 32767));
            bytes[i * 2] = (byte)(val & 0xFF);
            bytes[i * 2 + 1] = (byte)((val >> 8) & 0xFF);
        }
        writer.Write(bytes, 0, bytes.Length);

        return tmpPath;
    }

    /// <summary>Save a snippet of audio as a WAV file for preview playback.</summary>
    public string SaveSnippetWav(double t1, double t2)
    {
        if (Samples == null)
            throw new InvalidOperationException("Audio not loaded");

        int start = (int)((t1 - 0.02) * SampleRate);
        int end = (int)((t2 + 0.02) * SampleRate);
        if (start < 0) start = 0;
        end = Math.Min(end, Samples.Length);
        int length = end - start;
        if (length <= 0)
            throw new ArgumentException("Invalid snippet bounds");

        string snippetDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RepeatSegment", "decks", "media");
        Directory.CreateDirectory(snippetDir);
        string path = Path.Combine(snippetDir, $"snippet_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.wav");

        var waveFormat = new WaveFormat(SampleRate, 16, 1);
        using var writer = new WaveFileWriter(path, waveFormat);
        var chunkSamples = new float[length];
        Array.Copy(Samples, start, chunkSamples, 0, length);

        int fadeInSamples = (int)(0.005 * SampleRate);
        int fadeOutSamples = (int)(0.005 * SampleRate);
        for (int i = 0; i < length; i++)
        {
            float gain = 1f;
            if (i < fadeInSamples) gain = (float)i / fadeInSamples;
            else if (i >= length - fadeOutSamples) gain = (float)(length - 1 - i) / fadeOutSamples;
            chunkSamples[i] *= gain;
        }

        var bytes = new byte[length * 2];
        for (int i = 0; i < length; i++)
        {
            short val = (short)Math.Max(-32768, Math.Min(32767, chunkSamples[i] * 32767));
            bytes[i * 2] = (byte)(val & 0xFF);
            bytes[i * 2 + 1] = (byte)((val >> 8) & 0xFF);
        }
        writer.Write(bytes, 0, bytes.Length);
        return path;
    }

    /// <summary>Save a snippet as MP3 for Anki cards (WAV is not well supported).</summary>
    /// <summary>MP3 bitrate for sentence snippets, set by caller. Default 128 kbps.</summary>
    public int Mp3BitrateKbps { get; set; } = 128;

    /// <summary>Save a snippet as MP3 for Anki cards (WAV is not well supported).</summary>
    public string SaveSnippetMp3(double t1, double t2)
    {
        if (Samples == null)
            throw new InvalidOperationException("Audio not loaded");

        int start = (int)((t1 - 0.02) * SampleRate);
        int end = (int)((t2 + 0.02) * SampleRate);
        if (start < 0) start = 0;
        end = Math.Min(end, Samples.Length);
        int length = end - start;
        if (length <= 0)
            throw new ArgumentException("Invalid snippet bounds");

        string snippetDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RepeatSegment", "decks", "media");
        Directory.CreateDirectory(snippetDir);
        string path = Path.Combine(snippetDir, $"snippet_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.mp3");

        var chunkSamples = new float[length];
        Array.Copy(Samples, start, chunkSamples, 0, length);

        int fadeInSamples = (int)(0.005 * SampleRate);
        int fadeOutSamples = (int)(0.005 * SampleRate);
        for (int i = 0; i < length; i++)
        {
            float gain = 1f;
            if (i < fadeInSamples) gain = (float)i / fadeInSamples;
            else if (i >= length - fadeOutSamples) gain = (float)(length - 1 - i) / fadeOutSamples;
            chunkSamples[i] *= gain;
        }

        var shorts = new short[length];
        for (int i = 0; i < length; i++)
            shorts[i] = (short)Math.Max(-32768, Math.Min(32767, chunkSamples[i] * 32767));

        using var ms = new MemoryStream();
        using var mp3Writer = new NAudio.Lame.LameMP3FileWriter(ms, new WaveFormat(SampleRate, 16, 1), Mp3BitrateKbps);
        var shortBytes = new byte[length * 2];
        Buffer.BlockCopy(shorts, 0, shortBytes, 0, shortBytes.Length);
        mp3Writer.Write(shortBytes, 0, shortBytes.Length);
        mp3Writer.Flush();
        File.WriteAllBytes(path, ms.ToArray());

        return path;
    }

    /// <summary>Extract segment from SamplesSmall for waveform drawing.</summary>
    public float[]? GetPlotSamples(double t1Sec, double t2Sec)
    {
        if (SamplesSmall == null) return null;
        int sr = SampleRateSmall;
        int start = (int)(t1Sec * sr);
        int end = (int)(t2Sec * sr);
        end = Math.Min(end, SamplesSmall.Length);
        if (start >= end) return null;
        var result = new float[end - start];
        Array.Copy(SamplesSmall, start, result, 0, result.Length);
        return result;
    }

    // ── Dispose ────────────────────────────────────────────────────

    public void Dispose()
    {
        IsDisposed = true;
        StopPlaybackInternal();
        Samples = null;
        SamplesSmall = null;
    }

    // ── Private helpers ────────────────────────────────────────────

    private void StopPlaybackInternal()
    {
        if (_waveOut != null)
        {
            try
            {
                _waveOut.Stop();
                _waveOut.Dispose();
            }
            catch { }
            _waveOut = null;
        }
        _playbackProvider = null;
        _isPaused = false;
    }

    private static WaveStream CreateReader(string filePath)
    {
        string ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext switch
        {
            ".mp3" => new Mp3FileReader(filePath),
            ".wav" => new WaveFileReader(filePath),
            _ => throw new NotSupportedException($"Unsupported audio format: {ext}")
        };
    }

    // ── Inner class: RawSampleProvider ─────────────────────────────

    /// <summary>
    /// Simple IWaveProvider that feeds float[] samples to WaveOutEvent.
    /// Converts float [-1..1] to 16-bit PCM on the fly.
    /// Time-stretching is handled before this stage (via StretchSola).
    /// </summary>
    private class RawSampleProvider : IWaveProvider
    {
        private readonly float[] _samples;
        private int _position;

        public WaveFormat WaveFormat { get; }

        public RawSampleProvider(float[] samples, int sampleRate)
        {
            _samples = samples;
            WaveFormat = new WaveFormat(sampleRate, 16, 1);
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            int samplesNeeded = count / 2; // 16-bit mono = 2 bytes per sample
            int samplesAvailable = _samples.Length - _position;
            int samplesToRead = Math.Min(samplesNeeded, samplesAvailable);

            for (int i = 0; i < samplesToRead; i++)
            {
                float sample = _samples[_position + i];
                // Clamp
                if (sample < -1f) sample = -1f;
                if (sample > 1f) sample = 1f;
                short pcm = (short)(sample * 32767);
                buffer[offset + i * 2] = (byte)(pcm & 0xFF);
                buffer[offset + i * 2 + 1] = (byte)((pcm >> 8) & 0xFF);
            }

            _position += samplesToRead;
            return samplesToRead * 2;
        }
    }
}

/// <summary>
/// Minimal logger, mirrors Python logging.getLogger('SEAB.audio').
/// </summary>
internal static class Log
{
    private static readonly string LogFilePath;
    private static readonly object _lock = new();

    static Log()
    {
        LogFilePath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RepeatSegment", "repeat_segment.log");
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(LogFilePath)!);
    }

    public static void Info(string message) => WriteLine(message, "INFO");
    public static void Error(string message) => WriteLine(message, "ERROR");
    public static void Warn(string message) => WriteLine(message, "WARN");

    private static void WriteLine(string message, string level)
    {
        string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";
        System.Diagnostics.Debug.WriteLine(line);
        try
        {
            lock (_lock)
            {
                System.IO.File.AppendAllText(LogFilePath, line + Environment.NewLine);
            }
        }
        catch { /* best-effort */ }
    }
}
