using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// App UI shop (merchant stall). Keeps the ShopUI class name + Open()/Close() so
/// the existing ShopInteraction continues to find and drive it without changes.
/// Two tabs: Купить (spend Кровь орка on materials/trophies) and Продать (turn
/// surplus loot back into Кровь орка). Renders on a runtime-created UIDocument.
/// </summary>
public sealed class ShopUI : MonoBehaviour
{
    private const string ViewPath = "Assets/UI/HomePanels/ShopView.uxml";
    private const string SettingsPath = "Assets/UI/HomePanels/ShopPanelSettings.asset";
    private const string ViewResourcePath = "UI/HomePanels/ShopView";
    private const string SettingsResourcePath = "UI/HomePanels/ShopPanelSettings";

    private enum Tab { Buy, Sell }

    // Saplings and the blood currency reuse the existing Game/Items art (those
    // sprites already look right). The flower/trophies have dedicated transparent
    // icons under Game/Icons/<id>, which ResolveIcon falls back to automatically.
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
    private VisualElement bloodIcon;
    private Label bloodCount;
    private VisualElement tabBuy;
    private VisualElement tabSell;
    private ScrollView itemsList;
    private AppUIClickRouter clickRouter;

    private Tab activeTab = Tab.Buy;
    private bool built;
    private bool isOpen;

    private void Awake()
    {
        ShopService.Instance.OnShopChanged += OnShopChanged;
    }

    private void OnDestroy()
    {
        if (ShopService.Instance != null)
            ShopService.Instance.OnShopChanged -= OnShopChanged;

        if (isOpen)
        {
            HomeUIBlocker.Release();
            isOpen = false;
        }
    }

    public void Open()
    {
        if (!EnsureBuilt())
        {
            Debug.LogError("[ShopUI] Не удалось открыть лавку: UI не собран.");
            return;
        }

        if (dimBg != null) dimBg.style.display = DisplayStyle.Flex;
        if (panelRoot != null) panelRoot.pickingMode = PickingMode.Position;
        Refresh();
        if (!isOpen)
        {
            HomeUIBlocker.Acquire();
            isOpen = true;
            AudioHooks.Sfx(AudioClipId.SfxHomeShopBell);
            AudioHooks.PanelOpen();
        }
    }

    public void Close()
    {
        if (dimBg != null) dimBg.style.display = DisplayStyle.None;
        // Otherwise the Panel keeps eating all pointer events on top of Pause/HUD.
        if (panelRoot != null) panelRoot.pickingMode = PickingMode.Ignore;
        if (isOpen)
        {
            HomeUIBlocker.Release();
            isOpen = false;
            AudioHooks.PanelClose();
        }
    }

