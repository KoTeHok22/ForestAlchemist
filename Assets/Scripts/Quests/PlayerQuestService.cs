using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class PlayerQuestService
{
    private const int MaxActiveQuests = 2;
    private const int BoardSize = 4;

    private readonly IPlayerQuestRepository questRepository;
    private readonly IQuestRepository dataRepository;
    private PlayerQuestSave saveData;

    public event Action OnQuestsChanged;

    public PlayerQuestService(IPlayerQuestRepository questRepository, IQuestRepository dataRepository)
    {
        this.questRepository = questRepository;
        this.dataRepository = dataRepository;
        saveData = questRepository.Load();
    }

    public List<QuestData> GetBoardQuests()
    {
        List<QuestData> allQuests = dataRepository.LoadQuests();

        if (saveData.boardQuestIds.Count == 0)
        {
            GenerateNewBoard(allQuests);
        }

        List<QuestData> result = new List<QuestData>();
        foreach (string id in saveData.boardQuestIds)
        {
            QuestData quest = allQuests.FirstOrDefault(q => q.id == id);
            if (quest != null)
                result.Add(quest);
        }

        return result;
    }

    public List<QuestData> GetActiveQuests()
    {
        List<QuestData> allQuests = dataRepository.LoadQuests();
        List<QuestData> result = new List<QuestData>();

        foreach (string id in saveData.activeQuestIds)
        {
            QuestData quest = allQuests.FirstOrDefault(q => q.id == id);
            if (quest != null)
                result.Add(quest);
        }

        return result;
    }

    public bool CanAcceptQuest()
    {
        return saveData.activeQuestIds.Count < MaxActiveQuests;
    }

    public bool IsQuestActive(string questId)
    {
        return saveData.activeQuestIds.Contains(questId);
    }

    public bool TryAcceptQuest(string questId)
    {
        if (!CanAcceptQuest())
            return false;

        if (IsQuestActive(questId))
            return false;

        if (!saveData.boardQuestIds.Contains(questId))
            return false;

        saveData.activeQuestIds.Add(questId);
        questRepository.Save(saveData);
        OnQuestsChanged?.Invoke();
        return true;
    }

    private void GenerateNewBoard(List<QuestData> allQuests)
    {
        List<QuestData> available = allQuests
            .Where(q => !saveData.activeQuestIds.Contains(q.id))
            .ToList();

        Shuffle(available);

        int count = Mathf.Min(BoardSize, available.Count);
        saveData.boardQuestIds.Clear();
        for (int i = 0; i < count; i++)
        {
            saveData.boardQuestIds.Add(available[i].id);
        }

        questRepository.Save(saveData);
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
