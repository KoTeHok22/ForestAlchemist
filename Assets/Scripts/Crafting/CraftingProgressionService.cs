using UnityEngine;
using System.Collections.Generic;

public sealed class CraftingProgressionService : MonoBehaviour
{
    private static CraftingProgressionService instance;
    public static CraftingProgressionService Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("CraftingProgressionService");
                instance = go.AddComponent<CraftingProgressionService>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    [Header("Progression")]
    [SerializeField] private int baseXpPerCraft = 10;
    [SerializeField] private int[] xpThresholds = { 0, 100, 300, 600, 1000, 1500, 2200, 3000, 4000, 5500 };

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
