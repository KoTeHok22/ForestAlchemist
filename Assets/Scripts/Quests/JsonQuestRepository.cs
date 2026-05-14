using System.Collections.Generic;
using UnityEngine;

public sealed class JsonQuestRepository : IQuestRepository
{
    private const string ResourcePath = "QuestData";

    [System.Serializable]
    private sealed class LegacyQuestDataCollection
    {
        public List<LegacyQuestData> quests;
    }

    [System.Serializable]
    private sealed class LegacyQuestData
    {
        public string id;
        public string description;
        public string itemName;
        public int requiredCount;
        public int rewardPoints;
    }

    public List<QuestData> LoadQuests()
    {
        TextAsset json = Resources.Load<TextAsset>(ResourcePath);
        if (json == null)
        {
            return CreateFallbackQuests();
        }

        QuestDataCollection collection = JsonUtility.FromJson<QuestDataCollection>(json.text);
        List<QuestData> quests = collection?.quests ?? new List<QuestData>();

        if (NeedsLegacyImport(quests))
        {
            quests = ImportLegacyQuests(json.text);
        }

        NormalizeQuests(quests);

        if (quests.Count == 0)
        {
            return CreateFallbackQuests();
        }

        return quests;
    }

    private static void NormalizeQuests(List<QuestData> quests)
    {
        for (int i = 0; i < quests.Count; i++)
        {
            QuestData quest = quests[i];
            if (quest == null)
            {
                continue;
            }

            quest.targetId = ItemCatalog.Normalize(quest.targetId);

            if (quest.requiredCount <= 0)
            {
                quest.requiredCount = 1;
            }

            if (string.IsNullOrEmpty(quest.description))
            {
                quest.description = BuildDescription(quest);
            }
        }
    }

    private static bool NeedsLegacyImport(List<QuestData> quests)
    {
        if (quests == null || quests.Count == 0)
        {
            return false;
        }

        for (int i = 0; i < quests.Count; i++)
        {
            if (quests[i] != null && !string.IsNullOrEmpty(quests[i].targetId))
            {
                return false;
            }
        }

        return true;
    }

    private static List<QuestData> ImportLegacyQuests(string json)
    {
        LegacyQuestDataCollection legacy = JsonUtility.FromJson<LegacyQuestDataCollection>(json);
        List<QuestData> result = new List<QuestData>();
        if (legacy?.quests == null)
        {
            return result;
        }

        for (int i = 0; i < legacy.quests.Count; i++)
        {
            LegacyQuestData source = legacy.quests[i];
            if (source == null)
            {
                continue;
            }

            result.Add(new QuestData
            {
                id = source.id,
                description = source.description,
                type = QuestType.CollectItem,
                targetId = source.itemName,
                requiredCount = source.requiredCount,
                rewardPoints = source.rewardPoints
            });
        }

        return result;
    }

    private static string BuildDescription(QuestData quest)
    {
        return quest.type switch
        {
            QuestType.KillEnemy => $"Уничтожь {quest.requiredCount} целей: {quest.targetId}",
            QuestType.ReachLocation => $"Доберись до точки: {quest.targetId}",
            QuestType.ActivateAltar => $"Активируй алтарь: {quest.targetId}",
            QuestType.DefeatBoss => $"Победи босса: {quest.targetId}",
            _ => $"Собери {quest.requiredCount}x {quest.targetId}"
        };
    }

    private static List<QuestData> CreateFallbackQuests()
    {
        return new List<QuestData>
        {
            new QuestData
            {
                id = "collect_blood",
                description = "Добудь орочью кровь в лесу",
                type = QuestType.CollectItem,
                targetId = ItemCatalog.OrcBlood,
                requiredCount = 5,
                rewardPoints = 120
            },
            new QuestData
            {
                id = "collect_sakura",
                description = "Найди редкие саженцы сакуры",
                type = QuestType.CollectItem,
                targetId = ItemCatalog.SakuraSapling,
                requiredCount = 3,
                rewardPoints = 140
            },
            new QuestData
            {
                id = "reach_evac",
                description = "Доберись до точки эвакуации в чаще",
                type = QuestType.ReachLocation,
                targetId = "evacuation_point",
                requiredCount = 1,
                rewardPoints = 180
            },
            new QuestData
            {
                id = "activate_fire_altar",
                description = "Активируй огненный алтарь",
                type = QuestType.ActivateAltar,
                targetId = "altar_Fire",
                requiredCount = 1,
                rewardPoints = 200
            },
            new QuestData
            {
                id = "defeat_orc_leader",
                description = "Победи вождя орков",
                type = QuestType.DefeatBoss,
                targetId = "BossOrc",
                requiredCount = 1,
                rewardPoints = 320
            }
        };
    }
}
