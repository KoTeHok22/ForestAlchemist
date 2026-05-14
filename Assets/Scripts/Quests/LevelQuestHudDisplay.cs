using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public sealed class LevelQuestHudDisplay : MonoBehaviour
{
    [SerializeField] private Transform tasksContainer;
    [SerializeField] private GameObject taskTemplate;

    private readonly List<GameObject> spawnedTasks = new List<GameObject>();
    private PlayerQuestService questService;
    private IQuestItemIconProvider iconProvider;
    private PlayerInventory inventory;
    private readonly Dictionary<string, TextMeshProUGUI> countTexts = new Dictionary<string, TextMeshProUGUI>();

    public void Initialize(PlayerQuestService service, IQuestItemIconProvider provider, PlayerInventory inventory)
    {
        questService = service;
        iconProvider = provider;
        this.inventory = inventory;

        if (inventory != null)
            inventory.OnInventoryChanged += RefreshCounts;

        Refresh();
    }

    private void OnDestroy()
    {
        if (inventory != null)
            inventory.OnInventoryChanged -= RefreshCounts;
    }

    public void Refresh()
    {
        ClearExistingTasks();
        countTexts.Clear();

        if (questService == null)
            return;

        List<QuestData> activeQuests = questService.GetActiveQuests();
        foreach (QuestData quest in activeQuests)
        {
            CreateTaskEntry(quest);
        }

        taskTemplate.SetActive(false);
    }

    private void RefreshCounts()
    {
        if (questService == null || inventory == null)
            return;

        List<QuestData> activeQuests = questService.GetActiveQuests();
        foreach (QuestData quest in activeQuests)
        {
            if (countTexts.TryGetValue(quest.id, out TextMeshProUGUI text))
            {
                int collected = inventory.GetItemCount(quest.itemName);
                text.text = $"{collected}/{quest.requiredCount}";
            }
        }
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
                Sprite icon = quest.type == QuestType.CollectItem ? iconProvider.GetIcon(quest.itemName) : null;
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
            {
                int collected = quest.type == QuestType.CollectItem && inventory != null ? inventory.GetItemCount(quest.itemName) : QuestManager.Instance.GetProgress(quest.id);
                countText.text = $"{collected}/{quest.requiredCount}";
                countTexts[quest.id] = countText;
            }
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
