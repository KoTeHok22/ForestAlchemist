using System.Collections.Generic;
using UnityEngine;

public sealed class JsonQuestRepository : IQuestRepository
{
    private const string ResourcePath = "QuestData";

    public List<QuestData> LoadQuests()
    {
        TextAsset json = Resources.Load<TextAsset>(ResourcePath);
        if (json == null)
        {
            Debug.LogError($"Quest data not found at Resources/{ResourcePath}");
            return new List<QuestData>();
        }

        QuestDataCollection collection = JsonUtility.FromJson<QuestDataCollection>(json.text);
        return collection.quests ?? new List<QuestData>();
    }
}
