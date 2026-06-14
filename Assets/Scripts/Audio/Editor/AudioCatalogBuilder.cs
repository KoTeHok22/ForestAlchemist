using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class AudioCatalogBuilder
{
    private const string RegistryPath = "Assets/Resources/Audio/AudioClipRegistry.asset";

    [MenuItem("ForestAlchemist/Audio/Rebuild Clip Registry")]
    public static void RebuildRegistry()
    {
        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio" });
        List<AudioClipRegistry.RegistryEntry> entries = new List<AudioClipRegistry.RegistryEntry>(guids.Length);

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null)
            {
                continue;
            }

            string id = Path.GetFileNameWithoutExtension(path);
            entries.Add(new AudioClipRegistry.RegistryEntry
            {
                id = id,
                clip = clip,
                volumeGain = 1f
            });
        }

        entries.Sort((a, b) => string.CompareOrdinal(a.id, b.id));

        AudioClipRegistry registry = AssetDatabase.LoadAssetAtPath<AudioClipRegistry>(RegistryPath);
        if (registry == null)
        {
            string directory = Path.GetDirectoryName(RegistryPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            registry = ScriptableObject.CreateInstance<AudioClipRegistry>();
            AssetDatabase.CreateAsset(registry, RegistryPath);
        }

        registry.SetEntries(entries);
        EditorUtility.SetDirty(registry);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[AudioCatalogBuilder] Registered {entries.Count} clips at unity gain (1.0) in {RegistryPath}");
    }
}
