using System;
using System.Collections.Generic;

[Serializable]
public sealed class QuestDataCollection
{
    public List<QuestData> quests;
}

[Serializable]
public sealed class QuestData
{
    public string id;
    public string description;
    public string itemName;
    public int requiredCount;
    public int rewardPoints;
}
