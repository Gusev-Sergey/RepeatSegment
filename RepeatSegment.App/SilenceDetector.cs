using System;
using System.Collections.Generic;

namespace RepeatSegment.App;

/// <summary>
/// Silence detector — finds silence periods and splits audio into speech fragments.
/// Uses per-window dBFS comparison, matching pydub silence.detect_silence algorithm.
/// </summary>
public class SilenceDetector
{
    /// <summary>Minimum silence length in milliseconds.</summary>
    public int MinSilenceLenMs { get; set; } = 300;

    /// <summary>Silence threshold offset below overall dBFS (dB).</summary>
    public int SilenceThreshOffsetDb { get; set; } = 20;

    /// <summary>Search step in milliseconds.</summary>
    public int SeekStepMs { get; set; } = 100;

    /// <summary>Detected silence zones as (startSec, endSec).</summary>
    public List<(double Start, double End)> Silence { get; private set; } = new();

    /// <summary>Speech fragments as [t1, t2] for sequential playback.</summary>
    public List<(double T1, double T2)> T1T2Array { get; private set; } = new();

    /// <summary>Number of detected silence zones.</summary>
    public int NumberPartsSilence { get; private set; }

    /// <summary>
    /// Detect silence from AudioEngine's samples.
    /// Algorithm matches pydub: compute overall dBFS, then compare per-window dBFS
    /// against (dBFS - offset).
    /// </summary>
    public bool Detect(float[] samples, int samplingRate, double duration)
    {
        if (samples == null || samples.Length == 0)
        {
            Log.Info("[WARN] No samples for silence detection");
            return BuildT1T2Array(duration);
        }

        try
        {
            // Compute overall RMS and dBFS (matching pydub dBFS)
            double sumAll = 0;
            for (int i = 0; i < samples.Length; i++)
                sumAll += (double)samples[i] * samples[i];
            double overallRms = Math.Sqrt(sumAll / samples.Length);
            double dBFS = overallRms > 1e-10 ? 20.0 * Math.Log10(overallRms) : -96.0;
            double silenceThreshDB = dBFS - SilenceThreshOffsetDb;

            int minSilenceSamples = (int)(MinSilenceLenMs / 1000.0 * samplingRate);
            int seekSamples = (int)(SeekStepMs / 1000.0 * samplingRate);
            if (seekSamples < 1) seekSamples = 1;

            Log.Info($"[DEBUG] dBFS={dBFS:F1}, silenceThreshDB={silenceThreshDB:F1}, minSilenceSamples={minSilenceSamples}, seekSamples={seekSamples}");

            Silence = DetectSilenceInternal(samples, samplingRate, silenceThreshDB, minSilenceSamples, seekSamples);

            Log.Info($"[DEBUG] Found {Silence.Count} silence zones");

            return BuildT1T2Array(duration);
        }
        catch (Exception ex)
        {
            Log.Info($"[ERROR] Silence detection failed: {ex.Message}");
            Silence.Clear();
            return BuildT1T2Array(duration);
        }
    }

    /// <summary>
    /// Per-window dBFS comparison: a seek-window is silent if its dBFS < silenceThreshDB.
    /// This matches pydub's per-chunk dBFS thresholding.
    /// </summary>
    private static List<(double Start, double End)> DetectSilenceInternal(
        float[] samples, int samplingRate, double silenceThreshDB, int minSilenceSamples, int seekSamples)
    {
        var silenceZones = new List<(double Start, double End)>();
        int i = 0;

        while (i < samples.Length)
        {
            // Compute dBFS for current seek window
            int windowEnd = Math.Min(i + seekSamples, samples.Length);
            int windowLen = windowEnd - i;
            if (windowLen < 1) break;

            double sumSq = 0;
            for (int j = i; j < windowEnd; j++)
                sumSq += (double)samples[j] * samples[j];
            double rms = Math.Sqrt(sumSq / windowLen);
            double db = rms > 1e-10 ? 20.0 * Math.Log10(rms) : -96.0;

            if (db < silenceThreshDB)
            {
                // Start of silence — expand until loud again
                int silenceStart = i;
                while (i < samples.Length)
                {
                    i += seekSamples;
                    if (i >= samples.Length) break;

                    int wEnd = Math.Min(i + seekSamples, samples.Length);
                    int wLen = wEnd - i;
                    if (wLen < 1) break;

                    sumSq = 0;
                    for (int j = i; j < wEnd; j++)
                        sumSq += (double)samples[j] * samples[j];
                    rms = Math.Sqrt(sumSq / wLen);
                    double wDb = rms > 1e-10 ? 20.0 * Math.Log10(rms) : -96.0;

                    if (wDb >= silenceThreshDB)
                        break; // back to speech
                }

                int silenceEnd = Math.Min(i, samples.Length);
                int silenceLen = silenceEnd - silenceStart;

                if (silenceLen >= minSilenceSamples)
                {
                    double startSec = (double)silenceStart / samplingRate;
                    double endSec = (double)silenceEnd / samplingRate;
                    silenceZones.Add((startSec, endSec));
                    Log.Info($"[DEBUG] Silence zone: {startSec:F2}s - {endSec:F2}s (len={silenceLen} samples)");
                }
            }
            else
            {
                i += seekSamples;
            }
        }

        return silenceZones;
    }

    /// <summary>
    /// Build speech fragment array from silence zones.
    /// Logic:
    ///   - Silence zones are pauses.
    ///   - Speech fragments = segments BETWEEN silence zones.
    ///   - Initial silence (starting at 0) is discarded.
    ///   - Close silence zones (< 3 seconds between them) are merged.
    /// </summary>
    private bool BuildT1T2Array(double duration)
    {
        T1T2Array.Clear();
        var sil = new List<(double Start, double End)>(Silence);

        if (sil.Count == 0)
        {
            Log.Info("[WARN] No silence detected. Whole file is one fragment.");
            T1T2Array.Add((0.0, duration));
            NumberPartsSilence = 0;
            return true;
        }

        // Remove initial silence
        if (sil[0].Start <= 0.001)
            sil.RemoveAt(0);

        if (sil.Count == 0)
        {
            Log.Info("[WARN] Only initial silence found. Whole file is one fragment.");
            T1T2Array.Add((0.0, duration));
            NumberPartsSilence = 0;
            return true;
        }

        // Merge close silence zones (Python: compare start-to-start, >= 3 sec gap)
        var compact = new List<(double Start, double End)> { sil[0] };
        for (int i = 1; i < sil.Count; i++)
        {
            if (sil[i].Start - compact[compact.Count - 1].Start >= 3.0)
                compact.Add(sil[i]);
        }

        Silence = compact;
        NumberPartsSilence = compact.Count;

        // Build speech fragments: boundaries defined by silence START times (matches Python)
        T1T2Array.Add((0.0, compact[0].Start));

        for (int i = 0; i < NumberPartsSilence - 1; i++)
            T1T2Array.Add((compact[i].Start, compact[i + 1].Start));

        T1T2Array.Add((compact[compact.Count - 1].Start, duration));

        Log.Info($"[INFO] Detected {NumberPartsSilence} silence zones, created {T1T2Array.Count} speech fragments");
        return true;
    }
}
