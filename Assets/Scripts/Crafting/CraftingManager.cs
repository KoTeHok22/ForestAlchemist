using UnityEngine;
using System.Collections.Generic;

public sealed class CraftingManager : MonoBehaviour
{
    private static CraftingManager instance;
    public static CraftingManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("CraftingManager");
                instance = go.AddComponent<CraftingManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    [SerializeField] private List<RecipeDefinition> allRecipes = new List<RecipeDefinition>();
    [SerializeField] private SpellDefinition[] craftableSpells;

    public event System.Action<RecipeDefinition> OnRecipeCrafted;
    public event System.Action<SpellDefinition> OnSpellCrafted;

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

    public bool CanCraft(RecipeDefinition recipe)
    {
        if (recipe == null) return false;
        if (!CraftingProgressionService.Instance.CanCraftRecipe(recipe)) return false;

        var storage = InventoryService.Instance.HomeStorage;
        if (storage == null) return false;

        foreach (var ingredient in recipe.ingredients)
        {
            if (storage.GetItemCount(ingredient.itemName) < ingredient.count) return false;
        }

        return true;
    }

    public bool TryCraft(RecipeDefinition recipe)
    {
        if (!CanCraft(recipe)) return false;

        var storage = InventoryService.Instance.HomeStorage;
        foreach (var ingredient in recipe.ingredients)
        {
            storage.RemoveItem(ingredient.itemName, ingredient.count);
        }

        storage.AddItem(recipe.resultItemName, recipe.resultCount);

        CraftingProgressionService.Instance.GrantXp(10);
        OnRecipeCrafted?.Invoke(recipe);
        GameCore.Instance.SaveProgress();

        return true;
    }

    public bool CanCraftSpell(SpellDefinition spell)
    {
        if (spell == null) return false;
        if (!CraftingProgressionService.Instance.CanCraftSpell(spell)) return false;

        var storage = InventoryService.Instance.HomeStorage;
        if (storage == null) return false;

        var progress = GameCore.Instance.CurrentProgress;
        if (progress != null && progress.crafting.unlockedRecipes.Contains(spell.spellId)) return false;

        foreach (var ingredient in spell.recipeIngredients)
        {
            if (storage.GetItemCount(ingredient.itemName) < ingredient.count) return false;
        }

        return true;
    }

    public bool TryCraftSpell(SpellDefinition spell)
    {
        if (!CanCraftSpell(spell)) return false;

        var storage = InventoryService.Instance.HomeStorage;
        foreach (var ingredient in spell.recipeIngredients)
        {
            storage.RemoveItem(ingredient.itemName, ingredient.count);
        }

        var progress = GameCore.Instance.CurrentProgress;
        if (progress != null && !progress.crafting.unlockedRecipes.Contains(spell.spellId))
        {
            progress.crafting.unlockedRecipes.Add(spell.spellId);
        }

        CraftingProgressionService.Instance.GrantXp(25);
        OnSpellCrafted?.Invoke(spell);
        GameCore.Instance.SaveProgress();

        return true;
    }

    public List<RecipeDefinition> GetAvailableRecipes()
    {
        List<RecipeDefinition> available = new List<RecipeDefinition>();
        int level = CraftingProgressionService.Instance.GetCurrentLevel();

        if (allRecipes != null)
        {
            for (int i = 0; i < allRecipes.Count; i++)
            {
                if (allRecipes[i] != null && allRecipes[i].requiredCraftingLevel <= level)
                {
                    available.Add(allRecipes[i]);
                }
            }
        }

        return available;
    }

    public List<SpellDefinition> GetAvailableSpellRecipes()
    {
        List<SpellDefinition> available = new List<SpellDefinition>();
        int level = CraftingProgressionService.Instance.GetCurrentLevel();

        if (craftableSpells != null)
        {
            for (int i = 0; i < craftableSpells.Length; i++)
            {
                if (craftableSpells[i] != null && craftableSpells[i].requiredCraftingLevel <= level)
                {
                    available.Add(craftableSpells[i]);
                }
            }
        }

        return available;
    }
}
