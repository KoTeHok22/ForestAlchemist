using UnityEngine;

/// <summary>
/// Toggle spell projectile tracing in the Console. Search filter: [SpellDbg]
/// </summary>
public static class SpellProjectileDebug
{
    public const string Tag = "[SpellDbg]";

    /// <summary>Master switch — set false when done investigating.</summary>
    public static bool Enabled = true;

    /// <summary>Log position every N frames while projectile lives.</summary>
    public static int PositionLogIntervalFrames = 10;

    public static void Log(string message, Object context = null)
    {
        if (!Enabled) return;
        Debug.Log($"{Tag} {message}", context);
    }

    public static void LogWarning(string message, Object context = null)
    {
        if (!Enabled) return;
        Debug.LogWarning($"{Tag} {message}", context);
    }
}
