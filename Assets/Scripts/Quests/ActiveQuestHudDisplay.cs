using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public sealed class ActiveQuestHudDisplay : MonoBehaviour
{
    [SerializeField] private Transform tasksContainer;
    [SerializeField] private GameObject taskTemplate;

    private readonly List<GameObject> spawnedTasks = new List<GameObject>();
    private PlayerQuestService questService;
    private IQuestItemIconProvider iconProvider;

    public void Initialize(PlayerQuestService service, IQuestItemIconProvider provider)
    {
        questService = service;
        iconProvider = provider;
        questService.OnQuestsChanged += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        if (questService != null)
        {
            questService.OnQuestsChanged -= Refresh;
        }
    }

    public void Refresh()
    {
        ClearExistingTasks();

        if (questService == null)
            return;

        List<QuestData> activeQuests = questService.GetActiveQuests();
        foreach (QuestData quest in activeQuests)
        {
            CreateTaskEntry(quest);
        }

        taskTemplate.SetActive(false);
    }

    private void CreateTaskEntry(QuestData quest)
    {
        GameObject entry = Instantiate(taskTemplate, tasksContainer);
        entry.SetActive(true);

        Transform itemLogo = entry.transform.Find("ItemLogo");
        if (itemLogo != null)
        {
            Image logoImage = itemLogo.GetComponent<Image>();
            if (logoImage != null && iconProvider != null)
            {
                string targetId = quest.GetResolvedTargetId();
                Sprite icon = quest.type == QuestType.CollectItem && !string.IsNullOrEmpty(targetId) ? iconProvider.GetIcon(targetId) : null;
                if (icon != null)
                {
                    logoImage.sprite = icon;
                }
                else
                {
                    logoImage.enabled = false;
                }
            }
        }

        Transform itemCount = entry.transform.Find("ItemCount");
        if (itemCount != null)
        {
            TextMeshProUGUI countText = itemCount.GetComponent<TextMeshProUGUI>();
            if (countText != null)
                countText.text = quest.type == QuestType.CollectItem
                    ? $"x{quest.requiredCount} {ItemCatalog.GetDisplayName(quest.GetResolvedTargetId())}"
                    : $"x{quest.requiredCount}";
        }

        Transform cost = entry.transform.Find("Cost");
        if (cost != null)
        {
            TextMeshProUGUI costText = cost.GetComponent<TextMeshProUGUI>();
            if (costText != null)
                costText.text = $"{quest.rewardPoints}";
        }

        Transform description = entry.transform.Find("TaskDescription");
        if (description != null)
        {
            TextMeshProUGUI descText = description.GetComponent<TextMeshProUGUI>();
            if (descText != null)
                descText.text = quest.description;
        }

        spawnedTasks.Add(entry);
    }

    private void ClearExistingTasks()
    {
        foreach (GameObject task in spawnedTasks)
        {
            if (task != null)
                Destroy(task);
        }
        spawnedTasks.Clear();
    }
}
