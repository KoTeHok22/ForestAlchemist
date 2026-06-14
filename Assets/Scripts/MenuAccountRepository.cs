using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public interface IMenuAccountRepository
{
    MenuSaveRoot Load();
    void Save(MenuSaveRoot data);
}

public sealed class JsonMenuAccountRepository : IMenuAccountRepository
{
    private readonly string saveFilePath;

    public JsonMenuAccountRepository(string saveFileName)
    {
        saveFilePath = Path.Combine(Application.persistentDataPath, saveFileName);
    }

    public MenuSaveRoot Load()
    {
        if (!File.Exists(saveFilePath))
        {
            MenuSaveRoot created = CreateEmptySave();
            Save(created);
            return created;
        }

        try
        {
            string json = File.ReadAllText(saveFilePath);
            MenuSaveRoot loaded = JsonUtility.FromJson<MenuSaveRoot>(json);
            Normalize(loaded);
            return loaded;
        }
        catch
        {
            MenuSaveRoot fallback = CreateEmptySave();
            Save(fallback);
            return fallback;
        }
    }

    public void Save(MenuSaveRoot data)
    {
        Normalize(data);

        string directory = Path.GetDirectoryName(saveFilePath);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(saveFilePath, json, Encoding.UTF8);
    }

    private static void Normalize(MenuSaveRoot data)
    {
        if (data == null)
        {
            return;
        }

        data.lastLoggedInUser ??= string.Empty;
        data.accounts ??= new System.Collections.Generic.List<MenuAccountData>();

        foreach (MenuAccountData account in data.accounts.Where(account => account != null))
        {
            account.username ??= string.Empty;
            account.passwordHash ??= string.Empty;
            account.settings ??= MenuSettingsFactory.CreateDefault();
            account.playerData ??= new MenuPlayerProgressData();
            account.playerData.lastUpdatedUtc ??= string.Empty;
            account.gameProgress ??= new GameProgressData();
            account.gameProgress.lastUpdatedUtc ??= string.Empty;
            account.gameProgress.homeStorage ??= new HomeStorageData();
            account.gameProgress.homeStorage.slots ??= new System.Collections.Generic.List<InventorySlot>();
            account.gameProgress.garden ??= new GardenData();
            account.gameProgress.orcs ??= new OrcEvolutionData();
            account.gameProgress.crafting ??= new CraftingProgressData();
            account.gameProgress.crafting.unlockedRecipes ??= new System.Collections.Generic.List<string>();
            account.gameProgress.crafting.craftedSpells ??= new System.Collections.Generic.List<string>();
            account.gameProgress.hotbar ??= new HotbarData();
            account.gameProgress.hotbar.slotItemIds ??= new System.Collections.Generic.List<string>(new string[10]);
            while (account.gameProgress.hotbar.slotItemIds.Count < 10)
            {
                account.gameProgress.hotbar.slotItemIds.Add(string.Empty);
            }

            account.gameProgress.stats ??= new ExpeditionStats();
            account.gameProgress.loadout ??= new ExpeditionLoadoutData();
            account.gameProgress.loadout.consumables ??= new System.Collections.Generic.List<InventorySlot>();
            account.gameProgress.expeditionInventory ??= new HomeStorageData();
            account.gameProgress.expeditionInventory.slots ??= new System.Collections.Generic.List<InventorySlot>();
            account.gameProgress.quests ??= new PlayerQuestSave();
            account.gameProgress.quests.boardQuestIds ??= new System.Collections.Generic.List<string>();
            account.gameProgress.quests.activeQuestIds ??= new System.Collections.Generic.List<string>();
            account.gameProgress.score ??= new PlayerScoreSaveData();
            account.gameProgress.upgrades ??= new PlayerUpgradeData();
        }
    }

    private static MenuSaveRoot CreateEmptySave()
    {
        return new MenuSaveRoot();
    }
}
