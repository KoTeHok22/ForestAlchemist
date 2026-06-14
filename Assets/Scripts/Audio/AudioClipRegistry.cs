using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AudioClipRegistry", menuName = "ForestAlchemist/Audio Clip Registry")]
public sealed class AudioClipRegistry : ScriptableObject
{
    [System.Serializable]
    public sealed class RegistryEntry
    {
        public string id;
        public AudioClip clip;
        [Range(0.05f, 4f)] public float volumeGain = 1f;
    }

    [SerializeField] private List<RegistryEntry> entries = new List<RegistryEntry>();

    public void Populate(Dictionary<string, AudioClip> clips, Dictionary<string, float> gains)
    {
        if (clips == null || gains == null)
        {
            return;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            RegistryEntry entry = entries[i];
            if (entry == null || string.IsNullOrEmpty(entry.id) || entry.clip == null)
            {
                continue;
            }

            clips[entry.id] = entry.clip;
            gains[entry.id] = Mathf.Clamp(entry.volumeGain, 0.05f, 4f);
        }
    }

#if UNITY_EDITOR
    public void SetEntries(List<RegistryEntry> newEntries)
    {
        entries = newEntries ?? new List<RegistryEntry>();
    }

    public IReadOnlyList<RegistryEntry> GetEntriesForEditor() => entries;
#endif
}
