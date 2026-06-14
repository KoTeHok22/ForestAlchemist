using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// App UI expedition preparation board. Keeps the ExpeditionPreparationUI class
/// name + Open()/Close()/StartPreparedExpedition() so ExpeditionStarter continues
/// to find and drive it without changes. Renders on a runtime-created UIDocument
/// (doska bulletin board) instead of the legacy Canvas. The player picks which
/// consumables (and how many) to carry from the home storage into the expedition.
/// </summary>
public sealed class ExpeditionPreparationUI : MonoBehaviour
{
    private const string ViewPath = "Assets/UI/HomePanels/ExpeditionPrepView.uxml";
    private const string SettingsPath = "Assets/UI/HomePanels/ExpeditionPrepPanelSettings.asset";
    private const string ViewResourcePath = "UI/HomePanels/ExpeditionPrepView";
    private const string SettingsResourcePath = "UI/HomePanels/ExpeditionPrepPanelSettings";

    private static readonly string[] SupportedItems =
    {
        ItemCatalog.HealthPotion,
        ItemCatalog.ManaPotion,
        ItemCatalog.ShieldScroll,
        ItemCatalog.ReturnScroll,
        ItemCatalog.StaminaElixir,
        ItemCatalog.GreaterHealthPotion,
        ItemCatalog.GreaterManaPotion,
        ItemCatalog.EnhancedShieldScroll,
        ItemCatalog.EarthAmulet,
        ItemCatalog.LifebloomElixir,
        ItemCatalog.ShamanWard,
        ItemCatalog.WarchiefBrew,
        ItemCatalog.BloodcrownTonic
    };

    private UIDocument document;
    private VisualElement root;
    private VisualElement panelRoot;
    private VisualElement dimBg;
    private Label summary;
    private VisualElement rowsContainer;
    private AppUIClickRouter clickRouter;

    private readonly Dictionary<string, Label> countLabels = new Dictionary<string, Label>();
    private readonly Dictionary<string, Label> stockLabels = new Dictionary<string, Label>();
    private readonly Dictionary<string, VisualElement> minusButtons = new Dictionary<string, VisualElement>();
    private readonly Dictionary<string, VisualElement> plusButtons = new Dictionary<string, VisualElement>();

    private bool built;
    private bool isOpen;

    public bool IsOpen => isOpen;

    public void Open()
    {
        if (!EnsureBuilt())
        {
            Debug.LogError("[ExpeditionPreparationUI] Не удалось загрузить UI сборов (проверь Resources/UI/HomePanels/).");
            return;
        }

        if (dimBg != null) dimBg.style.display = DisplayStyle.Flex;
        if (panelRoot != null) panelRoot.pickingMode = PickingMode.Position;
        Refresh();
        if (!isOpen)
        {
            HomeUIBlocker.Acquire();
            isOpen = true;
            AudioHooks.PanelOpen();
        }
    }

    public void Close()
    {
        if (dimBg != null) dimBg.style.display = DisplayStyle.None;
        // Otherwise the Panel keeps eating all pointer events on top of HUD.
        if (panelRoot != null) panelRoot.pickingMode = PickingMode.Ignore;
        if (isOpen)
        {
            HomeUIBlocker.Release();
            isOpen = false;
            AudioHooks.PanelClose();
        }
    }

    public void StartPreparedExpedition()
    {
        Close();
        ExpeditionManager.Instance.StartExpedition();
    }

    private void OnDestroy()
    {
        if (isOpen)
        {
            HomeUIBlocker.Release();
            isOpen = false;
        }
    }

    private bool EnsureBuilt()
    {
        if (built && panelRoot != null && dimBg != null && rowsContainer != null)
        {
            return true;
        }

        document = GetComponent<UIDocument>();
        if (document == null)
        {
            document = gameObject.AddComponent<UIDocument>();
        }

        if (!HomePanelUiLoader.AssignAssets(document, ViewResourcePath, SettingsResourcePath, ViewPath, SettingsPath))
        {
            Debug.LogError("[ExpeditionPreparationUI] visualTreeAsset или panelSettings не найдены.");
            return false;
        }

        root = document.rootVisualElement;
        if (root == null)
        {
            Debug.LogError("[ExpeditionPreparationUI] rootVisualElement пуст.");
            return false;
        }

        panelRoot = root.Q<VisualElement>("panel-root");
        dimBg = root.Q<VisualElement>("dim-bg");
        summary = root.Q<Label>("summary");
        rowsContainer = root.Q<ScrollView>("rows");
        if (rowsContainer == null)
        {
            rowsContainer = root.Q<VisualElement>("rows");
        }

        if (panelRoot == null || dimBg == null || rowsContainer == null)
        {
            Debug.LogError("[ExpeditionPreparationUI] Разметка панели неполная (panel-root/dim-bg/rows).");
            return false;
        }

        clickRouter = new AppUIClickRouter(root);

        VisualElement closeX = root.Q<VisualElement>("btn-close-x");
        if (closeX != null) clickRouter.Add(closeX, Close);

        VisualElement btnAuto = root.Q<VisualElement>("btn-auto");
        if (btnAuto != null) clickRouter.Add(btnAuto, ApplyRecommendedLoadout);

        VisualElement btnClear = root.Q<VisualElement>("btn-clear");
        if (btnClear != null) clickRouter.Add(btnClear, ClearLoadout);

        VisualElement btnStart = root.Q<VisualElement>("btn-start");
        if (btnStart != null) clickRouter.Add(btnStart, StartPreparedExpedition);

        BuildRows();

        dimBg.style.display = DisplayStyle.None;
        panelRoot.pickingMode = PickingMode.Ignore;
        built = true;
        return true;
    }

