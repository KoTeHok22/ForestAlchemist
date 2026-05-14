using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ExpeditionPreparationUI : MonoBehaviour
{
    private const string PanelName = "ExpeditionPreparationPanel";
    private static readonly string[] SupportedItems =
    {
        ItemCatalog.HealthPotion,
        ItemCatalog.ManaPotion,
        ItemCatalog.ShieldScroll,
        ItemCatalog.ReturnScroll
    };

    private GameObject panelRoot;
    private TMP_Text summaryText;
    private readonly Dictionary<string, TMP_Text> countTexts = new Dictionary<string, TMP_Text>();
    private readonly Dictionary<string, TMP_Text> stockTexts = new Dictionary<string, TMP_Text>();
    private bool uiBuilt;

    public void Open()
    {
        BuildIfNeeded();
        Refresh();
        panelRoot.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Close()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        Time.timeScale = 1f;
    }

    public void StartPreparedExpedition()
    {
        Close();
        ExpeditionManager.Instance.StartExpedition();
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }

    private void BuildIfNeeded()
    {
        if (uiBuilt)
        {
            return;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("PreparationCanvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        panelRoot = new GameObject(PanelName, typeof(RectTransform), typeof(Image));
        panelRoot.transform.SetParent(canvas.transform, false);

        RectTransform rect = panelRoot.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(620f, 420f);

        Image background = panelRoot.GetComponent<Image>();
        background.color = new Color(0.08f, 0.12f, 0.1f, 0.95f);

        VerticalLayoutGroup layout = panelRoot.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 20, 20);
        layout.spacing = 10f;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;

        CreateHeader(panelRoot.transform);
        CreateSummary(panelRoot.transform);
        CreateItemRows(panelRoot.transform);
        CreateFooter(panelRoot.transform);

        panelRoot.SetActive(false);
        uiBuilt = true;
    }

    private void CreateHeader(Transform parent)
    {
        TMP_Text title = CreateText(parent, "Подготовка к экспедиции", 30, FontStyles.Bold);
        title.alignment = TextAlignmentOptions.Center;

        TMP_Text hint = CreateText(parent, "Выбери расходники, которые возьмёшь из домашнего хранилища.", 18, FontStyles.Normal);
        hint.alignment = TextAlignmentOptions.Center;
        hint.color = new Color(0.85f, 0.92f, 0.88f, 1f);
    }

    private void CreateSummary(Transform parent)
    {
        summaryText = CreateText(parent, string.Empty, 18, FontStyles.Normal);
        summaryText.alignment = TextAlignmentOptions.Left;
        summaryText.color = new Color(1f, 0.95f, 0.75f, 1f);
    }

    private void CreateItemRows(Transform parent)
    {
        for (int i = 0; i < SupportedItems.Length; i++)
        {
            string itemId = SupportedItems[i];

            GameObject row = new GameObject($"Row_{itemId}", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(parent, false);

            HorizontalLayoutGroup rowLayout = row.GetComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 8f;
            rowLayout.childControlWidth = false;
            rowLayout.childControlHeight = false;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;

            LayoutElement rowSize = row.AddComponent<LayoutElement>();
            rowSize.preferredHeight = 42f;

            TMP_Text nameText = CreateText(row.transform, GetDisplayName(itemId), 18, FontStyles.Bold);
            nameText.alignment = TextAlignmentOptions.Left;
            SetPreferredWidth(nameText.gameObject, 220f);

            TMP_Text stockText = CreateText(row.transform, string.Empty, 16, FontStyles.Normal);
            stockText.alignment = TextAlignmentOptions.Left;
            SetPreferredWidth(stockText.gameObject, 120f);
            stockTexts[itemId] = stockText;

            Button minusButton = CreateButton(row.transform, "-", () => ChangeCount(itemId, -1));
            SetPreferredWidth(minusButton.gameObject, 48f);

            TMP_Text countText = CreateText(row.transform, "0", 18, FontStyles.Bold);
            countText.alignment = TextAlignmentOptions.Center;
            SetPreferredWidth(countText.gameObject, 48f);
            countTexts[itemId] = countText;

            Button plusButton = CreateButton(row.transform, "+", () => ChangeCount(itemId, 1));
            SetPreferredWidth(plusButton.gameObject, 48f);
        }
    }

    private void CreateFooter(Transform parent)
    {
        GameObject row = new GameObject("Footer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(parent, false);

        HorizontalLayoutGroup rowLayout = row.GetComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 12f;
        rowLayout.childAlignment = TextAnchor.MiddleCenter;
        rowLayout.childControlHeight = false;
        rowLayout.childControlWidth = false;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;

        LayoutElement rowSize = row.AddComponent<LayoutElement>();
        rowSize.preferredHeight = 56f;

        Button quickFillButton = CreateButton(row.transform, "Авто", ApplyRecommendedLoadout);
        SetPreferredWidth(quickFillButton.gameObject, 120f);

        Button clearButton = CreateButton(row.transform, "Очистить", ClearLoadout);
        SetPreferredWidth(clearButton.gameObject, 140f);

        Button closeButton = CreateButton(row.transform, "Отмена", Close);
        SetPreferredWidth(closeButton.gameObject, 160f);

        Button startButton = CreateButton(row.transform, "В лес", StartPreparedExpedition);
        SetPreferredWidth(startButton.gameObject, 200f);
    }

    private void ChangeCount(string itemId, int delta)
    {
        GameProgressData progress = GameCore.Instance.CurrentProgress;
        PlayerInventory homeStorage = InventoryService.Instance.HomeStorage;
        if (progress?.loadout?.consumables == null || homeStorage == null)
        {
            return;
        }

        InventorySlot slot = GetOrCreateLoadoutSlot(progress.loadout.consumables, itemId);
        int maxAvailable = homeStorage.GetItemCount(itemId);
        slot.count = Mathf.Clamp(slot.count + delta, 0, maxAvailable);
        GameCore.Instance.SaveProgress();
        Refresh();
    }

    private void Refresh()
    {
        GameProgressData progress = GameCore.Instance.CurrentProgress;
        PlayerInventory homeStorage = InventoryService.Instance.HomeStorage;
        if (progress?.loadout?.consumables == null || homeStorage == null)
        {
            return;
        }

        int selectedItems = 0;
        for (int i = 0; i < SupportedItems.Length; i++)
        {
            string itemId = SupportedItems[i];
            InventorySlot slot = GetOrCreateLoadoutSlot(progress.loadout.consumables, itemId);
            int stock = homeStorage.GetItemCount(itemId);

            if (slot.count > stock)
            {
                slot.count = stock;
            }

            selectedItems += slot.count;

            if (countTexts.TryGetValue(itemId, out TMP_Text countText))
            {
                countText.text = slot.count.ToString();
            }

            if (stockTexts.TryGetValue(itemId, out TMP_Text stockText))
            {
                stockText.text = $"В доме: {stock}";
            }
        }

        if (summaryText != null)
        {
            summaryText.text = $"Выбрано предметов: {selectedItems}. Всё выбранное исчезнет при смерти в походе.";
        }

        GameCore.Instance.SaveProgress();
    }

    private void ApplyRecommendedLoadout()
    {
        GameProgressData progress = GameCore.Instance.CurrentProgress;
        PlayerInventory homeStorage = InventoryService.Instance.HomeStorage;
        if (progress?.loadout?.consumables == null || homeStorage == null)
        {
            return;
        }

        SetCount(progress.loadout.consumables, homeStorage, ItemCatalog.HealthPotion, 2);
        SetCount(progress.loadout.consumables, homeStorage, ItemCatalog.ManaPotion, 2);
        SetCount(progress.loadout.consumables, homeStorage, ItemCatalog.ShieldScroll, 1);
        SetCount(progress.loadout.consumables, homeStorage, ItemCatalog.ReturnScroll, 1);
        Refresh();
    }

    private void ClearLoadout()
    {
        GameProgressData progress = GameCore.Instance.CurrentProgress;
        if (progress?.loadout?.consumables == null)
        {
            return;
        }

        for (int i = 0; i < progress.loadout.consumables.Count; i++)
        {
            if (progress.loadout.consumables[i] != null)
            {
                progress.loadout.consumables[i].count = 0;
            }
        }

        Refresh();
    }

    private static InventorySlot GetOrCreateLoadoutSlot(List<InventorySlot> slots, string itemId)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null && slots[i].itemName == itemId)
            {
                return slots[i];
            }
        }

        InventorySlot slot = new InventorySlot { itemName = itemId, count = 0 };
        slots.Add(slot);
        return slot;
    }

    private static void SetCount(List<InventorySlot> slots, PlayerInventory homeStorage, string itemId, int requestedCount)
    {
        InventorySlot slot = GetOrCreateLoadoutSlot(slots, itemId);
        int stock = homeStorage.GetItemCount(itemId);
        slot.count = Mathf.Clamp(requestedCount, 0, stock);
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
        text.enableWordWrapping = false;
        return text;
    }

    private static Button CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction callback)
    {
        GameObject buttonObject = new GameObject($"Button_{label}", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.22f, 0.34f, 0.28f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(callback);

        TMP_Text text = CreateText(buttonObject.transform, label, 18f, FontStyles.Bold);
        text.alignment = TextAlignmentOptions.Center;
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 44f;
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

    private static string GetDisplayName(string itemId)
    {
        return ItemCatalog.GetDisplayName(itemId);
    }
}
