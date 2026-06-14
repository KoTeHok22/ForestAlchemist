using System;
using System.Collections.Generic;

[Serializable]
public sealed class MenuSaveRoot
{
    public string lastLoggedInUser = string.Empty;
    public bool suppressAutoLogin;
    public List<MenuAccountData> accounts = new List<MenuAccountData>();
}

[Serializable]
public sealed class MenuAccountData
{
    public string username = string.Empty;
    public string passwordHash = string.Empty;
    public MenuSettingsData settings = new MenuSettingsData();
    public MenuPlayerProgressData playerData = new MenuPlayerProgressData();
    public GameProgressData gameProgress = new GameProgressData();
}

[Serializable]
public sealed class GameProgressData
{
    public string lastUpdatedUtc = string.Empty;
    public HomeStorageData homeStorage = new HomeStorageData();
    public GardenData garden = new GardenData();
    public OrcEvolutionData orcs = new OrcEvolutionData();
    public CraftingProgressData crafting = new CraftingProgressData();
    public HotbarData hotbar = new HotbarData();
    public ExpeditionStats stats = new ExpeditionStats();
    public ExpeditionLoadoutData loadout = new ExpeditionLoadoutData();
    public HomeStorageData expeditionInventory = new HomeStorageData();
    public PlayerQuestSave quests = new PlayerQuestSave();
    public PlayerScoreSaveData score = new PlayerScoreSaveData();
    public PlayerUpgradeData upgrades = new PlayerUpgradeData();
}

[Serializable]
public sealed class PlayerUpgradeData
{
    public int spellPowerLevel;
    public int meleePowerLevel;
    public int staminaLevel;
    public int manaLevel;
    public int moveSpeedLevel;
}

[Serializable]
public sealed class HomeStorageData
{
    public List<InventorySlot> slots = new List<InventorySlot>();
}

[Serializable]
public sealed class GardenData
{
    public int currentStage;
    public int maxStage = 3;
    public int expeditionsToNextStage = 1;
}

[Serializable]
public sealed class OrcEvolutionData
{
    public int threatLevel = 1;
    public float statMultiplier = 1.0f;
}

[Serializable]
public sealed class CraftingProgressData
{
    public int level = 1;
    public int craftingXp;
    public List<string> unlockedRecipes = new List<string>();
    public List<string> craftedSpells = new List<string>();
}

[Serializable]
public sealed class HotbarData
{
    public List<string> slotItemIds = new List<string>(new string[10]);
}

[Serializable]
public sealed class ExpeditionStats
{
    public int successfulExpeditions;
    public int totalDeaths;
    public int deepestThreatReached;
}

[Serializable]
public sealed class ExpeditionLoadoutData
{
    public List<InventorySlot> consumables = new List<InventorySlot>();
}

[Serializable]
public sealed class MenuSettingsData
{
    public float musicVolume = 1f;
    public float sfxVolume = 1f;
    public int qualityDropdownIndex;
    public int resolutionDropdownIndex;
    public bool musicEnabled = true;
    public bool sfxEnabled = true;
    public bool windowedModeEnabled = true;
    public List<ControlBindingEntry> controlBindings = new List<ControlBindingEntry>();
}

[Serializable]
public sealed class ControlBindingEntry
{
    public string id = string.Empty;
    public string path = string.Empty;
}

[Serializable]
public sealed class MenuPlayerProgressData
{
    public int completedLevel;
    public int score;
    public string lastUpdatedUtc = string.Empty;
}
