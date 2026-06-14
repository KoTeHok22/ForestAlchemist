using UnityEngine;

/// <summary>
/// Peak/RMS-based gain for evening out AI-generated clip levels. Used when rebuilding the registry.
/// </summary>
public static class AudioGainAnalyzer
{
    private const int MaxSamples = 44100 * 45;

    public static float EstimateGain(AudioClip clip, string clipId)
    {
        if (clip == null)
        {
            return 1f;
        }

        bool isMusic = !string.IsNullOrEmpty(clipId) && clipId.StartsWith("music_");
        float target = isMusic ? 0.12f : 0.18f;

        if (!TryMeasureLoudness(clip, out float loudness))
        {
            return isMusic ? 0.85f : 1f;
        }

        if (loudness < 0.0005f)
        {
            return 1f;
        }

        float gain = target / loudness;
        float minGain = isMusic ? 0.25f : 0.3f;
        float maxGain = isMusic ? 2.2f : 3.5f;
        return Mathf.Clamp(gain, minGain, maxGain);
    }

    private static bool TryMeasureLoudness(AudioClip clip, out float loudness)
    {
        loudness = 0f;
        int totalSamples = clip.samples * clip.channels;
        if (totalSamples <= 0)
        {
            return false;
        }

        int sampleCount = Mathf.Min(totalSamples, MaxSamples);
        float[] data = new float[sampleCount];

        try
        {
            if (!clip.GetData(data, 0))
            {
                return false;
            }
        }
        catch
        {
            return false;
        }

        float peak = 0f;
        float sumSq = 0f;
        for (int i = 0; i < data.Length; i++)
        {
            float abs = Mathf.Abs(data[i]);
            if (abs > peak)
            {
                peak = abs;
            }

            sumSq += data[i] * data[i];
        }

        float rms = Mathf.Sqrt(sumSq / data.Length);
        loudness = Mathf.Max(peak * 0.7f, rms * 2.2f);
        return true;
    }
}
