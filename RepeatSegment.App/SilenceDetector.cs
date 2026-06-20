using System;
using System.Collections.Generic;

namespace RepeatSegment.App;

/// <summary>
/// Silence detector — finds silence periods and builds speech segments
/// by snapping equal-duration boundaries to nearest silence zones.
/// </summary>
public class SilenceDetector
{
    /// <summary>Target segment duration in seconds (default 5 sec).</summary>
    public double SegmentDurationSec { get; set; } = 5.0;

    /// <summary>Silence threshold offset below overall dBFS (dB).</summary>
    public int SilenceThreshOffsetDb { get; set; } = 20;

    /// <summary>Detected silence zones as (startSec, endSec).</summary>
    public List<(double Start, double End)> Silence { get; private set; } = new();

    /// <summary>Speech fragments as [t1, t2] for sequential playback.</summary>
    public List<(double T1, double T2)> T1T2Array { get; private set; } = new();

    /// <summary>Number of detected silence zones.</summary>
    public int NumberPartsSilence { get; private set; }

    /// <summary>
    /// Detect silence from samples, then build segments by
    /// snapping duration-based boundaries to nearest silence.
    /// </summary>
    public bool Detect(float[] samples, int samplingRate, double duration, double segmentDurationSec)
    {
        SegmentDurationSec = segmentDurationSec;

        if (samples == null || samples.Length == 0)
        {
            Log.Info("[WARN] No samples for silence detection");
            Silence.Clear();
            return BuildT1T2Array(duration, samplingRate);
        }

        try
        {
            // Compute overall RMS and dBFS
            double sumAll = 0;
            for (int i = 0; i < samples.Length; i++)
                sumAll += (double)samples[i] * samples[i];
            double overallRms = Math.Sqrt(sumAll / samples.Length);
            double dBFS = overallRms > 1e-10 ? 20.0 * Math.Log10(overallRms) : -96.0;
            double silenceThreshDB = dBFS - SilenceThreshOffsetDb;

            const int minSilenceMs = 80;  // minimum silence to consider (80ms = short pause)
            const int seekStepMs = 50;    // granularity
            int minSilenceSamples = (int)(minSilenceMs / 1000.0 * samplingRate);
            int seekSamples = (int)(seekStepMs / 1000.0 * samplingRate);
            if (seekSamples < 1) seekSamples = 1;

            Log.Info($"[DEBUG] dBFS={dBFS:F1}, silenceThreshDB={silenceThreshDB:F1}");

            Silence = DetectSilenceInternal(samples, samplingRate, silenceThreshDB, minSilenceSamples, seekSamples);
            Log.Info($"[DEBUG] Found {Silence.Count} silence zones");

            return BuildT1T2Array(duration, samplingRate);
        }
        catch (Exception ex)
        {
            Log.Info($"[ERROR] Silence detection failed: {ex.Message}");
            Silence.Clear();
            return BuildT1T2Array(duration, samplingRate);
        }
    }

    private static List<(double Start, double End)> DetectSilenceInternal(
        float[] samples, int samplingRate, double silenceThreshDB, int minSilenceSamples, int seekSamples)
    {
        var silenceZones = new List<(double Start, double End)>();
        int i = 0;

        while (i < samples.Length)
        {
            int windowEnd = Math.Min(i + seekSamples, samples.Length);
            int windowLen = windowEnd - i;
            if (windowLen < 1) break;

            double sumSq = 0;
            for (int j = i; j < windowEnd; j++) sumSq += (double)samples[j] * samples[j];
            double rms = Math.Sqrt(sumSq / windowLen);
            double db = rms > 1e-10 ? 20.0 * Math.Log10(rms) : -96.0;

            if (db < silenceThreshDB)
            {
                int silenceStart = i;
                while (i < samples.Length)
                {
                    i += seekSamples;
                    if (i >= samples.Length) break;
                    int wEnd = Math.Min(i + seekSamples, samples.Length);
                    int wLen = wEnd - i;
                    if (wLen < 1) break;
                    sumSq = 0;
                    for (int j = i; j < wEnd; j++) sumSq += (double)samples[j] * samples[j];
                    rms = Math.Sqrt(sumSq / wLen);
                    double wDb = rms > 1e-10 ? 20.0 * Math.Log10(rms) : -96.0;
                    if (wDb >= silenceThreshDB) break;
                }
                int silenceEnd = Math.Min(i, samples.Length);
                if (silenceEnd - silenceStart >= minSilenceSamples)
                {
                    double startSec = (double)silenceStart / samplingRate;
                    double endSec = (double)silenceEnd / samplingRate;
                    silenceZones.Add((startSec, endSec));
                }
            }
            else i += seekSamples;
        }
        return silenceZones;
    }

