using UnityEngine;
using TMPro;
using UnityEngine.UI;

public sealed class ExpeditionStatsDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text statsText;
    [SerializeField] private TMP_Text visibilityText;

    private bool runtimeUiBuilt;

    private void Awake()
    {
        BuildRuntimeUiIfNeeded();
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
        if (statsText == null) return;

        var progress = GameCore.Instance.CurrentProgress;
        if (progress == null)
        {
            statsText.text = string.Empty;
            return;
        }

        int threat = GameCore.Instance.CurrentProgress != null ? GameCore.Instance.CurrentProgress.orcs.threatLevel : 1;
        statsText.text = $"Успешных походов: {progress.stats.successfulExpeditions}\nСмертей: {progress.stats.totalDeaths}\nУгроза леса: {threat}";
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

    private void BuildRuntimeUiIfNeeded()
    {
        if (runtimeUiBuilt || (statsText != null && visibilityText != null))
        {
            runtimeUiBuilt = true;
            return;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            return;
        }

        GameObject root = new GameObject("RuntimeExpeditionStats", typeof(RectTransform), typeof(VerticalLayoutGroup));
        root.transform.SetParent(canvas.transform, false);
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(20f, -20f);
        rect.sizeDelta = new Vector2(320f, 140f);

        VerticalLayoutGroup layout = root.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 6f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;

        statsText = CreateText(root.transform, string.Empty, 20, FontStyles.Bold);
        visibilityText = CreateText(root.transform, string.Empty, 18, FontStyles.Normal);
        visibilityText.enableWordWrapping = true;
        runtimeUiBuilt = true;
    }

    private static TMP_Text CreateText(Transform parent, string content, float fontSize, FontStyles style)
    {
        GameObject textObject = new GameObject("Text", typeof(RectTransform));
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.TopLeft;
        return text;
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
