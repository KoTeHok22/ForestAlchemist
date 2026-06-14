/// <summary>
/// Per-clip volume resolution. All clips play at unity gain (1.0).
/// </summary>
public static class AudioMixProfile
{
    public static float ResolveSfxVolume(string clipId, float callScale = 1f)
    {
        if (string.IsNullOrEmpty(clipId))
        {
            return 0f;
        }

        return AudioCatalog.GetGain(clipId) * callScale;
    }

    public static float ResolveMusicVolume(string clipId, float layerScale = 1f)
    {
        if (string.IsNullOrEmpty(clipId))
        {
            return 0f;
        }

        return AudioCatalog.GetGain(clipId) * layerScale;
    }
}
