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
            HomeStorage = new PlayerInventory(new PlayerInventorySave { slots = progress.homeStorage.slots });
            HomeStorage.OnInventoryChanged += () => {
                progress.homeStorage.slots = HomeStorage.GetAllSlots();
                GameCore.Instance.SaveProgress();
            };
        }
    }
}
