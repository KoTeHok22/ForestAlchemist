using UnityEngine;
using TMPro;
using System.Collections.Generic;

public sealed class ActiveQuestPanel : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Transform questListContainer;
    [SerializeField] private GameObject questEntryTemplate;
    [SerializeField] private TMP_Text questEntryPrefab;

    private List<TMP_Text> questEntries = new List<TMP_Text>();

    private void Start()
    {
        if (questEntryTemplate != null) questEntryTemplate.SetActive(false);
        Refresh();
    }

    private void OnEnable()
    {
        QuestManager.Instance.OnQuestProgressUpdated += OnQuestProgress;
        QuestManager.Instance.OnQuestCompleted += OnQuestCompleted;
    }

    private void OnDisable()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestProgressUpdated -= OnQuestProgress;
            QuestManager.Instance.OnQuestCompleted -= OnQuestCompleted;
        }
    }

    private void OnQuestProgress(string questId, int count)
    {
        Refresh();
    }

    private void OnQuestCompleted(string questId)
    {
        Refresh();
    }

    public void Refresh()
    {
        ClearEntries();

        LevelManager levelManager = FindFirstObjectByType<LevelManager>();
        if (levelManager == null) return;

        PlayerQuestService questService = levelManager.GetQuestService();
        if (questService == null) return;

        List<QuestData> activeQuests = questService.GetActiveQuests();
        for (int i = 0; i < activeQuests.Count; i++)
        {
            QuestData quest = activeQuests[i];
            int progress = QuestManager.Instance.GetProgress(quest.id);
            int required = quest.requiredCount;

            TMP_Text entry = CreateEntry();
            if (entry != null)
            {
                string status = progress >= required ? "<color=green>✓</color>" : $"[{progress}/{required}]";
                entry.text = $"{quest.description} {status}";
            }
        }
    }

    private TMP_Text CreateEntry()
    {
        if (questListContainer == null) return null;

        GameObject go = new GameObject("QuestEntry");
        go.transform.SetParent(questListContainer, false);
        TMP_Text text = go.AddComponent<TMP_Text>();
        text.fontSize = 14;
        text.color = Color.white;
        questEntries.Add(text);
        return text;
    }

    private void ClearEntries()
    {
        for (int i = questEntries.Count - 1; i >= 0; i--)
        {
            if (questEntries[i] != null) Destroy(questEntries[i].gameObject);
        }
        questEntries.Clear();
    }
}
