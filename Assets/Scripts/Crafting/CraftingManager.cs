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
        EnsureCraftingCollections(progress);
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
        EnsureCraftingCollections(progress);
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
            // Lvl 1 — basics (costlier than before)
            allRecipes.Add(CreateRecipe("health_potion_recipe", ItemCatalog.HealthPotion, 1, 1,
                (ItemCatalog.OrcBlood, 2),
                (ItemCatalog.AppleSapling, 2)));
            allRecipes.Add(CreateRecipe("mana_potion_recipe", ItemCatalog.ManaPotion, 1, 1,
                (ItemCatalog.OrcBlood, 2),
                (ItemCatalog.SakuraSapling, 2)));

            // Lvl 2 — stamina elixir
            allRecipes.Add(CreateRecipe("stamina_elixir_recipe", ItemCatalog.StaminaElixir, 1, 2,
                (ItemCatalog.SakuraSapling, 3),
                (ItemCatalog.AppleSapling, 3),
                (ItemCatalog.OrcBlood, 2)));

            // Lvl 3 — scrolls (now need the rare flower / more wood)
            allRecipes.Add(CreateRecipe("shield_scroll_recipe", ItemCatalog.ShieldScroll, 1, 3,
                (ItemCatalog.OrcBlood, 4),
                (ItemCatalog.OakSapling, 3),
                (ItemCatalog.RareFlower, 1)));
            allRecipes.Add(CreateRecipe("return_scroll_recipe", ItemCatalog.ReturnScroll, 1, 3,
                (ItemCatalog.OrcBlood, 5),
                (ItemCatalog.RareFlower, 2),
                (ItemCatalog.SakuraSapling, 2)));

            // Lvl 4 — greater consumables (consume the base potions + rare flower)
            allRecipes.Add(CreateRecipe("greater_health_potion_recipe", ItemCatalog.GreaterHealthPotion, 1, 4,
                (ItemCatalog.HealthPotion, 3),
                (ItemCatalog.RareFlower, 2),
                (ItemCatalog.OrcBlood, 4)));
            allRecipes.Add(CreateRecipe("greater_mana_potion_recipe", ItemCatalog.GreaterManaPotion, 1, 4,
                (ItemCatalog.ManaPotion, 3),
                (ItemCatalog.SakuraSapling, 4),
                (ItemCatalog.OrcBlood, 3)));

            // Lvl 5 — enhanced scroll + earth amulet (need shaman talisman)
            allRecipes.Add(CreateRecipe("enhanced_shield_scroll_recipe", ItemCatalog.EnhancedShieldScroll, 1, 5,
                (ItemCatalog.ShieldScroll, 2),
                (ItemCatalog.ShamanTalisman, 1),
                (ItemCatalog.OakSapling, 4)));
            allRecipes.Add(CreateRecipe("earth_amulet_recipe", ItemCatalog.EarthAmulet, 1, 5,
                (ItemCatalog.OakSapling, 6),
                (ItemCatalog.ShamanTalisman, 2),
                (ItemCatalog.RareFlower, 3)));

            // Lvl 6 — Эликсир Жизнецвета (imba heal, built on the rare flower)
            allRecipes.Add(CreateRecipe("lifebloom_elixir_recipe", ItemCatalog.LifebloomElixir, 1, 6,
                (ItemCatalog.RareFlower, 4),
                (ItemCatalog.GreaterHealthPotion, 1),
                (ItemCatalog.OrcBlood, 6)));

            // Lvl 7 — Оберег шамана (imba shield + mana) and endgame brew
            allRecipes.Add(CreateRecipe("shaman_ward_recipe", ItemCatalog.ShamanWard, 1, 7,
                (ItemCatalog.ShamanTalisman, 2),
                (ItemCatalog.RareFlower, 3),
                (ItemCatalog.GreaterManaPotion, 1),
                (ItemCatalog.OrcBlood, 6)));
            allRecipes.Add(CreateRecipe("warchief_brew_recipe", ItemCatalog.WarchiefBrew, 1, 7,
                (ItemCatalog.WarchiefTrophy, 1),
                (ItemCatalog.GreenOrcDrop, 3),
                (ItemCatalog.RareFlower, 3),
                (ItemCatalog.OrcBlood, 8)));

            // Lvl 8 — Тоник Кровавой Короны (the ultimate consumable)
            allRecipes.Add(CreateRecipe("bloodcrown_tonic_recipe", ItemCatalog.BloodcrownTonic, 1, 8,
                (ItemCatalog.WarchiefTrophy, 2),
                (ItemCatalog.ShamanTalisman, 3),
                (ItemCatalog.GreaterHealthPotion, 2),
                (ItemCatalog.RareFlower, 4),
                (ItemCatalog.OrcBlood, 10)));
        }

        if (craftableSpells == null || craftableSpells.Length == 0)
        {
            craftableSpells = new[]
            {
                CreateSpell("spell_firebolt", "Огненный Болт", "Базовый огненный снаряд.", ElementType.Fire, SpellType.Projectile, 20, 2f, 15f, 8f, 0f, 0f, 10f, 1,
                    (ItemCatalog.OrcBlood, 3), (ItemCatalog.SakuraSapling, 2)),
                CreateSpell("spell_waterspring", "Источник Воды", "Лечит алхимика во время похода.", ElementType.Water, SpellType.SelfBuff, 0, 4f, 18f, 0f, 0f, 4f, 0f, 1,
                    (ItemCatalog.OrcBlood, 3), (ItemCatalog.AppleSapling, 2)),
                CreateSpell("spell_stoneskin", "Каменная Кожа", "Даёт временный щит.", ElementType.Earth, SpellType.SelfBuff, 35, 6f, 22f, 0f, 0f, 6f, 0f, 2,
                    (ItemCatalog.OrcBlood, 5), (ItemCatalog.OakSapling, 3), (ItemCatalog.RareFlower, 1)),
                CreateSpell("spell_airdash", "Порыв Ветра", "Короткий рывок для спасения.", ElementType.Air, SpellType.Dash, 0, 3f, 14f, 3f, 0f, 0f, 0f, 3,
                    (ItemCatalog.OrcBlood, 4), (ItemCatalog.SakuraSapling, 2), (ItemCatalog.AppleSapling, 2)),

                // Imba combat spells crafted from shop-bought orc trophies.
                CreateSpell("spell_infernobolt", "Адский Болт", "Мощный огненный снаряд с уроном по площади попадания.", ElementType.Fire, SpellType.Projectile, 55, 4f, 35f, 12f, 2f, 0f, 14f, 5,
                    (ItemCatalog.GreenOrcDrop, 2), (ItemCatalog.RareFlower, 2), (ItemCatalog.OrcBlood, 6)),
                CreateSpell("spell_warchief_wrath", "Гнев Вождя", "Разрушительный снаряд вождя орков. Огромный урон.", ElementType.Fire, SpellType.Projectile, 90, 7f, 60f, 14f, 3f, 0f, 16f, 6,
                    (ItemCatalog.WarchiefTrophy, 1), (ItemCatalog.ShamanTalisman, 2), (ItemCatalog.OrcBlood, 8))
            };
        }
    }

    private static void EnsureCraftingCollections(GameProgressData progress)
    {
        if (progress == null)
        {
            return;
        }

        if (progress.crafting == null)
        {
            progress.crafting = new CraftingProgressData();
        }

        if (progress.crafting.craftedSpells == null)
        {
            progress.crafting.craftedSpells = new List<string>();
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