    private void BuildRows()
    {
        if (rowsContainer == null) return;

        VisualElement rowsHost = rowsContainer is ScrollView scrollView
            ? scrollView.contentContainer
            : rowsContainer;

        rowsHost.Clear();
        clickRouter?.RemoveDead();
        countLabels.Clear();
        stockLabels.Clear();
        minusButtons.Clear();
        plusButtons.Clear();

        for (int i = 0; i < SupportedItems.Length; i++)
        {
            string itemId = SupportedItems[i];

            var row = new VisualElement();
            row.AddToClassList("prep-row");

            var icon = new VisualElement();
            icon.AddToClassList("prep-row__icon");
            Sprite sprite = Resources.Load<Sprite>($"Game/Icons/{itemId}");
            if (sprite != null) icon.style.backgroundImage = new StyleBackground(sprite);
            row.Add(icon);

            var info = new VisualElement();
            info.AddToClassList("prep-row__info");

            var name = new Label(ItemCatalog.GetDisplayName(itemId));
            name.AddToClassList("prep-row__name");
            info.Add(name);

            var stock = new Label(string.Empty);
            stock.AddToClassList("prep-row__stock");
            stockLabels[itemId] = stock;
            info.Add(stock);

            row.Add(info);

            var stepper = new VisualElement();
            stepper.AddToClassList("prep-row__stepper");

            VisualElement minus = MakeStep("−"); // minus sign
            minusButtons[itemId] = minus;
            clickRouter.Add(minus, () => ChangeCount(itemId, -1));
            stepper.Add(minus);

            var count = new Label("0");
            count.AddToClassList("prep-count");
            countLabels[itemId] = count;
            stepper.Add(count);

            VisualElement plus = MakeStep("+");
            plusButtons[itemId] = plus;
            clickRouter.Add(plus, () => ChangeCount(itemId, 1));
            stepper.Add(plus);

            row.Add(stepper);
            rowsHost.Add(row);
        }
    }

    private static VisualElement MakeStep(string glyph)
    {
        var btn = new VisualElement();
        btn.AddToClassList("prep-step");
        var label = new Label(glyph);
        label.AddToClassList("prep-step__label");
        btn.Add(label);
        return btn;
    }

    private void ChangeCount(string itemId, int delta)
    {
        GameProgressData progress = GameCore.Instance.CurrentProgress;
        PlayerInventory homeStorage = InventoryService.Instance.HomeStorage;
        EnsureLoadout(progress);
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

    private void ApplyRecommendedLoadout()
    {
        GameProgressData progress = GameCore.Instance.CurrentProgress;
        PlayerInventory homeStorage = InventoryService.Instance.HomeStorage;
        EnsureLoadout(progress);
        if (progress?.loadout?.consumables == null || homeStorage == null)
        {
            return;
        }

        SetCount(progress.loadout.consumables, homeStorage, ItemCatalog.HealthPotion, 2);
        SetCount(progress.loadout.consumables, homeStorage, ItemCatalog.ManaPotion, 2);
        SetCount(progress.loadout.consumables, homeStorage, ItemCatalog.ShieldScroll, 1);
        SetCount(progress.loadout.consumables, homeStorage, ItemCatalog.ReturnScroll, 1);
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

    private void Refresh()
    {
        if (root == null) return;

        GameProgressData progress = GameCore.Instance.CurrentProgress;
        PlayerInventory homeStorage = InventoryService.Instance.HomeStorage;
        EnsureLoadout(progress);
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

            if (countLabels.TryGetValue(itemId, out Label countLabel))
            {
                countLabel.text = slot.count.ToString();
            }

            if (stockLabels.TryGetValue(itemId, out Label stockLabel))
            {
                stockLabel.text = $"В доме: {stock}";
            }

            if (minusButtons.TryGetValue(itemId, out VisualElement minus))
            {
                minus.EnableInClassList("prep-step--disabled", slot.count <= 0);
            }

            if (plusButtons.TryGetValue(itemId, out VisualElement plus))
            {
                plus.EnableInClassList("prep-step--disabled", slot.count >= stock);
            }
        }

        if (summary != null)
        {
            summary.text = selectedItems > 0
                ? $"Выбрано предметов: {selectedItems}.\nВсё выбранное исчезнет при смерти в походе."
                : "Возьми расходники из домашнего хранилища.\nВсё выбранное исчезнет при смерти в походе.";
        }
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

    private static void EnsureLoadout(GameProgressData progress)
    {
        if (progress == null)
        {
            return;
        }

        if (progress.loadout == null)
        {
            progress.loadout = new ExpeditionLoadoutData();
        }

        if (progress.loadout.consumables == null)
        {
            progress.loadout.consumables = new List<InventorySlot>();
        }
    }

    private static void SetCount(List<InventorySlot> slots, PlayerInventory homeStorage, string itemId, int requestedCount)
    {
        InventorySlot slot = GetOrCreateLoadoutSlot(slots, itemId);
        int stock = homeStorage.GetItemCount(itemId);
        slot.count = Mathf.Clamp(requestedCount, 0, stock);
    }
}
