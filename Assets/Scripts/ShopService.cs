using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Travelling merchant. Unlike crafting (which turns gathered materials into
/// potions/scrolls), the shop trades in the materials themselves:
///   • BUY — saplings, the rare flower and combat trophies that are otherwise
///     only obtainable by farming the forest or killing specific orcs. This lets
///     a player who is stuck (no boss drop, empty garden) buy the ingredients
///     they need to keep crafting and planting.
///   • SELL — convert surplus loot back into Кровь орка (the shop currency),
///     giving expedition loot a guaranteed value sink.
/// Buy prices are always above sell prices for the same item, so there is no
/// arbitrage loop. Potions and scrolls are intentionally NOT sold here — those
/// remain exclusive to crafting.
/// </summary>
public sealed class ShopService : MonoBehaviour
{
    private static ShopService instance;
    public static ShopService Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("ShopService");
                instance = go.AddComponent<ShopService>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    [System.Serializable]
    public class ShopItem
    {
        public string itemId;
        public string displayName;
        public int priceInBlood;
    }

    public const string CurrencyId = ItemCatalog.OrcBlood;

    // What the merchant sells. Materials first (cheap, unblock crafting/garden),
    // then the three combat trophies (pricey — a shortcut past the boss fights).
    [SerializeField] private List<ShopItem> buyItems = new List<ShopItem>
    {
        new ShopItem { itemId = ItemCatalog.SakuraSapling,  displayName = "Саженец сакуры", priceInBlood = 2 },
        new ShopItem { itemId = ItemCatalog.OakSapling,     displayName = "Саженец дуба",   priceInBlood = 2 },
        new ShopItem { itemId = ItemCatalog.AppleSapling,   displayName = "Саженец яблони", priceInBlood = 2 },
        new ShopItem { itemId = ItemCatalog.RareFlower,     displayName = "Редкий цветок",  priceInBlood = 5 },
        new ShopItem { itemId = ItemCatalog.GreenOrcDrop,   displayName = "Трофей зелёного орка", priceInBlood = 6 },
        new ShopItem { itemId = ItemCatalog.ShamanTalisman, displayName = "Шаманский талисман",   priceInBlood = 8 },
        new ShopItem { itemId = ItemCatalog.WarchiefTrophy, displayName = "Трофей вождя",   priceInBlood = 14 }
    };

    // What the merchant buys from the player (loot → Кровь орка). Always cheaper
    // than the corresponding buy price so re-selling is a loss, never a profit.
    [SerializeField] private List<ShopItem> sellItems = new List<ShopItem>
    {
        new ShopItem { itemId = ItemCatalog.SakuraSapling,  displayName = "Саженец сакуры", priceInBlood = 1 },
        new ShopItem { itemId = ItemCatalog.OakSapling,     displayName = "Саженец дуба",   priceInBlood = 1 },
        new ShopItem { itemId = ItemCatalog.AppleSapling,   displayName = "Саженец яблони", priceInBlood = 1 },
        new ShopItem { itemId = ItemCatalog.RareFlower,     displayName = "Редкий цветок",  priceInBlood = 3 },
        new ShopItem { itemId = ItemCatalog.GreenOrcDrop,   displayName = "Трофей зелёного орка", priceInBlood = 3 },
        new ShopItem { itemId = ItemCatalog.ShamanTalisman, displayName = "Шаманский талисман",   priceInBlood = 5 },
        new ShopItem { itemId = ItemCatalog.WarchiefTrophy, displayName = "Трофей вождя",   priceInBlood = 9 }
    };

    public event System.Action<string> OnShopChanged;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public List<ShopItem> GetBuyItems() => new List<ShopItem>(buyItems);
    public List<ShopItem> GetSellItems() => new List<ShopItem>(sellItems);

    public int GetBlood()
    {
        var storage = InventoryService.Instance.HomeStorage;
        return storage != null ? storage.GetItemCount(CurrencyId) : 0;
    }

    public bool CanBuy(string itemId)
    {
        ShopItem item = Find(buyItems, itemId);
        return item != null && GetBlood() >= item.priceInBlood;
    }

    public bool TryBuy(string itemId)
    {
        if (!CanBuy(itemId)) return false;

        ShopItem item = Find(buyItems, itemId);
        var storage = InventoryService.Instance.HomeStorage;
        storage.RemoveItem(CurrencyId, item.priceInBlood);
        storage.AddItem(item.itemId, 1);

        OnShopChanged?.Invoke(itemId);
        GameCore.Instance.SaveProgress();
        return true;
    }

    // How many of this item the player still has available to sell.
    public int GetSellStock(string itemId)
    {
        var storage = InventoryService.Instance.HomeStorage;
        if (storage == null) return 0;
        return storage.GetItemCount(itemId);
    }

    public bool CanSell(string itemId)
    {
        ShopItem item = Find(sellItems, itemId);
        return item != null && GetSellStock(itemId) > 0;
    }

    public bool TrySell(string itemId)
    {
        if (!CanSell(itemId)) return false;

        ShopItem item = Find(sellItems, itemId);
        var storage = InventoryService.Instance.HomeStorage;
        if (!storage.RemoveItem(item.itemId, 1)) return false;
        storage.AddItem(CurrencyId, item.priceInBlood);

        OnShopChanged?.Invoke(itemId);
        GameCore.Instance.SaveProgress();
        return true;
    }

    private static ShopItem Find(List<ShopItem> list, string itemId)
    {
        string normalized = ItemCatalog.Normalize(itemId);
        for (int i = 0; i < list.Count; i++)
        {
            if (ItemCatalog.Normalize(list[i].itemId) == normalized) return list[i];
        }
        return null;
    }
}
