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
    private bool runtimeUiBuilt;

    private void Awake()
    {
        BuildRuntimeUiIfNeeded();
    }

    public void Initialize(PlayerQuestService service, IQuestItemIconProvider provider, PlayerInventory inventory)
    {
        BuildRuntimeUiIfNeeded();
        questService = service;
        iconProvider = provider;
        this.inventory = inventory;

        if (inventory != null)
            inventory.OnInventoryChanged += RefreshCounts;

        QuestManager.Instance.OnQuestProgressUpdated += HandleQuestProgressChanged;
        QuestManager.Instance.OnQuestCompleted += HandleQuestCompleted;

        Refresh();
    }

    private void OnDestroy()
    {
        if (inventory != null)
            inventory.OnInventoryChanged -= RefreshCounts;

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestProgressUpdated -= HandleQuestProgressChanged;
            QuestManager.Instance.OnQuestCompleted -= HandleQuestCompleted;
        }
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
                if (quest.type == QuestType.CollectItem)
                {
                    string targetId = quest.GetResolvedTargetId();
                    int collected = !string.IsNullOrEmpty(targetId) ? inventory.GetItemCount(targetId) : 0;
                    text.text = $"{collected}/{quest.requiredCount}";
                }
                else
                {
                    text.text = $"{QuestManager.Instance.GetProgress(quest.id)}/{quest.requiredCount}";
                }
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
                string targetId = quest.GetResolvedTargetId();
                int collected = quest.type == QuestType.CollectItem && inventory != null && !string.IsNullOrEmpty(targetId)
                    ? inventory.GetItemCount(targetId)
                    : QuestManager.Instance.GetProgress(quest.id);
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
                descText.text = GetQuestDisplayText(quest);
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

    private void HandleQuestProgressChanged(string questId, int value)
    {
        RefreshCounts();
    }

    private void HandleQuestCompleted(string questId)
    {
        Refresh();
    }

    private void BuildRuntimeUiIfNeeded()
    {
        if (runtimeUiBuilt || (tasksContainer != null && taskTemplate != null))
        {
            runtimeUiBuilt = true;
            return;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            return;
        }

        GameObject panel = new GameObject("RuntimeQuestPanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        panel.transform.SetParent(canvas.transform, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-20f, -20f);
        rect.sizeDelta = new Vector2(360f, 260f);

        panel.GetComponent<Image>().color = new Color(0.1f, 0.12f, 0.14f, 0.95f);

        VerticalLayoutGroup layout = panel.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;

        TextMeshProUGUI title = CreateText(panel.transform, "Активные задания", 24, FontStyles.Bold);
        title.alignment = TextAlignmentOptions.Center;

        GameObject content = new GameObject("TasksContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
        content.transform.SetParent(panel.transform, false);
        VerticalLayoutGroup contentLayout = content.GetComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 6f;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = false;
        contentLayout.childForceExpandHeight = false;
        content.AddComponent<LayoutElement>().preferredHeight = 200f;
        tasksContainer = content.transform;

        taskTemplate = CreateRuntimeTaskTemplate(tasksContainer);
        taskTemplate.SetActive(false);
        runtimeUiBuilt = true;
    }

    private static GameObject CreateRuntimeTaskTemplate(Transform parent)
    {
        GameObject root = new GameObject("QuestTaskTemplate", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        root.transform.SetParent(parent, false);
        root.GetComponent<Image>().color = new Color(0.18f, 0.22f, 0.26f, 1f);
        root.GetComponent<LayoutElement>().preferredHeight = 58f;

        HorizontalLayoutGroup layout = root.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.spacing = 8f;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        GameObject icon = new GameObject("ItemLogo", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        icon.transform.SetParent(root.transform, false);
        icon.GetComponent<LayoutElement>().preferredWidth = 36f;

        GameObject desc = new GameObject("TaskDescription", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        desc.transform.SetParent(root.transform, false);
        desc.GetComponent<LayoutElement>().preferredWidth = 190f;
        TextMeshProUGUI descText = desc.GetComponent<TextMeshProUGUI>();
        descText.fontSize = 16;
        descText.color = Color.white;
        descText.enableWordWrapping = true;

        GameObject count = new GameObject("ItemCount", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        count.transform.SetParent(root.transform, false);
        count.GetComponent<LayoutElement>().preferredWidth = 60f;
        TextMeshProUGUI countText = count.GetComponent<TextMeshProUGUI>();
        countText.fontSize = 16;
        countText.color = new Color(0.9f, 0.95f, 1f, 1f);
        countText.alignment = TextAlignmentOptions.Center;

        GameObject cost = new GameObject("Cost", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        cost.transform.SetParent(root.transform, false);
        cost.GetComponent<LayoutElement>().preferredWidth = 50f;
        TextMeshProUGUI costText = cost.GetComponent<TextMeshProUGUI>();
        costText.fontSize = 16;
        costText.color = new Color(1f, 0.85f, 0.45f, 1f);
        costText.alignment = TextAlignmentOptions.Center;

        return root;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string content, float size, FontStyles style)
    {
        GameObject textObject = new GameObject("Text", typeof(RectTransform));
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = Color.white;
        return text;
    }

    private static string GetQuestDisplayText(QuestData quest)
    {
        return quest.type switch
        {
            QuestType.ReachLocation => $"Доберись до точки: {quest.description}",
            QuestType.ActivateAltar => $"Активируй алтарь: {quest.description}",
            QuestType.DefeatBoss => $"Победи босса: {quest.description}",
            QuestType.KillEnemy => $"Уничтожай врагов: {quest.description}",
            _ => quest.description
        };
    }
}
