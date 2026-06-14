using UnityEngine;
using System.Collections.Generic;

public sealed class CraftingProgressionService : MonoBehaviour
{
    private static CraftingProgressionService instance;
    public static CraftingProgressionService Instance => ResolveInstance();

    private static CraftingProgressionService ResolveInstance()
    {
        if (RuntimeSingletonGuard.IsShuttingDown) return null;
        if (instance == null)
        {
            instance = FindAnyObjectByType<CraftingProgressionService>(FindObjectsInactive.Include);
        }
        if (instance == null)
        {
            GameObject go = new GameObject("CraftingProgressionService");
            instance = go.AddComponent<CraftingProgressionService>();
            DontDestroyOnLoad(go);
        }
        return instance;
    }

    [Header("Progression")]
    // Steeper curve + higher cap so the new high-tier recipes (levels 6-8) take
    // real grinding to unlock. Each recipe grants 10 XP, each spell 25 XP.
    [SerializeField] private int[] xpThresholds = { 0, 90, 230, 430, 700, 1050, 1490, 2030, 2680, 3450, 4350, 5390, 6580, 7930 };

    public event System.Action<int, int> OnLevelChanged;
    public event System.Action<int, int> OnXpChanged;

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

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public int GetCurrentLevel()
    {
        var progress = GameCore.Instance.CurrentProgress;
        if (progress == null) return 1;
        return Mathf.Max(1, progress.crafting.level);
    }

    public int GetCurrentXp()
    {
        var progress = GameCore.Instance.CurrentProgress;
        if (progress == null) return 0;
        return progress.crafting.craftingXp;
    }

    public int GetXpForNextLevel()
    {
        int level = GetCurrentLevel();
        if (level >= xpThresholds.Length)
        {
            return xpThresholds[xpThresholds.Length - 1];
        }

        int index = Mathf.Clamp(level, 0, xpThresholds.Length - 1);
        return xpThresholds[index];
    }

    public void GrantXp(int amount)
    {
        var progress = GameCore.Instance.CurrentProgress;
        if (progress == null) return;

        progress.crafting.craftingXp += amount;
        OnXpChanged?.Invoke(progress.crafting.craftingXp, GetXpForNextLevel());

        CheckLevelUp();
        GameCore.Instance.SaveProgress();
    }

    public bool CanCraftRecipe(RecipeDefinition recipe)
    {
        if (recipe == null) return false;
        return GetCurrentLevel() >= recipe.requiredCraftingLevel;
    }

    public bool CanCraftSpell(SpellDefinition spell)
    {
        if (spell == null) return false;
        return GetCurrentLevel() >= spell.requiredCraftingLevel;
    }

    private void CheckLevelUp()
    {
        var progress = GameCore.Instance.CurrentProgress;
        if (progress == null) return;

        int currentLevel = progress.crafting.level;
        int nextThreshold = GetThresholdForLevel(currentLevel + 1);

        while (progress.crafting.craftingXp >= nextThreshold && currentLevel < xpThresholds.Length)
        {
            currentLevel++;
            progress.crafting.level = currentLevel;
            AudioHooks.Sfx(AudioClipId.SfxCraftLevelUp);
            OnLevelChanged?.Invoke(currentLevel, progress.crafting.craftingXp);
            nextThreshold = GetThresholdForLevel(currentLevel + 1);
        }
    }

    private int GetThresholdForLevel(int level)
    {
        int index = Mathf.Clamp(level - 1, 0, xpThresholds.Length - 1);
        return xpThresholds[index];
    }
}
