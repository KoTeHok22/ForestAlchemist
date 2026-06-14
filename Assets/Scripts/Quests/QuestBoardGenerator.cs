using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Populates the App UI doska bulletin board with quest cards.
/// Wraps PlayerQuestService and feeds the runtime ActiveQuestHudDisplay.
/// </summary>
public sealed class QuestBoardGenerator : MonoBehaviour
{
    [SerializeField] private DeskItemIconProvider iconProvider;
    [SerializeField] private ActiveQuestHudDisplay hudDisplay;
    [SerializeField] private DeskBoardAppUI boardUI;

    private PlayerQuestService questService;

    public PlayerQuestService Service => questService;

    private void Awake()
    {
        questService = new PlayerQuestService(
            new JsonPlayerQuestRepository(),
            new JsonQuestRepository()
        );

        if (hudDisplay != null)
            hudDisplay.Initialize(questService, iconProvider);

        if (boardUI == null)
            boardUI = GetComponent<DeskBoardAppUI>();
    }

    private void Start()
    {
        // Start() runs each time the Home scene loads (i.e. after returning from
        // an expedition) — this is where the board pool refreshes: accepted/done
        // quests leave the board and fresh postings fill the empty slots.
        RefreshBoard();
    }

    /// <summary>
    /// Refreshes the board pool (drops accepted quests, refills empty slots) and
    /// re-renders. Call this on return home / after an expedition — NOT on accept.
    /// </summary>
    public void RefreshBoard()
    {
        questService.EnsureBoardRefreshed();
        GenerateBoard();
    }

    /// <summary>
    /// Renders the current board state without touching the pool. Accepted quests
    /// stay on the board rendered greyed-out (quest-card--accepted).
    /// </summary>
    public void GenerateBoard()
    {
        if (boardUI == null) return;
        if (boardUI.TasksGrid == null)
        {
            // UI not built yet — defer until first Open.
            return;
        }

        VisualElement grid = boardUI.TasksGrid;
        grid.Clear();

        List<QuestData> boardQuests = questService.GetBoardQuests();
        foreach (QuestData quest in boardQuests)
        {
            grid.Add(BuildCard(quest));
        }
    }

    private VisualElement BuildCard(QuestData quest)
    {
        bool isActive = questService.IsQuestActive(quest.id);

        var card = new VisualElement();
        card.AddToClassList("quest-card");
        if (isActive) card.AddToClassList("quest-card--accepted");

        // Head: icon + count/goal
        var head = new VisualElement();
        head.AddToClassList("quest-card__head");

        if (quest.type == QuestType.CollectItem && iconProvider != null)
        {
            string targetId = quest.GetResolvedTargetId();
            Sprite icon = !string.IsNullOrEmpty(targetId) ? iconProvider.GetIcon(targetId) : null;
            if (icon != null)
            {
                var iconEl = new VisualElement();
                iconEl.AddToClassList("quest-card__icon");
                iconEl.style.backgroundImage = new StyleBackground(icon);
                head.Add(iconEl);
            }
        }

        var count = new Label(quest.type == QuestType.CollectItem
            ? $"x{quest.requiredCount} {ItemCatalog.GetDisplayName(quest.GetResolvedTargetId())}"
            : $"Цель: {quest.requiredCount}");
        count.AddToClassList("quest-card__count");
        head.Add(count);

        card.Add(head);

        // Description
        if (!string.IsNullOrEmpty(quest.description))
        {
            var desc = new Label(quest.description);
            desc.AddToClassList("quest-card__desc");
            card.Add(desc);
        }

        // Reward
        var reward = new Label($"{quest.rewardPoints} награды");
        reward.AddToClassList("quest-card__reward");
        card.Add(reward);

        if (!isActive)
        {
            string capturedId = quest.id;
            boardUI.ClickRouter?.Add(card, () => OnQuestClicked(capturedId));
        }

        return card;
    }

    private void OnQuestClicked(string questId)
    {
        if (questService.TryAcceptQuest(questId))
        {
            AudioHooks.Sfx(AudioClipId.SfxHomeQuestAccept);
            // Re-render only — the accepted quest stays on the board greyed-out.
            // The board pool refreshes later, when returning home (see Start).
            GenerateBoard();

            if (hudDisplay != null)
                hudDisplay.Refresh();
        }
    }
}
