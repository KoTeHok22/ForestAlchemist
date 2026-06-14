using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// App UI expedition pack (Level scene). Toggle with I. Replaces the legacy
/// Canvas InventoryDisplay on Level while keeping the same service wiring via
/// LevelManager.Initialize.
/// </summary>
public sealed class ExpeditionInventoryUI : MonoBehaviour
{
    private const string ViewPath = "Assets/UI/LevelPanels/ExpeditionInventoryView.uxml";
    private const string SettingsPath = "Assets/UI/LevelPanels/ExpeditionInventoryPanelSettings.asset";
    private const string ViewResourcePath = "UI/LevelPanels/ExpeditionInventoryView";
    private const string SettingsResourcePath = "UI/LevelPanels/ExpeditionInventoryPanelSettings";
    private const string ChildName = "ExpeditionInventoryOverlay_AppUI";

    private static readonly (string title, string[] ids)[] Categories =
    {
        ("Ресурсы и трофеи", new[]
        {
            ItemCatalog.OrcBlood, ItemCatalog.RareFlower, ItemCatalog.GreenOrcDrop,
            ItemCatalog.ShamanTalisman, ItemCatalog.WarchiefTrophy,
            ItemCatalog.SakuraSapling, ItemCatalog.OakSapling, ItemCatalog.AppleSapling
        }),
        ("Зелья и свитки", new[]
        {
            ItemCatalog.HealthPotion, ItemCatalog.ManaPotion, ItemCatalog.ShieldScroll, ItemCatalog.ReturnScroll,
            ItemCatalog.GreaterHealthPotion, ItemCatalog.GreaterManaPotion, ItemCatalog.EnhancedShieldScroll,
            ItemCatalog.StaminaElixir, ItemCatalog.WarchiefBrew, ItemCatalog.EarthAmulet,
            ItemCatalog.LifebloomElixir, ItemCatalog.ShamanWard, ItemCatalog.BloodcrownTonic
        }),
    };

    private static readonly Dictionary<string, string> ItemSpritePaths = new Dictionary<string, string>
    {
        { ItemCatalog.OrcBlood,      "Game/Items/5.1_krov_orka" },
        { ItemCatalog.SakuraSapling, "Game/Items/2.1_sazhenec_sakury" },
        { ItemCatalog.OakSapling,    "Game/Items/2.2_sazhenec_duba" },
        { ItemCatalog.AppleSapling,  "Game/Items/2.3_sazhenec_yabloni" }
    };

    private UIDocument document;
    private VisualElement root;
    private VisualElement panelRoot;
    private VisualElement dimBg;
    private ScrollView itemsList;
    private VisualElement emptyState;
    private Label summary;
    private AppUIClickRouter clickRouter;

    private PlayerInventory inventory;
    private IQuestItemIconProvider iconProvider;
    private bool built;
    private bool isOpen;

    public bool IsOpen => isOpen;

    private void OnDestroy()
    {
        UnsubscribeInventory();
    }

    public void Initialize(PlayerInventory pack, IQuestItemIconProvider provider)
    {
        UnsubscribeInventory();
        inventory = pack;
        iconProvider = provider;
        if (inventory != null)
        {
            inventory.OnInventoryChanged += HandleInventoryChanged;
        }

        PlayerInventory managerPack = ExpeditionManager.Instance?.ExpeditionInventory;
        ExpeditionItemTrace.Log(
            "InventoryUI.Initialize",
            $"ui={GetHashCode()} bound={DescribeRef(inventory)} manager={DescribeRef(managerPack)} match={(inventory == managerPack)} subscribed={(inventory != null)}");

        if (isOpen)
        {
            Refresh();
        }
    }

    private static string DescribeRef(PlayerInventory pack)
    {
        return pack == null ? "NULL" : $"{pack.DebugLabel}#{pack.GetHashCode()} [{ExpeditionItemTrace.DescribeSlots(pack)}]";
    }

