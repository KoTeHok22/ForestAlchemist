using System.Collections.Generic;

public interface IQuestRepository
{
    List<QuestData> LoadQuests();
}
