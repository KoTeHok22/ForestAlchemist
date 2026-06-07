using UnityEngine;
using TMPro;
using UnityEngine.UI;

public sealed class ExpeditionResultUI : MonoBehaviour
{
    private const string DefaultPanelPath = "Canvas/ExpeditionResultPanel";

    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text statsText;
    [SerializeField] private Button closeButton;

    private void Awake()
    {
        ResolveReferences();

        if (panel != null)
            panel.SetActive(false);

        BindCloseButton();
    }

    private void Start()
    {
        if (ExpeditionManager.Instance != null)
        {
            ExpeditionManager.Instance.OnExpeditionEnded += ShowResult;
        }

        if (ExpeditionManager.Instance.TryConsumePendingResult(out ExpeditionResult pendingResult))
        {
            ShowResult(pendingResult);
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
        ResolveReferences();
        if (panel == null) return;

        panel.SetActive(true);

        ExpeditionStats stats = ExpeditionManager.Instance.GetLastResultStatsSnapshot();

        resultText.text = result switch
        {
            ExpeditionResult.Success => "Экспедиция успешно завершена",
            ExpeditionResult.Death => "Экспедиция провалена: алхимик погиб",
            ExpeditionResult.Abandoned => "Экспедиция прервана",
            _ => "Экспедиция завершена"
        };
        resultText.color = result == ExpeditionResult.Success ? Color.green : new Color(1f, 0.45f, 0.45f, 1f);

        statsText.text = $"Успешных походов: {stats.successfulExpeditions}\nСмертей: {stats.totalDeaths}\nМаксимальная угроза: {stats.deepestThreatReached}\n{BuildMilestoneText(result, stats)}";

        if (closeButton != null)
        {
            closeButton.transform.SetAsLastSibling();
        }
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

    private void ResolveReferences()
    {
        if (panel == null)
        {
            GameObject defaultPanel = GameObject.Find(DefaultPanelPath);
            if (defaultPanel != null)
            {
                panel = defaultPanel;
            }
        }

        if (panel == null)
        {
            return;
        }

        if (resultText == null)
            resultText = panel.transform.Find("ResultText")?.GetComponent<TMP_Text>();

        if (statsText == null)
            statsText = panel.transform.Find("StatsText")?.GetComponent<TMP_Text>();

        if (closeButton == null)
            closeButton = panel.transform.Find("CloseButton")?.GetComponent<Button>();
    }

    private void BindCloseButton()
    {
        if (closeButton == null)
        {
            return;
        }

        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(ClosePanel);
    }

    private void ClosePanel()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }
}
