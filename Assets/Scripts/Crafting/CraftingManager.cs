using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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

        EnsureFallbackContent();
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
        if (progress != null && progress.crafting.craftedSpells.Contains(spell.spellId)) return false;

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
        if (progress != null && !progress.crafting.craftedSpells.Contains(spell.spellId))
        {
            progress.crafting.craftedSpells.Add(spell.spellId);
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

    private void EnsureFallbackContent()
    {
        if (allRecipes == null)
        {
            allRecipes = new List<RecipeDefinition>();
        }

        if (allRecipes.Count == 0)
        {
            allRecipes.Add(CreateRecipe("health_potion_recipe", ItemCatalog.HealthPotion, 1, 1,
                (ItemCatalog.OrcBlood, 1),
                (ItemCatalog.AppleSapling, 1)));
            allRecipes.Add(CreateRecipe("mana_potion_recipe", ItemCatalog.ManaPotion, 1, 1,
                (ItemCatalog.OrcBlood, 1),
                (ItemCatalog.SakuraSapling, 1)));
            allRecipes.Add(CreateRecipe("shield_scroll_recipe", ItemCatalog.ShieldScroll, 1, 2,
                (ItemCatalog.OrcBlood, 3),
                (ItemCatalog.OakSapling, 2)));
            allRecipes.Add(CreateRecipe("return_scroll_recipe", ItemCatalog.ReturnScroll, 1, 2,
                (ItemCatalog.OrcBlood, 4),
                (ItemCatalog.RareFlower, 1)));
        }

        if (craftableSpells == null || craftableSpells.Length == 0)
        {
            craftableSpells = new[]
            {
                CreateSpell("spell_firebolt", "Огненный Болт", "Базовый огненный снаряд.", ElementType.Fire, SpellType.Projectile, 20, 2f, 15f, 8f, 0f, 0f, 10f, 1,
                    (ItemCatalog.OrcBlood, 2), (ItemCatalog.SakuraSapling, 1)),
                CreateSpell("spell_waterspring", "Источник Воды", "Лечит алхимика во время похода.", ElementType.Water, SpellType.SelfBuff, 0, 4f, 18f, 0f, 0f, 4f, 0f, 1,
                    (ItemCatalog.OrcBlood, 2), (ItemCatalog.AppleSapling, 1)),
                CreateSpell("spell_stoneskin", "Каменная Кожа", "Даёт временный щит.", ElementType.Earth, SpellType.SelfBuff, 35, 6f, 22f, 0f, 0f, 6f, 0f, 2,
                    (ItemCatalog.OrcBlood, 4), (ItemCatalog.OakSapling, 2)),
                CreateSpell("spell_airdash", "Порыв Ветра", "Короткий рывок для спасения.", ElementType.Air, SpellType.Dash, 0, 3f, 14f, 3f, 0f, 0f, 0f, 2,
                    (ItemCatalog.OrcBlood, 3), (ItemCatalog.SakuraSapling, 1), (ItemCatalog.AppleSapling, 1))
            };
        }
    }

    private static RecipeDefinition CreateRecipe(string recipeId, string resultItemName, int resultCount, int requiredLevel, params (string itemName, int count)[] ingredients)
    {
        RecipeDefinition recipe = ScriptableObject.CreateInstance<RecipeDefinition>();
        recipe.recipeId = recipeId;
        recipe.resultItemName = resultItemName;
        recipe.resultCount = resultCount;
        recipe.requiredCraftingLevel = requiredLevel;
        recipe.ingredients = ingredients.Select(ingredient => new InventorySlot
        {
            itemName = ingredient.itemName,
            count = ingredient.count
        }).ToList();
        return recipe;
    }

    private static SpellDefinition CreateSpell(
        string spellId,
        string displayName,
        string description,
        ElementType element,
        SpellType spellType,
        int damage,
        float cooldown,
        float manaCost,
        float range,
        float radius,
        float duration,
        float speed,
        int requiredLevel,
        params (string itemName, int count)[] ingredients)
    {
        SpellDefinition spell = ScriptableObject.CreateInstance<SpellDefinition>();
        spell.spellId = spellId;
        spell.displayName = displayName;
        spell.description = description;
        spell.element = element;
        spell.spellType = spellType;
        spell.damage = damage;
        spell.cooldown = cooldown;
        spell.manaCost = manaCost;
        spell.range = range;
        spell.radius = radius;
        spell.duration = duration;
        spell.speed = speed;
        spell.requiredCraftingLevel = requiredLevel;
        spell.recipeIngredients = ingredients.Select(ingredient => new InventorySlot
        {
            itemName = ingredient.itemName,
            count = ingredient.count
        }).ToList();
        spell.elementColor = element switch
        {
            ElementType.Fire => new Color(1f, 0.4f, 0.2f),
            ElementType.Water => new Color(0.3f, 0.7f, 1f),
            ElementType.Earth => new Color(0.55f, 0.4f, 0.2f),
            ElementType.Air => new Color(0.85f, 0.95f, 1f),
            _ => Color.white
        };
        return spell;
    }
}