    /// <summary>
    /// Build segments: cut by duration, then snap to nearest silence.
    /// Search radius = 30% of segment duration.
    /// </summary>
    private bool BuildT1T2Array(double duration, int samplingRate)
    {
        T1T2Array.Clear();

        if (duration <= 0 || SegmentDurationSec <= 0)
        {
            T1T2Array.Add((0.0, duration));
            return true;
        }

        int numSegments = (int)Math.Ceiling(duration / SegmentDurationSec);
        if (numSegments <= 1)
        {
            T1T2Array.Add((0.0, duration));
            NumberPartsSilence = Silence.Count;
            return true;
        }

        // Step 1: ideal equal-duration boundaries
        var boundaries = new List<double> { 0.0 };
        for (int i = 1; i < numSegments; i++)
            boundaries.Add(i * SegmentDurationSec);
        boundaries.Add(duration);

        // Step 2: snap internal boundaries to nearest silence
        double searchRadius = SegmentDurationSec * 0.30;
        for (int b = 1; b < boundaries.Count - 1; b++)
        {
            double ideal = boundaries[b];
            double snapped = SnapToSilence(ideal, searchRadius);
            // Ensure boundaries don't cross
            if (snapped <= boundaries[b - 1] + 0.3)
                snapped = boundaries[b - 1] + SegmentDurationSec * 0.5;
            if (snapped >= boundaries[b + 1] - 0.3)
                snapped = ideal;
            boundaries[b] = Math.Clamp(snapped, 0, duration);
        }

        // Step 3: build fragments (merge too-short ones < 0.5s)
        double lastT = 0;
        for (int i = 1; i < boundaries.Count; i++)
        {
            if (i == boundaries.Count - 1 || boundaries[i] - lastT >= 0.5)
            {
                T1T2Array.Add((lastT, boundaries[i]));
                lastT = boundaries[i];
            }
            // else: skip — merge with next
        }
        // Ensure we reach the end
        if (T1T2Array.Count == 0 || T1T2Array[^1].T2 < duration - 0.1)
        {
            if (T1T2Array.Count > 0)
                T1T2Array[^1] = (T1T2Array[^1].T1, duration);
            else
                T1T2Array.Add((0.0, duration));
        }

        NumberPartsSilence = Silence.Count;
        Log.Info($"[INFO] {numSegments} ideal segments, {T1T2Array.Count} final fragments (snapped to silence)");
        return true;
    }

    /// <summary>Find nearest silence midpoint within searchRadius of idealSec.</summary>
    private double SnapToSilence(double idealSec, double searchRadius)
    {
        double bestDist = double.MaxValue;
        double bestMid = idealSec;

        foreach (var (start, end) in Silence)
        {
            double mid = (start + end) / 2.0;
            double dist = Math.Abs(mid - idealSec);
            if (dist <= searchRadius && dist < bestDist)
            {
                bestDist = dist;
                bestMid = mid;
            }
            // Also check edges of silence
            if (Math.Abs(start - idealSec) <= searchRadius && Math.Abs(start - idealSec) < bestDist)
            {
                bestDist = Math.Abs(start - idealSec);
                bestMid = start;
            }
            if (Math.Abs(end - idealSec) <= searchRadius && Math.Abs(end - idealSec) < bestDist)
            {
                bestDist = Math.Abs(end - idealSec);
                bestMid = end;
            }
        }
        return bestMid;
    }
}
