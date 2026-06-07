using UnityEngine;
using System;
using System.Collections.Generic;

public sealed class InventoryService : MonoBehaviour
{
    private static InventoryService instance;
    public static InventoryService Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("InventoryService");
                instance = go.AddComponent<InventoryService>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    public PlayerInventory HomeStorage { get; private set; }
    public PlayerInventory ExpeditionInventory => ExpeditionManager.Instance.ExpeditionInventory;
    private Action homeStorageChangedHandler;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeHomeStorage();
    }

    private void InitializeHomeStorage()
    {
        var progress = GameCore.Instance.CurrentProgress;
        if (progress != null)
        {
            if (HomeStorage != null && homeStorageChangedHandler != null)
            {
                HomeStorage.OnInventoryChanged -= homeStorageChangedHandler;
            }

            HomeStorage = new PlayerInventory(new PlayerInventorySave { slots = progress.homeStorage.slots });
            homeStorageChangedHandler = () => {
                progress.homeStorage.slots = HomeStorage.GetAllSlots();
                GameProgressUtility.Touch(progress);
                GameCore.Instance.SaveProgress();
            };
            HomeStorage.OnInventoryChanged += homeStorageChangedHandler;

            EnsureStarterResources(progress);
            EnsureDefaultLoadout(progress);
        }
    }

    public void ReloadFromCurrentAccount()
    {
        InitializeHomeStorage();
    }

    private void EnsureStarterResources(GameProgressData progress)
    {
        if (HomeStorage == null)
        {
            return;
        }

        EnsureMinimumItem(ItemCatalog.OrcBlood, 5);
        EnsureMinimumItem(ItemCatalog.SakuraSapling, 2);
        EnsureMinimumItem(ItemCatalog.OakSapling, 2);
        EnsureMinimumItem(ItemCatalog.AppleSapling, 2);
        EnsureMinimumItem(ItemCatalog.RareFlower, 1);
    }

    private void EnsureDefaultLoadout(GameProgressData progress)
    {
        if (progress.loadout == null)
        {
            progress.loadout = new ExpeditionLoadoutData();
        }

        if (progress.loadout.consumables == null)
        {
            progress.loadout.consumables = new List<InventorySlot>();
        }

        if (progress.loadout.consumables.Count == 0)
        {
            progress.loadout.consumables.Add(new InventorySlot { itemName = ItemCatalog.HealthPotion, count = 1 });
            progress.loadout.consumables.Add(new InventorySlot { itemName = ItemCatalog.ReturnScroll, count = 1 });
        }
    }

    private void EnsureMinimumItem(string itemName, int minimumCount)
    {
        int currentCount = HomeStorage.GetItemCount(itemName);
        if (currentCount < minimumCount)
        {
            HomeStorage.AddItem(itemName, minimumCount - currentCount);
        }
    }
}
