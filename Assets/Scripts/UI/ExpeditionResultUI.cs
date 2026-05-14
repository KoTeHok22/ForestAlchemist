using UnityEngine;
using TMPro;
using UnityEngine.UI;

public sealed class ExpeditionResultUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text statsText;
    [SerializeField] private Button closeButton;

    private bool runtimeUiBuilt;

    private void Awake()
    {
        BuildRuntimeUiIfNeeded();
        if (panel != null) panel.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(() => panel.SetActive(false));
    }

    private void Start()
    {
        if (ExpeditionManager.Instance != null)
        {
            ExpeditionManager.Instance.OnExpeditionEnded += ShowResult;
        }
    }

    private void OnDestroy()
    {
        if (ExpeditionManager.Instance != null)
        {
            ExpeditionManager.Instance.OnExpeditionEnded -= ShowResult;
        }
    }

    private void ShowResult(ExpeditionResult result)
    {
        BuildRuntimeUiIfNeeded();
        if (panel == null) return;
        panel.SetActive(true);

        resultText.text = result switch
        {
            ExpeditionResult.Success => "Экспедиция успешно завершена",
            ExpeditionResult.Death => "Экспедиция провалена: алхимик погиб",
            ExpeditionResult.Abandoned => "Экспедиция прервана",
            _ => "Экспедиция завершена"
        };
        resultText.color = result == ExpeditionResult.Success ? Color.green : new Color(1f, 0.45f, 0.45f, 1f);

        var stats = GameCore.Instance.CurrentProgress.stats;
        statsText.text = $"Успешных походов: {stats.successfulExpeditions}\nСмертей: {stats.totalDeaths}\nМаксимальная угроза: {stats.deepestThreatReached}\n{BuildMilestoneText(result, stats)}";
    }

    private static string BuildMilestoneText(ExpeditionResult result, ExpeditionStats stats)
    {
        if (result == ExpeditionResult.Success && stats.successfulExpeditions == 1)
        {
            return "Первый успешный поход завершён.";
        }

        if (result == ExpeditionResult.Success && stats.successfulExpeditions == 3)
        {
            return "Ты закрепился в лесу. Домашняя база начинает развиваться быстрее.";
        }

        if (result == ExpeditionResult.Death && stats.totalDeaths == 1)
        {
            return "Первая потеря напомнила: жадность в лесу опасна.";
        }

        if (stats.deepestThreatReached >= 5)
        {
            return "Лес ответил силой. Орки стали гораздо опаснее.";
        }

        return "Каждый поход меняет лес и укрепляет базу дома.";
    }

    private void BuildRuntimeUiIfNeeded()
    {
        if (runtimeUiBuilt || panel != null)
        {
            runtimeUiBuilt = true;
            return;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            return;
        }

        panel = new GameObject("ExpeditionResultPanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        panel.transform.SetParent(canvas.transform, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(520f, 280f);

        panel.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.96f);

        VerticalLayoutGroup layout = panel.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 18, 18);
        layout.spacing = 10f;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;

        resultText = CreateText(panel.transform, string.Empty, 28, FontStyles.Bold);
        resultText.alignment = TextAlignmentOptions.Center;

        statsText = CreateText(panel.transform, string.Empty, 20, FontStyles.Normal);
        statsText.alignment = TextAlignmentOptions.Center;
        statsText.enableWordWrapping = true;

        closeButton = CreateButton(panel.transform, "Продолжить", () => panel.SetActive(false));

        panel.SetActive(false);
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
        return text;
    }

    private static Button CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction callback)
    {
        GameObject buttonObject = new GameObject($"Button_{label}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.transform.SetParent(parent, false);
        buttonObject.GetComponent<Image>().color = new Color(0.24f, 0.28f, 0.34f, 1f);
        buttonObject.GetComponent<LayoutElement>().preferredHeight = 44f;
        buttonObject.GetComponent<LayoutElement>().preferredWidth = 180f;

        Button button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(callback);

        TMP_Text text = CreateText(buttonObject.transform, label, 18f, FontStyles.Bold);
        text.alignment = TextAlignmentOptions.Center;
        RectTransform rect = text.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return button;
    }
}
