using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

[Serializable]
public sealed class PlayerInventorySave
{
    public List<InventorySlot> slots = new List<InventorySlot>();
}

[Serializable]
public sealed class InventorySlot
{
    public string itemName;
    public int count;
}

public sealed class PlayerInventory
{
    private readonly string filePath;
    private PlayerInventorySave saveData;

    public event Action OnInventoryChanged;

    public PlayerInventory(string fileName = "player_inventory.json")
    {
        filePath = Path.Combine(Application.persistentDataPath, fileName);
        Load();
    }

    public PlayerInventory(PlayerInventorySave existingData)
    {
        saveData = existingData;
    }

    public void Clear()
    {
        saveData.slots.Clear();
        Save();
        OnInventoryChanged?.Invoke();
    }

    public int GetItemCount(string itemName)
    {
        InventorySlot slot = FindSlot(itemName);
        return slot?.count ?? 0;
    }

    public void AddItem(string itemName, int amount = 1)
    {
        InventorySlot slot = FindSlot(itemName);
        if (slot == null)
        {
            slot = new InventorySlot { itemName = itemName, count = 0 };
            saveData.slots.Add(slot);
        }

        slot.count += amount;
        Save();
        OnInventoryChanged?.Invoke();
    }

    public bool RemoveItem(string itemName, int amount = 1)
    {
        InventorySlot slot = FindSlot(itemName);
        if (slot == null || slot.count < amount)
            return false;

        slot.count -= amount;
        if (slot.count <= 0)
        {
            saveData.slots.Remove(slot);
        }

        Save();
        OnInventoryChanged?.Invoke();
        return true;
    }

    public List<InventorySlot> GetAllSlots()
    {
        return new List<InventorySlot>(saveData.slots);
    }

    private InventorySlot FindSlot(string itemName)
    {
        for (int i = 0; i < saveData.slots.Count; i++)
        {
            if (saveData.slots[i].itemName == itemName)
                return saveData.slots[i];
        }
        return null;
    }

    private void Load()
    {
        if (!File.Exists(filePath))
        {
            saveData = new PlayerInventorySave();
            return;
        }

        string json = File.ReadAllText(filePath, Encoding.UTF8);
        saveData = JsonUtility.FromJson<PlayerInventorySave>(json) ?? new PlayerInventorySave();
    }

    private void Save()
    {
        string directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(filePath, json, Encoding.UTF8);
    }
}
