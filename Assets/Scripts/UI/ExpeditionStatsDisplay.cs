using UnityEngine;
using TMPro;
using UnityEngine.UI;

public sealed class ExpeditionStatsDisplay : MonoBehaviour
{
    private const string ExpeditionCountTextName = "СколькоПоходовВЛесБылоСделано";

    [SerializeField] private TMP_Text statsText;
    [SerializeField] private TMP_Text visibilityText;
    [SerializeField] private TMP_Text expeditionCountText;

    private void Awake()
    {
        ResolveTextReferences();
    }

    private void Start()
    {
        RefreshStats();
    }

    private void Update()
    {
        if (ExpeditionManager.Instance.IsInExpedition && visibilityText != null)
        {
            VisibilitySystem vis = FindFirstObjectByType<VisibilitySystem>();
            if (vis != null)
            {
                float speedMult = vis.CurrentSpeedMultiplier;
                string returnInfo = ExpeditionManager.Instance.CanReturn()
                    ? $"Готов к выходу: {GetReturnMethodName(ExpeditionManager.Instance.ActiveReturnMethod)}"
                    : "Выход ещё не открыт";
                visibilityText.text = $"Видимость: {vis.CurrentVisibility:F1}\nШтраф скорости: {(1f - speedMult):P0}\n{returnInfo}";
            }
        }
    }

    private void RefreshStats()
    {
        ResolveTextReferences();

        if (statsText == null && expeditionCountText == null) return;

        var progress = GameCore.Instance.CurrentProgress;
        if (progress == null)
        {
            SetAttemptTexts(string.Empty);
            return;
        }

        int expeditionAttempts = progress.stats.successfulExpeditions + progress.stats.totalDeaths;
        SetAttemptTexts(expeditionAttempts.ToString());
    }

    private void OnEnable()
    {
        if (ExpeditionManager.Instance != null)
            ExpeditionManager.Instance.OnExpeditionEnded += OnExpeditionEnded;

        if (QuestManager.Instance != null)
            QuestManager.Instance.OnQuestRewardGranted += OnQuestRewardGranted;
    }

    private void OnDisable()
    {
        if (ExpeditionManager.Instance != null)
            ExpeditionManager.Instance.OnExpeditionEnded -= OnExpeditionEnded;

        if (QuestManager.Instance != null)
            QuestManager.Instance.OnQuestRewardGranted -= OnQuestRewardGranted;
    }

    private void OnExpeditionEnded(ExpeditionResult result)
    {
        RefreshStats();
    }

    private void OnQuestRewardGranted(int reward)
    {
        RefreshStats();
    }

    private void ResolveTextReferences()
    {
        if (statsText == null)
        {
            statsText = GetComponent<TMP_Text>();
        }

        if (expeditionCountText == null)
        {
            TMP_Text[] texts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && texts[i].name == ExpeditionCountTextName)
                {
                    expeditionCountText = texts[i];
                    break;
                }
            }
        }
    }

    private void SetAttemptTexts(string text)
    {
        if (statsText != null)
        {
            statsText.text = text;
        }

        if (expeditionCountText != null && expeditionCountText != statsText)
        {
            expeditionCountText.text = text;
        }
    }

    private static string GetReturnMethodName(string methodId)
    {
        return methodId switch
        {
            "portal" => "портал",
            ItemCatalog.ReturnScroll => "свиток возврата",
            "evacuation_point" => "точка эвакуации",
            _ => "неизвестно"
        };
    }
}
