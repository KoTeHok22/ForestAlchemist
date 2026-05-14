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

    private Dictionary<string, int> questProgress = new Dictionary<string, int>();
    private HashSet<string> activatedAltars = new HashSet<string>();
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
        var progress = GameCore.Instance.AccountService;
        if (progress == null) return;

        var activeQuests = UnityEngine.Object.FindAnyObjectByType<LevelManager>()?.GetQuestService()?.GetActiveQuests();
        if (activeQuests == null) return;

        foreach (var quest in activeQuests)
        {
            if (quest.type == type && quest.targetId == targetId)
            {
                if (!questProgress.ContainsKey(quest.id)) questProgress[quest.id] = 0;
                questProgress[quest.id] += amount;
                OnQuestProgressUpdated?.Invoke(quest.id, questProgress[quest.id]);
            }
        }
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
    }
}
