using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class AudioCatalog
{
    private static readonly Dictionary<string, AudioClip> Clips = new Dictionary<string, AudioClip>();
    private static readonly Dictionary<string, float> Gains = new Dictionary<string, float>();
    private static bool loaded;

    public static AudioClip Get(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        EnsureLoaded();
        return Clips.TryGetValue(id, out AudioClip clip) ? clip : null;
    }

    public static float GetGain(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return 1f;
        }

        EnsureLoaded();
        return Gains.TryGetValue(id, out float gain) ? gain : 1f;
    }

    public static bool TryGet(string id, out AudioClip clip)
    {
        clip = Get(id);
        return clip != null;
    }

    private static void EnsureLoaded()
    {
        if (loaded)
        {
            return;
        }

        loaded = true;
        AudioClipRegistry registry = Resources.Load<AudioClipRegistry>("Audio/AudioClipRegistry");
        if (registry != null)
        {
            registry.Populate(Clips, Gains);
        }

#if UNITY_EDITOR
        if (Clips.Count == 0)
        {
            LoadFromAssetDatabase();
        }
#endif
    }

#if UNITY_EDITOR
    private static void LoadFromAssetDatabase()
    {
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio" });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
            AudioClip clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null)
            {
                continue;
            }

            string id = Path.GetFileNameWithoutExtension(path);
            Clips[id] = clip;
            Gains[id] = 1f;
        }
    }
#endif
}
