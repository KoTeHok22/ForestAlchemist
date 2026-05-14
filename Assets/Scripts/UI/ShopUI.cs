using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public sealed class ShopUI : MonoBehaviour
{
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private Transform itemsContainer;
    [SerializeField] private GameObject itemButtonTemplate;
    [SerializeField] private TMP_Text bloodCountText;
    [SerializeField] private Button closeButton;

    private bool runtimeUiBuilt;

    private void Awake()
    {
        BuildRuntimeUiIfNeeded();
        if (shopPanel != null) shopPanel.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (itemButtonTemplate != null) itemButtonTemplate.SetActive(false);

        ShopService.Instance.OnItemPurchased += Refresh;
    }

    private void OnDestroy()
    {
        if (ShopService.Instance != null)
            ShopService.Instance.OnItemPurchased -= Refresh;
    }

    public void Open()
    {
        BuildRuntimeUiIfNeeded();
        if (itemsContainer == null || itemButtonTemplate == null)
        {
            return;
        }

        if (shopPanel != null) shopPanel.SetActive(true);
        Refresh(null);
    }

    public void Close()
    {
        if (shopPanel != null) shopPanel.SetActive(false);
    }

    private void Refresh(string _)
    {
        UpdateBloodCount();
        PopulateItems();
    }

    private void UpdateBloodCount()
    {
        if (bloodCountText == null) return;
        var homeStorage = InventoryService.Instance.HomeStorage;
        int blood = homeStorage != null ? homeStorage.GetItemCount(ItemCatalog.OrcBlood) : 0;
        bloodCountText.text = $"{ItemCatalog.GetDisplayName(ItemCatalog.OrcBlood)}: {blood}";
    }

    private void PopulateItems()
    {
        ClearContainer();

        List<ShopService.ShopItem> items = ShopService.Instance.GetAvailableItems();
        for (int i = 0; i < items.Count; i++)
        {
            ShopService.ShopItem shopItem = items[i];
            GameObject btn = Instantiate(itemButtonTemplate, itemsContainer);
            btn.SetActive(true);
            btn.name = $"ShopItem_{shopItem.itemId}";

            TMP_Text label = btn.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = $"{shopItem.displayName} ({shopItem.priceInBlood} крови)";

            Button button = btn.GetComponent<Button>();
            if (button != null)
            {
                string capturedId = shopItem.itemId;
                button.onClick.AddListener(() => OnPurchaseClicked(capturedId));
            }
        }
    }

    private void OnPurchaseClicked(string itemId)
    {
        ShopService.Instance.TryPurchase(itemId);
    }

    private void ClearContainer()
    {
        if (itemsContainer == null) return;
        for (int i = itemsContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = itemsContainer.GetChild(i);
            if (child.gameObject == itemButtonTemplate) continue;
            Destroy(child.gameObject);
        }
    }

    private void BuildRuntimeUiIfNeeded()
    {
        if (runtimeUiBuilt || shopPanel != null)
        {
            runtimeUiBuilt = true;
            return;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("ShopCanvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        shopPanel = new GameObject("ShopPanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        shopPanel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = shopPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(560f, 520f);

        Image panelImage = shopPanel.GetComponent<Image>();
        panelImage.color = new Color(0.1f, 0.08f, 0.06f, 0.96f);

        VerticalLayoutGroup layout = shopPanel.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 18, 18);
        layout.spacing = 10f;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;

        TMP_Text title = CreateText(shopPanel.transform, "Магазин", 30, FontStyles.Bold);
        title.alignment = TextAlignmentOptions.Center;

        bloodCountText = CreateText(shopPanel.transform, string.Empty, 20, FontStyles.Bold);
        bloodCountText.color = new Color(1f, 0.85f, 0.85f, 1f);
        bloodCountText.alignment = TextAlignmentOptions.Center;

        GameObject listRoot = new GameObject("ItemsContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
        listRoot.transform.SetParent(shopPanel.transform, false);
        itemsContainer = listRoot.transform;

        VerticalLayoutGroup listLayout = listRoot.GetComponent<VerticalLayoutGroup>();
        listLayout.spacing = 8f;
        listLayout.childControlHeight = false;
        listLayout.childControlWidth = true;
        listLayout.childForceExpandHeight = false;

        LayoutElement listSize = listRoot.AddComponent<LayoutElement>();
        listSize.preferredHeight = 360f;

        itemButtonTemplate = CreateButtonTemplate(itemsContainer, "ShopItemTemplate");
        itemButtonTemplate.SetActive(false);

        closeButton = CreateButton(shopPanel.transform, "Закрыть", Close);
        SetPreferredWidth(closeButton.gameObject, 180f);

        shopPanel.SetActive(false);
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
        text.enableWordWrapping = true;
        return text;
    }

    private static GameObject CreateButtonTemplate(Transform parent, string name)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.transform.SetParent(parent, false);
        buttonObject.GetComponent<Image>().color = new Color(0.22f, 0.18f, 0.12f, 1f);
        buttonObject.GetComponent<LayoutElement>().preferredHeight = 48f;

        TMP_Text label = CreateText(buttonObject.transform, string.Empty, 18f, FontStyles.Normal);
        label.alignment = TextAlignmentOptions.Center;
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(10f, 0f);
        labelRect.offsetMax = new Vector2(-10f, 0f);

        return buttonObject;
    }

    private static Button CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction callback)
    {
        GameObject buttonObject = CreateButtonTemplate(parent, $"Button_{label}");
        buttonObject.SetActive(true);
        Button button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(callback);
        TMP_Text text = buttonObject.GetComponentInChildren<TMP_Text>();
        if (text != null)
        {
            text.text = label;
        }

        return button;
    }

    private static void SetPreferredWidth(GameObject gameObject, float width)
    {
        LayoutElement layout = gameObject.GetComponent<LayoutElement>();
        if (layout == null)
        {
            layout = gameObject.AddComponent<LayoutElement>();
        }

        layout.preferredWidth = width;
    }
}
