using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// App UI shop. Keeps the ShopUI class name + Open()/Close() so the existing
/// ShopInteraction continues to find and drive it without changes. Renders on a
/// runtime-created UIDocument (doska bulletin board) instead of the legacy Canvas.
/// </summary>
public sealed class ShopUI : MonoBehaviour
{
    private const string ViewPath = "Assets/UI/HomePanels/ShopView.uxml";
    private const string SettingsPath = "Assets/UI/HomePanels/HomePanelsSettings.asset";

    private UIDocument document;
    private VisualElement root;
    private VisualElement dimBg;
    private Label bloodCount;
    private ScrollView itemsList;

    private bool built;

    private void Awake()
    {
        ShopService.Instance.OnItemPurchased += Refresh;
    }

    private void OnDestroy()
    {
        if (ShopService.Instance != null)
            ShopService.Instance.OnItemPurchased -= Refresh;
    }

    public void Open()
    {
        EnsureBuilt();
        if (root == null) return;
        if (dimBg != null) dimBg.style.display = DisplayStyle.Flex;
        Refresh(null);
    }

    public void Close()
    {
        if (dimBg != null) dimBg.style.display = DisplayStyle.None;
    }

    private void EnsureBuilt()
    {
        if (built && document != null) return;

        document = GetComponent<UIDocument>();
        if (document == null) document = gameObject.AddComponent<UIDocument>();

#if UNITY_EDITOR
        if (document.visualTreeAsset == null)
            document.visualTreeAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ViewPath);
        if (document.panelSettings == null)
            document.panelSettings = UnityEditor.AssetDatabase.LoadAssetAtPath<PanelSettings>(SettingsPath);
#endif

        root = document.rootVisualElement;
        if (root == null) return;

        dimBg = root.Q<VisualElement>("dim-bg");
        bloodCount = root.Q<Label>("blood-count");
        itemsList = root.Q<ScrollView>("items-list");

        var closeX = root.Q<VisualElement>("btn-close-x");
        if (closeX != null) closeX.RegisterCallback<ClickEvent>(_ => Close());

        if (dimBg != null) dimBg.style.display = DisplayStyle.None;
        built = true;
    }

    private void Refresh(string _)
    {
        if (root == null) return;
        UpdateBloodCount();
        PopulateItems();
    }

    private void UpdateBloodCount()
    {
        if (bloodCount == null) return;
        var homeStorage = InventoryService.Instance.HomeStorage;
        int blood = homeStorage != null ? homeStorage.GetItemCount(ItemCatalog.OrcBlood) : 0;
        bloodCount.text = $"{ItemCatalog.GetDisplayName(ItemCatalog.OrcBlood)}: {blood}";
    }

    private void PopulateItems()
    {
        if (itemsList == null) return;
        itemsList.contentContainer.Clear();

        List<ShopService.ShopItem> items = ShopService.Instance.GetAvailableItems();
        foreach (ShopService.ShopItem item in items)
        {
            string capturedId = item.itemId;

            var row = new VisualElement();
            row.AddToClassList("shop-row");

            var name = new Label(item.displayName);
            name.AddToClassList("shop-row__name");
            row.Add(name);

            var price = new Label($"{item.priceInBlood} крови");
            price.AddToClassList("shop-row__price");
            row.Add(price);

            row.RegisterCallback<ClickEvent>(_ => ShopService.Instance.TryPurchase(capturedId));
            itemsList.contentContainer.Add(row);
        }
    }
}
