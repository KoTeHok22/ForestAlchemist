using System.IO;
using System.Text;
using UnityEngine;

public sealed class JsonPlayerQuestRepository : IPlayerQuestRepository
{
    private readonly string filePath;

    public JsonPlayerQuestRepository()
    {
        filePath = Path.Combine(Application.persistentDataPath, "player_quests.json");
    }

    public PlayerQuestSave Load()
    {
        if (!File.Exists(filePath))
            return new PlayerQuestSave();

        string json = File.ReadAllText(filePath, Encoding.UTF8);
        PlayerQuestSave save = JsonUtility.FromJson<PlayerQuestSave>(json);
        return save ?? new PlayerQuestSave();
    }

    public void Save(PlayerQuestSave data)
    {
        string directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(filePath, json, Encoding.UTF8);
    }
}
