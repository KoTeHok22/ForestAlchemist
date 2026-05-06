using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public sealed class QuestBoardGenerator : MonoBehaviour
{
    [SerializeField] private Transform tasksContainer;
    [SerializeField] private GameObject taskTemplate;
    [SerializeField] private DeskItemIconProvider iconProvider;
    [SerializeField] private ActiveQuestHudDisplay hudDisplay;

    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color acceptedColor = new Color(0.6f, 1f, 0.6f, 1f);

    private readonly List<GameObject> spawnedTasks = new List<GameObject>();
    private PlayerQuestService questService;

    private void Awake()
    {
        questService = new PlayerQuestService(
            new JsonPlayerQuestRepository(),
            new JsonQuestRepository()
        );

        if (hudDisplay != null)
            hudDisplay.Initialize(questService, iconProvider);
    }

    private void Start()
    {
        GenerateBoard();
    }

    public void GenerateBoard()
    {
        ClearExistingTasks();

        List<QuestData> boardQuests = questService.GetBoardQuests();

        foreach (QuestData quest in boardQuests)
        {
            CreateTaskEntry(quest);
        }

        taskTemplate.SetActive(false);
    }

    private void CreateTaskEntry(QuestData quest)
    {
        GameObject entry = Instantiate(taskTemplate, tasksContainer);
        entry.SetActive(true);

        bool isActive = questService.IsQuestActive(quest.id);

        Image entryImage = entry.GetComponent<Image>();
        if (entryImage != null)
            entryImage.color = isActive ? acceptedColor : normalColor;

        Transform itemLogo = entry.transform.Find("ItemLogo");
        if (itemLogo != null)
        {
            Image logoImage = itemLogo.GetComponent<Image>();
            if (logoImage != null)
            {
                Sprite icon = iconProvider.GetIcon(quest.itemName);
                if (icon != null)
                    logoImage.sprite = icon;
            }
        }

        Transform itemCount = entry.transform.Find("ItemCount");
        if (itemCount != null)
        {
            TextMeshProUGUI countText = itemCount.GetComponent<TextMeshProUGUI>();
            if (countText != null)
                countText.text = $"x{quest.requiredCount}";
        }

        Transform cost = entry.transform.Find("Cost");
        if (cost != null)
        {
            TextMeshProUGUI costText = cost.GetComponent<TextMeshProUGUI>();
            if (costText != null)
                costText.text = $"{quest.rewardPoints} очков";
        }

        Transform description = entry.transform.Find("TaskDescription");
        if (description != null)
        {
            TextMeshProUGUI descText = description.GetComponent<TextMeshProUGUI>();
            if (descText != null)
                descText.text = quest.description;
        }

        Button button = entry.GetComponent<Button>();
        if (button == null)
            button = entry.AddComponent<Button>();

        if (isActive)
        {
            button.interactable = false;
        }
        else
        {
            string capturedId = quest.id;
            button.onClick.AddListener(() => OnQuestClicked(capturedId));
        }

        spawnedTasks.Add(entry);
    }

    private void OnQuestClicked(string questId)
    {
        if (questService.TryAcceptQuest(questId))
        {
            GenerateBoard();

            if (hudDisplay != null)
                hudDisplay.Refresh();
        }
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