    public void Open()
    {
        if (!EnsureBuilt())
        {
            Debug.LogError("[ExpeditionInventoryUI] Не удалось открыть рюкзак: UI не собран.");
            return;
        }

        EnsureInventoryBound();
        if (dimBg != null) dimBg.style.display = DisplayStyle.Flex;
        if (panelRoot != null) panelRoot.pickingMode = PickingMode.Position;
        ExpeditionItemTrace.Log("InventoryUI.Open", $"ui={GetHashCode()} bound={DescribeRef(inventory)}");
        Refresh();
        if (!isOpen)
        {
            AudioHooks.PanelOpen();
        }

        isOpen = true;
    }

    public void Close()
    {
        if (dimBg != null) dimBg.style.display = DisplayStyle.None;
        if (panelRoot != null) panelRoot.pickingMode = PickingMode.Ignore;
        if (isOpen)
        {
            AudioHooks.PanelClose();
        }

        isOpen = false;
    }

    public void Toggle()
    {
        if (isOpen) Close();
        else Open();
    }

    private void UnsubscribeInventory()
    {
        if (inventory != null)
        {
            inventory.OnInventoryChanged -= HandleInventoryChanged;
        }
    }

    private void HandleInventoryChanged()
    {
        EnsureBuilt();
        ExpeditionItemTrace.Log(
            "InventoryUI.Event",
            $"OnInventoryChanged ui={GetHashCode()} bound={DescribeRef(inventory)} open={isOpen}");
        Refresh();
    }

    private bool EnsureBuilt()
    {
        if (built && panelRoot != null && dimBg != null)
        {
            return true;
        }

        Transform existing = transform.Find(ChildName);
        GameObject host = existing != null ? existing.gameObject : new GameObject(ChildName);
        if (existing == null) host.transform.SetParent(null, false);

        document = host.GetComponent<UIDocument>();
        if (document == null)
        {
            document = host.AddComponent<UIDocument>();
        }

        if (!HomePanelUiLoader.AssignAssets(document, ViewResourcePath, SettingsResourcePath, ViewPath, SettingsPath))
        {
            Debug.LogError("[ExpeditionInventoryUI] visualTreeAsset или panelSettings не найдены.");
            return false;
        }

        if (!HomePanelUiLoader.TryResolveShell(document, out root, out panelRoot, out dimBg))
        {
            Debug.LogError("[ExpeditionInventoryUI] Разметка панели неполная (panel-root/dim-bg).");
            return false;
        }

        itemsList = root.Q<ScrollView>("items-list");
        emptyState = root.Q<VisualElement>("empty-state");
        summary = root.Q<Label>("summary");

        if (itemsList != null)
        {
            itemsList.contentContainer.style.width = Length.Percent(100);
            itemsList.contentContainer.style.alignItems = Align.Stretch;
        }

        clickRouter = new AppUIClickRouter(root);
        VisualElement closeX = root.Q<VisualElement>("btn-close-x");
        if (closeX != null) clickRouter.Add(closeX, Close);

        dimBg.style.display = DisplayStyle.None;
        panelRoot.pickingMode = PickingMode.Ignore;
        built = true;
        return true;
    }

    private void EnsureInventoryBound()
    {
        PlayerInventory pack = ExpeditionManager.Instance?.ExpeditionInventory;
        if (pack == null)
        {
            ExpeditionItemTrace.Log("InventoryUI.Bind", $"ui={GetHashCode()} manager inventory=NULL");
            return;
        }

        if (pack != inventory)
        {
            ExpeditionItemTrace.Log(
                "InventoryUI.Bind",
                $"ui={GetHashCode()} rebinding {DescribeRef(inventory)} -> {DescribeRef(pack)}");
            Initialize(pack, iconProvider);
        }
    }

