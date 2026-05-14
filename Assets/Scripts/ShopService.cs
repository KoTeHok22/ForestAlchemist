using UnityEngine;
using System.Collections.Generic;

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
        public bool isConsumable;
    }

    [SerializeField] private List<ShopItem> shopItems = new List<ShopItem>
    {
        new ShopItem { itemId = ItemCatalog.HealthPotion, displayName = "Зелье здоровья", priceInBlood = 2, isConsumable = true },
        new ShopItem { itemId = ItemCatalog.ManaPotion, displayName = "Зелье маны", priceInBlood = 2, isConsumable = true },
        new ShopItem { itemId = ItemCatalog.ShieldScroll, displayName = "Свиток щита", priceInBlood = 4, isConsumable = true },
        new ShopItem { itemId = ItemCatalog.ReturnScroll, displayName = "Свиток возврата", priceInBlood = 6, isConsumable = true }
    };

    public event System.Action<string> OnItemPurchased;

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

    public List<ShopItem> GetAvailableItems() => new List<ShopItem>(shopItems);

    public bool CanPurchase(string itemId)
    {
        ShopItem item = FindItem(itemId);
        if (item == null) return false;

        var homeStorage = InventoryService.Instance.HomeStorage;
        return homeStorage != null && homeStorage.GetItemCount(ItemCatalog.OrcBlood) >= item.priceInBlood;
    }

    public bool TryPurchase(string itemId)
    {
        if (!CanPurchase(itemId)) return false;

        ShopItem item = FindItem(itemId);
        var homeStorage = InventoryService.Instance.HomeStorage;
        homeStorage.RemoveItem(ItemCatalog.OrcBlood, item.priceInBlood);
        homeStorage.AddItem(item.itemId, 1);

        OnItemPurchased?.Invoke(itemId);
        GameCore.Instance.SaveProgress();

        return true;
    }

    private ShopItem FindItem(string itemId)
    {
        for (int i = 0; i < shopItems.Count; i++)
        {
            if (shopItems[i].itemId == itemId) return shopItems[i];
        }
        return null;
    }
}
