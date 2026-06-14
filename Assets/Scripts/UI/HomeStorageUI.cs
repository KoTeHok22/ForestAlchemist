using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// App UI home storage chest. Replaces the legacy uGUI HomeStorageUI while keeping
/// the class name + Open()/Close() so the existing ChestInteraction drives it
/// unchanged. Renders a clean, read-only grid of everything stored at home over an
/// open-chest board, grouped by category. The expedition loadout and hotbar are
/// configured on the preparation (backpack) screen, so they are intentionally not
/// duplicated here.
/// </summary>
public sealed class HomeStorageUI : MonoBehaviour
{
    private const string ViewPath = "Assets/UI/HomePanels/HomeStorageView.uxml";
    private const string SettingsPath = "Assets/UI/HomePanels/HomeStoragePanelSettings.asset";
    private const string ViewResourcePath = "UI/HomePanels/HomeStorageView";
    private const string SettingsResourcePath = "UI/HomePanels/HomeStoragePanelSettings";

    // Category grouping and display order. Items not listed fall into "Прочее".
    private static readonly (string title, string[] ids)[] Categories =
    {
        ("Ресурсы и трофеи", new[]
        {
            ItemCatalog.OrcBlood, ItemCatalog.RareFlower, ItemCatalog.GreenOrcDrop,
            ItemCatalog.ShamanTalisman, ItemCatalog.WarchiefTrophy
        }),
        ("Саженцы", new[]
        {
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

    // Saplings and the blood currency reuse the existing Game/Items art; everything
    // else falls back to the transparent Game/Icons/<id> sprites.
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
    private Label summary;
    private AppUIClickRouter clickRouter;

    private bool built;
    private bool isOpen;
    private PlayerInventory subscribedStorage;

    public void Open()
    {
        if (!EnsureBuilt())
        {
            Debug.LogError("[HomeStorageUI] Не удалось открыть сундук: UI не собран.");
            return;
        }

        if (dimBg != null) dimBg.style.display = DisplayStyle.Flex;
        if (panelRoot != null) panelRoot.pickingMode = PickingMode.Position;
        if (!isOpen)
        {
            HomeUIBlocker.Acquire();
            isOpen = true;
            AudioHooks.Sfx(AudioClipId.SfxHomeChestOpen);
            AudioHooks.PanelOpen();
        }

        SubscribeStorage();
        Refresh();
    }

    public void Close()
    {
        if (dimBg != null) dimBg.style.display = DisplayStyle.None;
        // Otherwise the Panel keeps eating pointer events on top of the HUD.
        if (panelRoot != null) panelRoot.pickingMode = PickingMode.Ignore;
        if (isOpen)
        {
            HomeUIBlocker.Release();
            isOpen = false;
            AudioHooks.Sfx(AudioClipId.SfxHomeChestClose);
            AudioHooks.PanelClose();
        }
    }

    private void OnDestroy()
    {
        UnsubscribeStorage();
        if (isOpen)
        {
            HomeUIBlocker.Release();
            isOpen = false;
        }
    }

    private bool EnsureBuilt()
    {
        if (built && panelRoot != null && dimBg != null)
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
            Debug.LogError("[HomeStorageUI] visualTreeAsset или panelSettings не найдены.");
            return false;
        }

        if (!HomePanelUiLoader.TryResolveShell(document, out root, out panelRoot, out dimBg))
        {
            Debug.LogError("[HomeStorageUI] Разметка панели неполная (panel-root/dim-bg).");
            return false;
        }

        itemsList = root.Q<ScrollView>("items-list");
        summary = root.Q<Label>("summary");

        clickRouter = new AppUIClickRouter(root);
        VisualElement closeX = root.Q<VisualElement>("btn-close-x");
        if (closeX != null) clickRouter.Add(closeX, Close);

        dimBg.style.display = DisplayStyle.None;
        panelRoot.pickingMode = PickingMode.Ignore;
        built = true;
        return true;
    }

    private void SubscribeStorage()
    {
        PlayerInventory storage = InventoryService.Instance != null ? InventoryService.Instance.HomeStorage : null;
        if (storage == subscribedStorage) return;

        UnsubscribeStorage();
        if (storage != null)
        {
            storage.OnInventoryChanged += OnStorageChanged;
            subscribedStorage = storage;
        }
    }

    private void UnsubscribeStorage()
    {
        if (subscribedStorage != null) subscribedStorage.OnInventoryChanged -= OnStorageChanged;
        subscribedStorage = null;
    }

    private void OnStorageChanged()
    {
        if (isOpen) Refresh();
    }

    private void Refresh()
    {
        if (itemsList == null) return;
        itemsList.contentContainer.Clear();
        clickRouter?.RemoveDead();

        PlayerInventory storage = InventoryService.Instance != null ? InventoryService.Instance.HomeStorage : null;
        if (storage == null) return;

        Dictionary<string, int> counts = new Dictionary<string, int>();
        int total = 0;
        foreach (InventorySlot slot in storage.GetAllSlots())
        {
            if (slot == null || string.IsNullOrEmpty(slot.itemName) || slot.count <= 0) continue;
            string id = ItemCatalog.Normalize(slot.itemName);
            counts.TryGetValue(id, out int existing);
            counts[id] = existing + slot.count;
            total += slot.count;
        }

        if (summary != null)
            summary.text = total > 0 ? $"Всего предметов: {total}" : string.Empty;

        if (counts.Count == 0)
        {
            Label empty = new Label("Сундук пуст. Вернись из похода с добычей.");
            empty.AddToClassList("chest-empty");
            itemsList.contentContainer.Add(empty);
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
                    present.Add(normalized);
            }

            if (present.Count > 0)
                AddSection(category.title, present, counts);
        }

        List<string> rest = new List<string>();
        foreach (KeyValuePair<string, int> kv in counts)
        {
            if (!shown.Contains(kv.Key))
                rest.Add(kv.Key);
        }

        if (rest.Count > 0)
            AddSection("Прочее", rest, counts);
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
            grid.Add(MakeSlot(id, counts[id]));
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

    private static Sprite ResolveIcon(string itemId)
    {
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