    private void Refresh()
    {
        EnsureInventoryBound();
        if (itemsList == null || inventory == null)
        {
            ExpeditionItemTrace.Log(
                "InventoryUI.Refresh",
                $"ui={GetHashCode()} skipped itemsList={(itemsList != null)} inventory={(inventory != null)}");
            return;
        }

        itemsList.contentContainer.Clear();
        clickRouter?.RemoveDead();

        Dictionary<string, int> counts = new Dictionary<string, int>();
        int total = 0;
        foreach (InventorySlot slot in inventory.GetAllSlots())
        {
            if (slot == null || string.IsNullOrEmpty(slot.itemName) || slot.count <= 0) continue;
            string id = ItemCatalog.Normalize(slot.itemName);
            counts.TryGetValue(id, out int existing);
            counts[id] = existing + slot.count;
            total += slot.count;
        }

        ExpeditionItemTrace.Log(
            "InventoryUI.Refresh",
            $"ui={GetHashCode()} bound={DescribeRef(inventory)} open={isOpen} total={total} unique={counts.Count} raw=[{ExpeditionItemTrace.DescribeSlots(inventory)}]");

        if (summary != null)
        {
            summary.text = total > 0
                ? $"Предметов в рюкзаке: {total}"
                : string.Empty;
        }

        bool hasItems = counts.Count > 0;
        if (emptyState != null)
        {
            emptyState.style.display = hasItems ? DisplayStyle.None : DisplayStyle.Flex;
        }

        itemsList.style.display = hasItems ? DisplayStyle.Flex : DisplayStyle.None;
        if (!hasItems)
        {
            return;
        }

        HashSet<string> shown = new HashSet<string>();
        foreach ((string title, string[] ids) category in Categories)
        {
            List<string> present = new List<string>();
            foreach (string id in category.ids)
            {
                string normalized = ItemCatalog.Normalize(id);
                if (counts.ContainsKey(normalized) && shown.Add(normalized))
                {
                    present.Add(normalized);
                }
            }

            if (present.Count > 0)
            {
                AddSection(category.title, present, counts);
            }
        }

        List<string> rest = new List<string>();
        foreach (KeyValuePair<string, int> kv in counts)
        {
            if (!shown.Contains(kv.Key))
            {
                rest.Add(kv.Key);
            }
        }

        if (rest.Count > 0)
        {
            AddSection("Прочее", rest, counts);
        }
    }

    private void AddSection(string title, List<string> ids, Dictionary<string, int> counts)
    {
        Label header = new Label(title);
        header.AddToClassList("chest-category");
        itemsList.contentContainer.Add(header);

        VisualElement grid = new VisualElement();
        grid.AddToClassList("chest-grid");
        itemsList.contentContainer.Add(grid);

        foreach (string id in ids)
        {
            grid.Add(MakeSlot(id, counts[id]));
        }
    }

    private VisualElement MakeSlot(string id, int count)
    {
        VisualElement slot = new VisualElement();
        slot.AddToClassList("chest-slot");

        VisualElement icon = new VisualElement();
        icon.AddToClassList("chest-slot__icon");
        Sprite sprite = ResolveIcon(id);
        if (sprite != null) icon.style.backgroundImage = new StyleBackground(sprite);
        slot.Add(icon);

        Label name = new Label(ItemCatalog.GetDisplayName(id));
        name.AddToClassList("chest-slot__name");
        slot.Add(name);

        Label badge = new Label(count.ToString());
        badge.AddToClassList("chest-slot__count");
        slot.Add(badge);

        return slot;
    }

    private Sprite ResolveIcon(string itemId)
    {
        if (iconProvider != null)
        {
            Sprite fromProvider = iconProvider.GetIcon(itemId);
            if (fromProvider != null) return fromProvider;
        }

        string normalized = ItemCatalog.Normalize(itemId);
        if (ItemSpritePaths.TryGetValue(normalized, out string path))
        {
            Sprite mapped = Resources.Load<Sprite>(path);
            if (mapped != null) return mapped;
        }

        Sprite icon = Resources.Load<Sprite>($"Game/Icons/{normalized}");
        if (icon != null) return icon;

        return Resources.Load<Sprite>($"Game/Items/{normalized}");
    }
}
