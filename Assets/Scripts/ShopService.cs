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
        new ShopItem { itemId = "return_scroll", displayName = "Свиток Возврата", priceInBlood = 5, isConsumable = true },
        new ShopItem { itemId = "health_potion", displayName = "Зелье Здоровья", priceInBlood = 3, isConsumable = true },
        new ShopItem { itemId = "mana_potion", displayName = "Зелье Маны", priceInBlood = 3, isConsumable = true },
        new ShopItem { itemId = "shield_scroll", displayName = "Свиток Щита", priceInBlood = 8, isConsumable = true }
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
        return homeStorage != null && homeStorage.GetItemCount("КровьОрка") >= item.priceInBlood;
    }

    public bool TryPurchase(string itemId)
    {
        if (!CanPurchase(itemId)) return false;

        ShopItem item = FindItem(itemId);
        var homeStorage = InventoryService.Instance.HomeStorage;
        homeStorage.RemoveItem("КровьОрка", item.priceInBlood);
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
