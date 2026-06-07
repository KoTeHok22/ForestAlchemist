using UnityEngine;
using UnityEngine.UI;

public sealed class GatherProgressDisplay : MonoBehaviour
{
    [SerializeField] private GameObject gatherPanel;
    [SerializeField] private Scrollbar progressBar;

    public void Configure(GameObject panel, Scrollbar scrollbar)
    {
        gatherPanel = panel;
        progressBar = scrollbar;
    }

    private void Awake()
    {
        ResolveReferences();
        Hide();
    }

    public void Show()
    {
        EnsureResolved();

        if (gatherPanel != null)
            gatherPanel.SetActive(true);

        SetProgress(0f);
    }

    public void Hide()
    {
        EnsureResolved();
        SetProgress(0f);

        if (gatherPanel != null)
            gatherPanel.SetActive(false);
    }

    public void SetProgress(float value)
    {
        EnsureResolved();

        if (progressBar != null)
            progressBar.size = Mathf.Clamp01(value);
    }

    private void EnsureResolved()
    {
        if (gatherPanel == null || progressBar == null)
        {
            ResolveReferences();
        }
    }

    private void ResolveReferences()
    {
        if (gatherPanel != null && progressBar != null)
        {
            return;
        }

        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null)
            {
                continue;
            }

            Transform panelTransform = canvas.transform.Find("Сбор");
            if (panelTransform == null)
            {
                continue;
            }

            gatherPanel = panelTransform.gameObject;

            Transform progressTransform = panelTransform.Find("ВремяСбора");
            if (progressTransform != null)
            {
                progressBar = progressTransform.GetComponent<Scrollbar>();
            }

            if (gatherPanel != null && progressBar != null)
            {
                return;
            }
        }
    }
}
