using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class HomeStorageUI : MonoBehaviour
{
    private const string PanelName = "HomeStoragePanel";
    private static readonly string[] ConsumableIds = { ItemCatalog.HealthPotion, ItemCatalog.ManaPotion, ItemCatalog.ShieldScroll, ItemCatalog.ReturnScroll };

    private GameObject panelRoot;
    private Transform storageListRoot;
    private Transform loadoutListRoot;
    private Transform hotbarListRoot;
    private Transform actionListRoot;
    private TMP_Text summaryText;
    private bool built;

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

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }

    private void BuildIfNeeded()
    {
        if (built)
        {
            return;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("HomeStorageCanvas");
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
        rect.sizeDelta = new Vector2(980f, 620f);

        Image background = panelRoot.GetComponent<Image>();
        background.color = new Color(0.08f, 0.1f, 0.12f, 0.97f);

        VerticalLayoutGroup rootLayout = panelRoot.AddComponent<VerticalLayoutGroup>();
        rootLayout.padding = new RectOffset(18, 18, 18, 18);
        rootLayout.spacing = 12f;
        rootLayout.childControlHeight = false;
        rootLayout.childControlWidth = true;
        rootLayout.childForceExpandHeight = false;

        CreateHeader();
        CreateSummary();
        CreateBody();
        CreateFooter();

        panelRoot.SetActive(false);
        built = true;
    }

    private void CreateHeader()
    {
        TMP_Text title = CreateText(panelRoot.transform, "Домашний склад", 30, FontStyles.Bold);
        title.alignment = TextAlignmentOptions.Center;

        TMP_Text hint = CreateText(panelRoot.transform, "Здесь можно просмотреть запасы дома, настроить походные расходники и назначить предметы в хотбар.", 18, FontStyles.Normal);
        hint.alignment = TextAlignmentOptions.Center;
        hint.color = new Color(0.82f, 0.9f, 0.92f, 1f);
    }

    private void CreateSummary()
    {
        summaryText = CreateText(panelRoot.transform, string.Empty, 18, FontStyles.Normal);
        summaryText.alignment = TextAlignmentOptions.Left;
        summaryText.color = new Color(1f, 0.95f, 0.76f, 1f);
    }

    private void CreateBody()
    {
        GameObject body = new GameObject("Body", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        body.transform.SetParent(panelRoot.transform, false);

        HorizontalLayoutGroup bodyLayout = body.GetComponent<HorizontalLayoutGroup>();
        bodyLayout.spacing = 12f;
        bodyLayout.childControlWidth = true;
        bodyLayout.childControlHeight = true;
        bodyLayout.childForceExpandWidth = true;
        bodyLayout.childForceExpandHeight = true;

        LayoutElement bodySize = body.AddComponent<LayoutElement>();
        bodySize.preferredHeight = 470f;

        storageListRoot = CreateSection(body.transform, "Ресурсы дома", 260f);
        loadoutListRoot = CreateSection(body.transform, "Походный набор", 260f);
        hotbarListRoot = CreateSection(body.transform, "Настройка хотбара", 260f);
        actionListRoot = CreateSection(body.transform, "Быстрые действия", 180f);
    }

    private void CreateFooter()
    {
        GameObject footer = new GameObject("Footer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        footer.transform.SetParent(panelRoot.transform, false);

        HorizontalLayoutGroup layout = footer.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;

        LayoutElement footerSize = footer.AddComponent<LayoutElement>();
        footerSize.preferredHeight = 52f;

        Button closeButton = CreateButton(footer.transform, "Закрыть", Close);
        SetPreferredWidth(closeButton.gameObject, 180f);
    }

    private Transform CreateSection(Transform parent, string title, float width)
    {
        GameObject section = new GameObject(title, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        section.transform.SetParent(parent, false);

        Image image = section.GetComponent<Image>();
        image.color = new Color(0.14f, 0.17f, 0.19f, 0.95f);

        VerticalLayoutGroup layout = section.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;

        LayoutElement sectionSize = section.AddComponent<LayoutElement>();
        sectionSize.preferredWidth = width;

        TMP_Text titleText = CreateText(section.transform, title, 22, FontStyles.Bold);
        titleText.alignment = TextAlignmentOptions.Center;

        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
        content.transform.SetParent(section.transform, false);

        VerticalLayoutGroup contentLayout = content.GetComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 6f;
        contentLayout.childControlHeight = false;
        contentLayout.childControlWidth = true;
        contentLayout.childForceExpandHeight = false;

        LayoutElement contentSize = content.AddComponent<LayoutElement>();
        contentSize.preferredHeight = 400f;
        return content.transform;
    }

    private void Refresh()
    {
        if (!built)
        {
            return;
        }

        PlayerInventory homeStorage = InventoryService.Instance.HomeStorage;
        GameProgressData progress = GameCore.Instance.CurrentProgress;
        if (homeStorage == null || progress == null)
        {
            return;
        }

        ClearChildren(storageListRoot);
        ClearChildren(loadoutListRoot);
        ClearChildren(hotbarListRoot);
        ClearChildren(actionListRoot);

        PopulateStorage(homeStorage);
        PopulateLoadout(homeStorage, progress);
        PopulateHotbar(progress);
        PopulateActions();

        int storedItems = 0;
        List<InventorySlot> slots = homeStorage.GetAllSlots();
        for (int i = 0; i < slots.Count; i++)
        {
            storedItems += slots[i].count;
        }

        summaryText.text = $"Всего в доме предметов: {storedItems}. Используй походный набор для временных расходников и хотбар для боевой раскладки.";
    }

    private void PopulateStorage(PlayerInventory homeStorage)
    {
        List<InventorySlot> slots = homeStorage.GetAllSlots();
        if (slots.Count == 0)
        {
            TMP_Text empty = CreateText(storageListRoot, "Склад пуст.", 18, FontStyles.Italic);
            empty.alignment = TextAlignmentOptions.Center;
            return;
        }

        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlot slot = slots[i];
            if (slot == null || string.IsNullOrEmpty(slot.itemName) || slot.count <= 0)
            {
                continue;
            }

            TMP_Text entry = CreateText(storageListRoot, $"{GetDisplayName(slot.itemName)}: {slot.count}", 18, FontStyles.Normal);
            entry.alignment = TextAlignmentOptions.Left;
        }
    }

    private void PopulateLoadout(PlayerInventory homeStorage, GameProgressData progress)
    {
        for (int i = 0; i < ConsumableIds.Length; i++)
        {
            string itemId = ConsumableIds[i];
            InventorySlot loadoutSlot = GetOrCreateLoadoutSlot(progress, itemId);
            int stock = homeStorage.GetItemCount(itemId);
            if (loadoutSlot.count > stock)
            {
                loadoutSlot.count = stock;
            }

            GameObject row = CreateRow(loadoutListRoot);
            CreateText(row.transform, GetDisplayName(itemId), 18, FontStyles.Bold);
            CreateText(row.transform, $"В доме: {stock}", 16, FontStyles.Normal);

            Button minusButton = CreateButton(row.transform, "-", () => ChangeLoadout(itemId, -1));
            SetPreferredWidth(minusButton.gameObject, 42f);

            TMP_Text count = CreateText(row.transform, loadoutSlot.count.ToString(), 18, FontStyles.Bold);
            count.alignment = TextAlignmentOptions.Center;
            SetPreferredWidth(count.gameObject, 42f);

            Button plusButton = CreateButton(row.transform, "+", () => ChangeLoadout(itemId, 1));
            SetPreferredWidth(plusButton.gameObject, 42f);
        }
    }

    private void PopulateHotbar(GameProgressData progress)
    {
        List<string> options = GetHotbarOptions(progress);
        for (int i = 0; i < HotbarManager.SlotCount; i++)
        {
            int slotIndex = i;
            string currentItem = HotbarManager.Instance.GetSlotItem(slotIndex);

            GameObject row = CreateRow(hotbarListRoot);
            TMP_Text slotLabel = CreateText(row.transform, $"Слот {(slotIndex + 1) % 10}", 18, FontStyles.Bold);
            SetPreferredWidth(slotLabel.gameObject, 90f);

            TMP_Dropdown dropdown = CreateDropdown(row.transform, options, currentItem, value =>
            {
                string selected = value == 0 ? string.Empty : options[value - 1];
                HotbarManager.Instance.SetSlotItem(slotIndex, selected);
                Refresh();
            });
            SetPreferredWidth(dropdown.gameObject, 180f);

            TMP_Text current = CreateText(row.transform, string.IsNullOrEmpty(currentItem) ? "Пусто" : GetDisplayName(currentItem), 16, FontStyles.Normal);
            current.color = new Color(0.8f, 0.9f, 1f, 1f);
        }
    }

    private void PopulateActions()
    {
        CreateActionButton("Реком. набор", ApplyRecommendedLoadout);
        CreateActionButton("Очистить набор", ClearLoadout);
        CreateActionButton("Реком. хотбар", ApplyRecommendedHotbar);
        CreateActionButton("Очистить хотбар", ClearHotbar);
        CreateActionButton("В лес сейчас", StartExpeditionNow);
    }

    private void ChangeLoadout(string itemId, int delta)
    {
        GameProgressData progress = GameCore.Instance.CurrentProgress;
        PlayerInventory homeStorage = InventoryService.Instance.HomeStorage;
        if (progress == null || homeStorage == null)
        {
            return;
        }

        InventorySlot slot = GetOrCreateLoadoutSlot(progress, itemId);
        int max = homeStorage.GetItemCount(itemId);
        slot.count = Mathf.Clamp(slot.count + delta, 0, max);
        GameCore.Instance.SaveProgress();
        Refresh();
    }

    private void ApplyRecommendedLoadout()
    {
        GameProgressData progress = GameCore.Instance.CurrentProgress;
        PlayerInventory homeStorage = InventoryService.Instance.HomeStorage;
        if (progress == null || homeStorage == null)
        {
            return;
        }

        SetLoadoutCount(progress, homeStorage, ItemCatalog.HealthPotion, 2);
        SetLoadoutCount(progress, homeStorage, ItemCatalog.ManaPotion, 2);
        SetLoadoutCount(progress, homeStorage, ItemCatalog.ShieldScroll, 1);
        SetLoadoutCount(progress, homeStorage, ItemCatalog.ReturnScroll, 1);
        GameCore.Instance.SaveProgress();
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

        GameCore.Instance.SaveProgress();
        Refresh();
    }

    private void ApplyRecommendedHotbar()
    {
        HotbarManager.Instance.SetSlotItem(0, "spell_firebolt");
        HotbarManager.Instance.SetSlotItem(1, "spell_waterspring");
        HotbarManager.Instance.SetSlotItem(2, "spell_stoneskin");
        HotbarManager.Instance.SetSlotItem(3, "spell_airdash");
        HotbarManager.Instance.SetSlotItem(4, ItemCatalog.HealthPotion);
        HotbarManager.Instance.SetSlotItem(5, ItemCatalog.ManaPotion);
        HotbarManager.Instance.SetSlotItem(6, ItemCatalog.ShieldScroll);
        HotbarManager.Instance.SetSlotItem(7, ItemCatalog.ReturnScroll);
        HotbarManager.Instance.SetSlotItem(8, string.Empty);
        HotbarManager.Instance.SetSlotItem(9, string.Empty);
        Refresh();
    }

    private void ClearHotbar()
    {
        for (int i = 0; i < HotbarManager.SlotCount; i++)
        {
            HotbarManager.Instance.SetSlotItem(i, string.Empty);
        }

        Refresh();
    }

    private void StartExpeditionNow()
    {
        Close();
        ExpeditionManager.Instance.StartExpedition();
    }

    private static InventorySlot GetOrCreateLoadoutSlot(GameProgressData progress, string itemId)
    {
        if (progress.loadout == null)
        {
            progress.loadout = new ExpeditionLoadoutData();
        }

        if (progress.loadout.consumables == null)
        {
            progress.loadout.consumables = new List<InventorySlot>();
        }

        for (int i = 0; i < progress.loadout.consumables.Count; i++)
        {
            InventorySlot slot = progress.loadout.consumables[i];
            if (slot != null && slot.itemName == itemId)
            {
                return slot;
            }
        }

        InventorySlot created = new InventorySlot { itemName = itemId, count = 0 };
        progress.loadout.consumables.Add(created);
        return created;
    }

    private static void SetLoadoutCount(GameProgressData progress, PlayerInventory homeStorage, string itemId, int requestedCount)
    {
        InventorySlot slot = GetOrCreateLoadoutSlot(progress, itemId);
        int max = homeStorage.GetItemCount(itemId);
        slot.count = Mathf.Clamp(requestedCount, 0, max);
    }

    private static List<string> GetHotbarOptions(GameProgressData progress)
    {
        List<string> options = new List<string>();
        for (int i = 0; i < ConsumableIds.Length; i++)
        {
            options.Add(ConsumableIds[i]);
        }

        if (progress?.crafting?.craftedSpells != null)
        {
            for (int i = 0; i < progress.crafting.craftedSpells.Count; i++)
            {
                string spellId = progress.crafting.craftedSpells[i];
                if (!string.IsNullOrEmpty(spellId) && !options.Contains(spellId))
                {
                    options.Add(spellId);
                }
            }
        }

        string[] defaultSpells = { "spell_firebolt", "spell_waterspring", "spell_stoneskin", "spell_airdash" };
        for (int i = 0; i < defaultSpells.Length; i++)
        {
            if (!options.Contains(defaultSpells[i]))
            {
                options.Add(defaultSpells[i]);
            }
        }

        return options;
    }

    private static GameObject CreateRow(Transform parent)
    {
        GameObject row = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(parent, false);
        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 6f;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        LayoutElement size = row.AddComponent<LayoutElement>();
        size.preferredHeight = 36f;
        return row;
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
        buttonObject.GetComponent<Image>().color = new Color(0.24f, 0.33f, 0.37f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(callback);

        TMP_Text text = CreateText(buttonObject.transform, label, 16f, FontStyles.Bold);
        text.alignment = TextAlignmentOptions.Center;
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 34f;
        return button;
    }

    private static TMP_Dropdown CreateDropdown(Transform parent, List<string> options, string currentItem, UnityEngine.Events.UnityAction<int> onChanged)
    {
        GameObject dropdownObject = new GameObject("Dropdown", typeof(RectTransform), typeof(Image), typeof(TMP_Dropdown));
        dropdownObject.transform.SetParent(parent, false);
        dropdownObject.GetComponent<Image>().color = new Color(0.18f, 0.22f, 0.25f, 1f);

        TMP_Dropdown dropdown = dropdownObject.GetComponent<TMP_Dropdown>();
        dropdown.options = new List<TMP_Dropdown.OptionData> { new TMP_Dropdown.OptionData("Пусто") };
        for (int i = 0; i < options.Count; i++)
        {
            dropdown.options.Add(new TMP_Dropdown.OptionData(GetDisplayName(options[i])));
        }

        int index = 0;
        for (int i = 0; i < options.Count; i++)
        {
            if (options[i] == currentItem)
            {
                index = i + 1;
                break;
            }
        }

        TMP_Text caption = CreateText(dropdownObject.transform, dropdown.options[index].text, 16f, FontStyles.Normal);
        caption.name = "Label";
        caption.alignment = TextAlignmentOptions.MidlineLeft;
        RectTransform captionRect = caption.rectTransform;
        captionRect.anchorMin = Vector2.zero;
        captionRect.anchorMax = Vector2.one;
        captionRect.offsetMin = new Vector2(8f, 0f);
        captionRect.offsetMax = new Vector2(-8f, 0f);
        dropdown.captionText = caption;
        dropdown.value = index;
        dropdown.onValueChanged.AddListener(onChanged);

        LayoutElement layout = dropdownObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 34f;
        return dropdown;
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

    private static void ClearChildren(Transform root)
    {
        if (root == null)
        {
            return;
        }

        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Object.Destroy(root.GetChild(i).gameObject);
        }
    }

    private void CreateActionButton(string label, UnityEngine.Events.UnityAction callback)
    {
        Button button = CreateButton(actionListRoot, label, callback);
        SetPreferredWidth(button.gameObject, 150f);
    }

    private static string GetDisplayName(string itemId)
    {
        return ItemCatalog.GetDisplayName(itemId);
    }
}
