using UnityEngine;
using TMPro;
using UnityEngine.UI;

public sealed class ExpeditionStatsDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text statsText;
    [SerializeField] private TMP_Text visibilityText;

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
                visibilityText.text = $"Видимость: {vis.CurrentVisibility:F1} | Скорость: {speedMult:P0}";
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

        statsText.text = $"Успешных походов: {progress.stats.successfulExpeditions}\nСмертей: {progress.stats.totalDeaths}";
    }

    private void OnEnable()
    {
        if (ExpeditionManager.Instance != null)
            ExpeditionManager.Instance.OnExpeditionEnded += OnExpeditionEnded;
    }

    private void OnDisable()
    {
        if (ExpeditionManager.Instance != null)
            ExpeditionManager.Instance.OnExpeditionEnded -= OnExpeditionEnded;
    }

    private void OnExpeditionEnded(ExpeditionResult result)
    {
        RefreshStats();
    }
}
