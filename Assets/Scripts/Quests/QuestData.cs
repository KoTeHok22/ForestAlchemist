using System;
using System.Collections.Generic;

[Serializable]
public sealed class QuestDataCollection
{
    public List<QuestData> quests;
}

public enum QuestType
{
    CollectItem,
    KillEnemy,
    ReachLocation,
    ActivateAltar,
    DefeatBoss
}

[Serializable]
public sealed class QuestData
{
    public string id;
    public string description;
    public QuestType type = QuestType.CollectItem;
    public string targetId; // ItemName, EnemyType, or LocationId
    public int requiredCount;
    public int rewardPoints;
    public string weatherId;
    
    // Legacy support for itemName
    public string itemName => targetId;

    public string GetResolvedTargetId()
    {
        return string.IsNullOrEmpty(targetId) ? string.Empty : targetId;
    }
}
