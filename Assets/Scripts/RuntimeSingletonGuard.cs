using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Prevents lazy singletons from spawning new GameObjects while Unity tears down
/// play mode or unloads scenes (avoids "objects were not cleaned up" warnings).
/// </summary>
public static class RuntimeSingletonGuard
{
    public static bool IsShuttingDown { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        IsShuttingDown = false;
    }

    public static void MarkShuttingDown()
    {
        IsShuttingDown = true;
    }

#if UNITY_EDITOR
    [InitializeOnLoadMethod]
    private static void RegisterEditorHooks()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            MarkShuttingDown();
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            IsShuttingDown = false;
        }
    }
#endif
}
