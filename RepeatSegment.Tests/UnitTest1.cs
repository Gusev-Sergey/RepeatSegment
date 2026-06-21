using RepeatSegment.App;

namespace RepeatSegment.Tests;

public class AudioEngineTests
{
    private const int SampleRate = 44100;

    [Fact]
    public void StretchSola_Speed1_ReturnsClone()
    {
        float[] input = GenerateSine(440, SampleRate, 1.0f);
        float[] result = AudioEngine.StretchSola(input, 1.0);
        Assert.Equal(input.Length, result.Length);
        for (int i = 0; i < input.Length; i++)
            Assert.Equal(input[i], result[i], 3);
    }

    [Fact]
    public void StretchSola_Speed2_ShorterOutput()
    {
        float[] input = GenerateSine(440, SampleRate, 2.0f);
        float[] result = AudioEngine.StretchSola(input, 2.0);
        // At 2x speed, output should be roughly half the input
        double ratio = (double)result.Length / input.Length;
        Assert.True(ratio > 0.4 && ratio < 0.65, $"Expected ratio ~0.5, got {ratio:F2}");
    }

    [Fact]
    public void StretchSola_Speed05_LongerOutput()
    {
        float[] input = GenerateSine(440, SampleRate, 2.0f);
        float[] result = AudioEngine.StretchSola(input, 0.5);
        // At 0.5x speed, output should be roughly double the input
        double ratio = (double)result.Length / input.Length;
        Assert.True(ratio > 1.5 && ratio < 2.5, $"Expected ratio ~2.0, got {ratio:F2}");
    }

    [Fact]
    public void StretchSola_NoClipping()
    {
        float[] input = GenerateSine(440, SampleRate, 3.0f);
        foreach (float speed in new[] { 0.4, 0.7, 1.0, 1.3, 1.5 })
        {
            float[] result = AudioEngine.StretchSola(input, speed);
            Assert.All(result, sample => Assert.True(Math.Abs(sample) <= 1.0f, $"Clipping at speed {speed}"));
        }
    }

    [Fact]
    public void StretchSola_ShortInput_ReturnsClone()
    {
        float[] input = new float[100]; // shorter than frameSize=4096
        float[] result = AudioEngine.StretchSola(input, 0.5);
        Assert.Equal(input.Length, result.Length);
    }

    [Fact]
    public void StretchSola_ZeroInput_NoCrash()
    {
        float[] input = new float[4096];
        float[] result = AudioEngine.StretchSola(input, 0.4);
        Assert.NotEmpty(result);
        Assert.True(result.Length > 0);
    }

    private static float[] GenerateSine(float freq, int sampleRate, float durationSec)
    {
        int count = (int)(sampleRate * durationSec);
        var samples = new float[count];
        for (int i = 0; i < count; i++)
            samples[i] = MathF.Sin(2f * MathF.PI * freq * i / sampleRate);
        return samples;
    }
}

public class SilenceDetectorTests
{
    [Fact]
    public void Detect_SilenceAtEdges_FindsZones()
    {
        int sampleRate = 44100;
        float duration = 10.0f;
        int total = (int)(sampleRate * duration);
        var samples = new float[total];
        // Put a loud tone in middle, silence at edges
        for (int i = 0; i < total; i++)
        {
            double t = (double)i / sampleRate;
            if (t > 1.0 && t < 9.0)
                samples[i] = MathF.Sin(2f * MathF.PI * 440f * (float)t) * 0.8f;
            // else silence (0)
        }

        var detector = new SilenceDetector();
        bool ok = detector.Detect(samples, sampleRate, duration, 3.0);

        Assert.True(ok);
        Assert.NotEmpty(detector.T1T2Array);
        // First segment should start near 0, last end near 10
        var frags = detector.T1T2Array.ToArray();
        Assert.True(frags[0].T1 < 1.5, $"First T1={frags[0].T1:F1} too far from 0");
        Assert.True(frags[^1].T2 > 8.5, $"Last T2={frags[^1].T2:F1} too far from 10");
    }

    [Fact]
    public void Detect_PureSignal_NoFragments()
    {
        int sampleRate = 44100;
        float duration = 3.0f;
        int total = (int)(sampleRate * duration);
        var samples = new float[total];
        for (int i = 0; i < total; i++)
            samples[i] = MathF.Sin(2f * MathF.PI * 440f * i / sampleRate) * 0.5f;

        var detector = new SilenceDetector();
        // Full signal — should return one big fragment or none
        detector.Detect(samples, sampleRate, duration, 5.0);
        var frags = detector.T1T2Array.ToArray();
        Assert.True(frags.Length <= 1);
    }
}

public class ConfigManagerTests
{
    [Fact]
    public void Load_Defaults_AreCorrect()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "rs_test_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(tempDir);
        try
        {
            File.WriteAllText(Path.Combine(tempDir, "config.ini"),
                "[Settings]\npath = \nfile = \nposition = 0\ncounter = 0\nsegment_duration_sec = 5.0\nlanguage =\ntheme = light\nmp3_bitrate = 128\n\n[Transcription]\nproviders_enabled = deepgram\nassemblyai_api_key = \ndeepgram_api_key = \nchunk_minutes = 10\nplayback_latency = 0.32\n",
                System.Text.Encoding.UTF8);
            File.WriteAllText(Path.Combine(tempDir, ".env"), "\n", System.Text.Encoding.UTF8);

            var cfg = new ConfigManager(tempDir);
            bool ok = cfg.Load();
            Assert.True(ok);
            Assert.Equal(5.0, cfg.SegmentDurationSec, 3);
            Assert.Equal("light", cfg.Theme);
            Assert.Equal(128, cfg.Mp3BitrateKbps);
            Assert.Equal(10, cfg.ChunkMinutes);
        }
        finally { try { Directory.Delete(tempDir, true); } catch { } }
    }
}
