using UnityEngine;
using UnityEngine.UI;

public sealed class GatherProgressDisplay : MonoBehaviour
{
    [SerializeField] private Scrollbar progressBar;

    private void Awake()
    {
        BuildRuntimeUiIfNeeded();
        Hide();
    }

    public void Show()
    {
        if (progressBar != null)
            progressBar.gameObject.SetActive(true);
        SetProgress(0f);
    }

    public void Hide()
    {
        if (progressBar != null)
            progressBar.gameObject.SetActive(false);
    }

    public void SetProgress(float value)
    {
        if (progressBar != null)
            progressBar.size = Mathf.Clamp01(value);
    }

    private void BuildRuntimeUiIfNeeded()
    {
        if (progressBar != null)
        {
            return;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            return;
        }

        GameObject root = new GameObject("GatherProgress", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        root.transform.SetParent(canvas.transform, false);

        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 110f);
        rect.sizeDelta = new Vector2(220f, 22f);

        Image background = root.GetComponent<Image>();
        background.color = new Color(0.12f, 0.14f, 0.16f, 0.95f);

        GameObject slidingArea = new GameObject("Sliding Area", typeof(RectTransform));
        slidingArea.transform.SetParent(root.transform, false);
        RectTransform slidingRect = slidingArea.GetComponent<RectTransform>();
        slidingRect.anchorMin = Vector2.zero;
        slidingRect.anchorMax = Vector2.one;
        slidingRect.offsetMin = new Vector2(4f, 4f);
        slidingRect.offsetMax = new Vector2(-4f, -4f);

        GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(slidingArea.transform, false);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.anchorMin = Vector2.zero;
        handleRect.anchorMax = new Vector2(0f, 1f);
        handleRect.sizeDelta = new Vector2(0f, 0f);

        Image handleImage = handle.GetComponent<Image>();
        handleImage.color = new Color(0.45f, 0.85f, 0.55f, 1f);

        progressBar = root.GetComponent<Scrollbar>();
        progressBar.direction = Scrollbar.Direction.LeftToRight;
        progressBar.size = 0f;
        progressBar.handleRect = handleRect;
        progressBar.targetGraphic = handleImage;
        progressBar.value = 0f;
    }
}
