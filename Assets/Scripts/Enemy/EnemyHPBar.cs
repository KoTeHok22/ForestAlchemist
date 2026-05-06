using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class EnemyHPBar : MonoBehaviour
{
    [SerializeField] private float yOffset = 0.35f;
    [SerializeField] private float barWidth = 0.31f;
    [SerializeField] private float barHeight = 0.03f;

    private Canvas canvas;
    private Image backgroundImage;
    private Image fillImage;
    private RectTransform fillRect;
    private Camera cachedCamera;
    private bool isVisible;

    public void Initialize(float offset)
    {
        yOffset = offset;
        CreateBar();
        canvas.gameObject.SetActive(false);
        isVisible = false;
    }

    public void Show()
    {
        if (isVisible)
        {
            return;
        }

        isVisible = true;
        canvas.gameObject.SetActive(true);
    }

    public void UpdateHealth(float normalizedHealth)
    {
        if (fillRect != null)
        {
            fillRect.anchorMax = new Vector2(Mathf.Clamp01(normalizedHealth), 1f);
        }
    }

    public void Hide()
    {
        if (!isVisible)
        {
            return;
        }

        isVisible = false;
        if (canvas != null)
        {
            canvas.gameObject.SetActive(false);
        }
    }

    private void LateUpdate()
    {
        if (!isVisible || canvas == null)
        {
            return;
        }

        canvas.transform.position = transform.position + Vector3.up * yOffset;

        if (cachedCamera == null)
        {
            cachedCamera = Camera.main;
        }
    }

    private void CreateBar()
    {
        GameObject canvasGo = new GameObject("HPBarCanvas");
        canvasGo.transform.SetParent(transform, false);
        canvasGo.transform.localPosition = Vector3.up * yOffset;

        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 100f;

        RectTransform canvasRect = canvasGo.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(barWidth, barHeight);
        canvasRect.localScale = Vector3.one;

        GameObject bgGo = new GameObject("Background");
        bgGo.transform.SetParent(canvasGo.transform, false);
        backgroundImage = bgGo.AddComponent<Image>();
        backgroundImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        RectTransform bgRect = bgGo.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        GameObject fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(canvasGo.transform, false);
        fillImage = fillGo.AddComponent<Image>();
        fillImage.color = new Color(0.2f, 0.85f, 0.2f, 0.9f);
        fillRect = fillGo.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
    }
}