    private void OnShopChanged(string _) => Refresh();

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
            Debug.LogError("[ShopUI] visualTreeAsset или panelSettings не найдены.");
            return false;
        }

        if (!HomePanelUiLoader.TryResolveShell(document, out root, out panelRoot, out dimBg))
        {
            Debug.LogError("[ShopUI] Разметка панели неполная (panel-root/dim-bg).");
            return false;
        }

        bloodIcon = root.Q<VisualElement>("blood-icon");
        bloodCount = root.Q<Label>("blood-count");
        tabBuy = root.Q<VisualElement>("tab-buy");
        tabSell = root.Q<VisualElement>("tab-sell");
        itemsList = root.Q<ScrollView>("items-list");

        clickRouter = new AppUIClickRouter(root);

        VisualElement closeX = root.Q<VisualElement>("btn-close-x");
        if (closeX != null) clickRouter.Add(closeX, Close);
        if (tabBuy != null) clickRouter.Add(tabBuy, () => SwitchTab(Tab.Buy));
        if (tabSell != null) clickRouter.Add(tabSell, () => SwitchTab(Tab.Sell));

        Sprite bloodSprite = ResolveIcon(ItemCatalog.OrcBlood);
        if (bloodIcon != null && bloodSprite != null)
        {
            bloodIcon.style.backgroundImage = new StyleBackground(bloodSprite);
        }

        dimBg.style.display = DisplayStyle.None;
        panelRoot.pickingMode = PickingMode.Ignore;
        built = true;
        return true;
    }

    private void SwitchTab(Tab tab)
    {
        activeTab = tab;
        Refresh();
    }

    private void Refresh()
    {
        if (root == null) return;
        UpdateBloodCount();
        UpdateTabs();
        PopulateItems();
    }

    private void UpdateBloodCount()
    {
        if (bloodCount == null) return;
        bloodCount.text = ShopService.Instance.GetBlood().ToString();
    }

    private void UpdateTabs()
    {
        tabBuy?.EnableInClassList("shop-tab--active", activeTab == Tab.Buy);
        tabSell?.EnableInClassList("shop-tab--active", activeTab == Tab.Sell);
    }

    private void PopulateItems()
    {
        if (itemsList == null) return;
        itemsList.contentContainer.Clear();
        clickRouter?.RemoveDead();

        if (activeTab == Tab.Buy)
        {
            foreach (ShopService.ShopItem item in ShopService.Instance.GetBuyItems())
            {
                string capturedId = item.itemId;
                bool affordable = ShopService.Instance.CanBuy(capturedId);
                var row = MakeRow(item, affordable, "Купить",
                    () => ShopService.Instance.TryBuy(capturedId), subtitle: null);
                itemsList.contentContainer.Add(row);
            }
        }
        else
        {
            foreach (ShopService.ShopItem item in ShopService.Instance.GetSellItems())
            {
                string capturedId = item.itemId;
                int stock = ShopService.Instance.GetSellStock(capturedId);
                bool sellable = stock > 0;
                var row = MakeRow(item, sellable, "Продать",
                    () => ShopService.Instance.TrySell(capturedId), subtitle: $"В доме: {stock}");
                itemsList.contentContainer.Add(row);
            }
        }
    }

    private VisualElement MakeRow(ShopService.ShopItem item, bool enabled, string actionLabel,
        System.Action onClick, string subtitle)
    {
        // The whole row is the button (matches the quest board / old shop). The
        // action verb is only used for the disabled-state styling cue.
        var row = new VisualElement();
        row.AddToClassList("shop-row");
        if (!enabled) row.AddToClassList("shop-row--disabled");

        var icon = new VisualElement();
        icon.AddToClassList("shop-row__icon");
        Sprite sprite = ResolveIcon(item.itemId);
        if (sprite != null) icon.style.backgroundImage = new StyleBackground(sprite);
        row.Add(icon);

        var info = new VisualElement();
        info.AddToClassList("shop-row__info");

        var name = new Label(item.displayName);
        name.AddToClassList("shop-row__name");
        info.Add(name);

        var sub = new Label(string.IsNullOrEmpty(subtitle) ? actionLabel : subtitle);
        sub.AddToClassList("shop-row__sub");
        info.Add(sub);
        row.Add(info);

        var price = new Label($"{item.priceInBlood}");
        price.AddToClassList("shop-row__price");
        row.Add(price);

        var priceIcon = new VisualElement();
        priceIcon.AddToClassList("shop-row__price-icon");
        Sprite bloodSprite = ResolveIcon(ItemCatalog.OrcBlood);
        if (bloodSprite != null) priceIcon.style.backgroundImage = new StyleBackground(bloodSprite);
        row.Add(priceIcon);

        if (enabled) clickRouter.Add(row, onClick);

        return row;
    }

    private static Sprite ResolveIcon(string itemId)
    {
        string normalized = ItemCatalog.Normalize(itemId);
        if (ItemSpritePaths.TryGetValue(normalized, out string path))
        {
            Sprite s = Resources.Load<Sprite>(path);
            if (s != null) return s;
        }
        return Resources.Load<Sprite>($"Game/Icons/{normalized}");
    }
}
