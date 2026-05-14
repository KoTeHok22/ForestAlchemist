using UnityEngine;
using TMPro;
using UnityEngine.UI;

public sealed class ExpeditionResultUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text statsText;
    [SerializeField] private Button closeButton;

    private void Awake()
    {
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
        if (panel == null) return;
        panel.SetActive(true);

        resultText.text = result == ExpeditionResult.Success ? "Экспедиция Успешна!" : "Вы Погибли...";
        resultText.color = result == ExpeditionResult.Success ? Color.green : Color.red;

        var stats = GameCore.Instance.CurrentProgress.stats;
        statsText.text = $"Успешных походов: {stats.successfulExpeditions}\nСмертей: {stats.totalDeaths}";
    }
}
