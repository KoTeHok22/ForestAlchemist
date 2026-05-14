using UnityEngine;
using System;
using System.Collections.Generic;

public sealed class QuestManager : MonoBehaviour
{
    private static QuestManager instance;
    public static QuestManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("QuestManager");
                instance = go.AddComponent<QuestManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    public event Action<string, int> OnQuestProgressUpdated;
    public event Action<WeatherSystem.WeatherType> OnWeatherQuestTriggered;
    public event Action<string> OnQuestCompleted;
    public event Action<int> OnQuestRewardGranted;

    private Dictionary<string, int> questProgress = new Dictionary<string, int>();
    private HashSet<string> activatedAltars = new HashSet<string>();
    private HashSet<string> completedQuests = new HashSet<string>();
    private WeatherSystem.WeatherType currentWeather = WeatherSystem.WeatherType.Clear;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ReportEnemyKilled(string enemyType)
    {
        UpdateProgress(QuestType.KillEnemy, enemyType, 1);
    }

    public void ReportItemCollected(string itemName, int amount)
    {
        itemName = ItemCatalog.Normalize(itemName);
        UpdateProgress(QuestType.CollectItem, itemName, amount);
    }

    public void ReportAltarActivated(string altarId, ElementType element)
    {
        if (activatedAltars.Contains(altarId)) return;
        activatedAltars.Add(altarId);
        UpdateProgress(QuestType.ActivateAltar, $"altar_{element}", 1);
    }

    public void ReportLocationReached(string locationId)
    {
        UpdateProgress(QuestType.ReachLocation, locationId, 1);
    }

    public void ReportBossDefeated(string bossId)
    {
        UpdateProgress(QuestType.DefeatBoss, bossId, 1);
    }

    public void ReportWeatherChanged(WeatherSystem.WeatherType weather)
    {
        currentWeather = weather;
        OnWeatherQuestTriggered?.Invoke(weather);
    }

    public WeatherSystem.WeatherType GetCurrentWeather() => currentWeather;

    private void UpdateProgress(QuestType type, string targetId, int amount)
    {
        if (GameCore.Instance.AccountService == null) return;

        var activeQuests = UnityEngine.Object.FindAnyObjectByType<LevelManager>()?.GetQuestService()?.GetActiveQuests();
        if (activeQuests == null) return;

        foreach (var quest in activeQuests)
        {
            if (quest.type == type && IsMatchingTarget(quest, targetId))
            {
                if (!questProgress.ContainsKey(quest.id)) questProgress[quest.id] = 0;
                questProgress[quest.id] = Mathf.Min(quest.requiredCount, questProgress[quest.id] + amount);
                OnQuestProgressUpdated?.Invoke(quest.id, questProgress[quest.id]);

                if (questProgress[quest.id] >= quest.requiredCount)
                {
                    CompleteQuest(quest.id);
                }
            }
        }
    }

    public void ReportReturnPointReached(string locationId)
    {
        UpdateProgress(QuestType.ReachLocation, locationId, 1);
    }

    public int GetProgress(string questId)
    {
        if (questProgress.TryGetValue(questId, out int val)) return val;
        return 0;
    }

    public void ResetExpeditionProgress()
    {
        questProgress.Clear();
        activatedAltars.Clear();
        completedQuests.Clear();
    }

    private bool IsMatchingTarget(QuestData quest, string targetId)
    {
        if (quest == null)
        {
            return false;
        }

        string questTarget = quest.GetResolvedTargetId();
        if (string.Equals(questTarget, targetId, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (quest.type == QuestType.ActivateAltar && !string.IsNullOrEmpty(questTarget) && targetId.StartsWith("altar_", StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(questTarget, targetId, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private void CompleteQuest(string questId)
    {
        if (string.IsNullOrEmpty(questId) || !completedQuests.Add(questId))
        {
            return;
        }

        LevelManager levelManager = UnityEngine.Object.FindAnyObjectByType<LevelManager>();
        PlayerQuestService service = levelManager?.GetQuestService();
        QuestData quest = service?.GetActiveQuests()?.Find(activeQuest => activeQuest.id == questId);
        service?.CompleteQuest(questId);

        if (quest != null)
        {
            int reward = GetBloodReward(quest);
            ExpeditionManager.Instance.ExpeditionInventory?.AddItem(ItemCatalog.OrcBlood, reward);
            OnQuestRewardGranted?.Invoke(reward);
        }

        OnQuestCompleted?.Invoke(questId);
        GameCore.Instance.SaveProgress();
    }

    private static int GetBloodReward(QuestData quest)
    {
        if (quest == null)
        {
            return 0;
        }

        if (quest.type == QuestType.DefeatBoss)
        {
            return 6;
        }

        if (quest.type == QuestType.ActivateAltar || quest.type == QuestType.ReachLocation)
        {
            return 3;
        }

        if (quest.requiredCount >= 5)
        {
            return 3;
        }

        return 2;
    }
}
