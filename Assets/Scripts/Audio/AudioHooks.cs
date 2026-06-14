public static class AudioHooks
{
    public static AudioManager Manager => AudioManager.Instance;

    public static AudioEventBridge Bridge =>
        Manager != null ? Manager.GetComponent<AudioEventBridge>() : null;

    public static void Sfx(string clipId, float volumeScale = 1f)
    {
        Manager?.PlaySfx(clipId, volumeScale);
    }

    public static void SfxUnscaled(string clipId, float volumeScale = 1f)
    {
        Manager?.PlaySfxUnscaled(clipId, volumeScale);
    }

    public static void SfxAtPoint(string clipId, UnityEngine.Vector3 position, float volumeScale = 1f)
    {
        Manager?.PlaySfxAtPoint(clipId, position, volumeScale);
    }

    public static void PanelOpen() { }

    public static void PanelClose() => Manager?.PlayUiPanelClose();
}
