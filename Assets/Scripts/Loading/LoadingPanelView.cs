using UnityEngine;
using UnityEngine.UI;

public sealed class LoadingPanelView : MonoBehaviour, ILoadingView
{
    [SerializeField] private GameObject loadPanel;
    [SerializeField] private Scrollbar progressBar;

    public void Show()
    {
        loadPanel.SetActive(true);
        SetProgress(0f);
    }

    public void Hide()
    {
        loadPanel.SetActive(false);
    }

    public void SetProgress(float progress)
    {
        progressBar.size = Mathf.Clamp01(progress);
    }
}
