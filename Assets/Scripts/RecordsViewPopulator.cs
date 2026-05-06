using System.Collections.Generic;
using TMPro;
using UnityEngine;

public interface IRecordsViewPopulator
{
    void Populate(IReadOnlyList<RecordEntryData> records);
    void Clear();
}

public sealed class RecordsViewPopulator : IRecordsViewPopulator
{
    private const string NicknameTextPath = "Nickname/Text (TMP)";
    private const string LevelTextPath = "Level/Text (TMP)";
    private const string PointsTextPath = "Points/Text (TMP)";

    private readonly GameObject ratingTemplate;
    private readonly Transform container;
    private readonly List<GameObject> spawnedEntries = new List<GameObject>();

    public RecordsViewPopulator(GameObject ratingTemplate, Transform container)
    {
        this.ratingTemplate = ratingTemplate;
        this.container = container;
    }

    public void Populate(IReadOnlyList<RecordEntryData> records)
    {
        Clear();

        if (records == null || records.Count == 0)
        {
            return;
        }

        for (int i = 0; i < records.Count; i++)
        {
            GameObject entry = Object.Instantiate(ratingTemplate, container);
            entry.SetActive(true);
            FillEntry(entry.transform, records[i]);
            spawnedEntries.Add(entry);
        }
    }

    public void Clear()
    {
        for (int i = spawnedEntries.Count - 1; i >= 0; i--)
        {
            if (spawnedEntries[i] != null)
            {
                Object.Destroy(spawnedEntries[i]);
            }
        }

        spawnedEntries.Clear();
    }

    private static void FillEntry(Transform entry, RecordEntryData data)
    {
        SetChildText(entry, NicknameTextPath, data.Nickname);
        SetChildText(entry, LevelTextPath, data.Level.ToString());
        SetChildText(entry, PointsTextPath, data.Score.ToString());
    }

    private static void SetChildText(Transform parent, string path, string value)
    {
        Transform child = parent.Find(path);

        if (child == null)
        {
            return;
        }

        TMP_Text text = child.GetComponent<TMP_Text>();

        if (text != null)
        {
            text.text = value;
        }
    }
}
