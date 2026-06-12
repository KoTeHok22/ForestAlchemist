using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// App UI crafting board. Keeps the CraftingUI class name + Open()/Close() so
/// existing interaction scripts (CraftingStationInteraction, ChestInteraction-as-crafting)
/// can drive it without changes. Renders on a runtime-created UIDocument hosted on a
/// dedicated child GameObject (so it doesn't share a Transform with the carrier
/// sprite/collider and lose pointer events).
/// </summary>
public sealed class CraftingUI : MonoBehaviour
{
    private const string ViewPath = "Assets/UI/HomePanels/CraftingView.uxml";
    private const string SettingsPath = "Assets/UI/HomePanels/CraftingPanelSettings.asset";
    private const string ChildName = "CraftingOverlay_AppUI";

    private UIDocument document;
    private VisualElement root;
    private VisualElement panelRoot;
    private VisualElement dimBg;
    private VisualElement tabRecipes;
    private VisualElement tabSpells;
    private ScrollView itemsList;
    private Label craftLevel;
    private Label craftXp;
    private Label detailName;
    private Label detailResult;
    private Label detailIngredients;
    private VisualElement btnCraft;
    private Label btnCraftLabel;
    private AppUIClickRouter clickRouter;

    private bool built;
    private bool isSpellMode;
    private RecipeDefinition selectedRecipe;
    private SpellDefinition selectedSpell;
    private VisualElement selectedRow;
    private System.Action onCloseRequested;

    private void OnDestroy()
    {
        if (CraftingProgressionService.Instance != null)
        {
            CraftingProgressionService.Instance.OnLevelChanged -= RefreshLevelDisplay;
            CraftingProgressionService.Instance.OnXpChanged -= RefreshXpDisplay;
        }
    }

    public void Open() => Open(null);

    public void Open(System.Action onClose)
    {
        EnsureBuilt();
        if (root == null) return;

        onCloseRequested = onClose;
        if (dimBg != null) dimBg.style.display = DisplayStyle.Flex;
        if (panelRoot != null) panelRoot.pickingMode = PickingMode.Position;

        CraftingProgressionService.Instance.OnLevelChanged -= RefreshLevelDisplay;
        CraftingProgressionService.Instance.OnXpChanged -= RefreshXpDisplay;
        CraftingProgressionService.Instance.OnLevelChanged += RefreshLevelDisplay;
        CraftingProgressionService.Instance.OnXpChanged += RefreshXpDisplay;

        RefreshLevelDisplay(CraftingProgressionService.Instance.GetCurrentLevel(), 0);
        RefreshXpDisplay(CraftingProgressionService.Instance.GetCurrentXp(), CraftingProgressionService.Instance.GetXpForNextLevel());

        SetTab(false);
    }

    public void Close()
    {
        if (dimBg != null) dimBg.style.display = DisplayStyle.None;
        if (panelRoot != null) panelRoot.pickingMode = PickingMode.Ignore;

        if (CraftingProgressionService.Instance != null)
        {
            CraftingProgressionService.Instance.OnLevelChanged -= RefreshLevelDisplay;
            CraftingProgressionService.Instance.OnXpChanged -= RefreshXpDisplay;
        }

        var cb = onCloseRequested;
        onCloseRequested = null;
        cb?.Invoke();
    }

    private void EnsureBuilt()
    {
        if (built && document != null) return;

        Transform existing = transform.Find(ChildName);
        GameObject host = existing != null ? existing.gameObject : new GameObject(ChildName);
        if (existing == null) host.transform.SetParent(null, false);

        document = host.GetComponent<UIDocument>();
        if (document == null) document = host.AddComponent<UIDocument>();

#if UNITY_EDITOR
        if (document.visualTreeAsset == null)
            document.visualTreeAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ViewPath);
        if (document.panelSettings == null)
            document.panelSettings = UnityEditor.AssetDatabase.LoadAssetAtPath<PanelSettings>(SettingsPath);
#endif

        root = document.rootVisualElement;
        if (root == null) return;

        panelRoot = root.Q<VisualElement>("panel-root");
        dimBg = root.Q<VisualElement>("dim-bg");
        craftLevel = root.Q<Label>("craft-level");
        craftXp = root.Q<Label>("craft-xp");
        tabRecipes = root.Q<VisualElement>("tab-recipes");
        tabSpells = root.Q<VisualElement>("tab-spells");
        itemsList = root.Q<ScrollView>("items-list");
        detailName = root.Q<Label>("detail-name");
        detailResult = root.Q<Label>("detail-result");
        detailIngredients = root.Q<Label>("detail-ingredients");
        btnCraft = root.Q<VisualElement>("btn-craft");
        btnCraftLabel = root.Q<Label>("btn-craft-label");

        clickRouter = new AppUIClickRouter(root);
        var closeX = root.Q<VisualElement>("btn-close-x");
        if (closeX != null) clickRouter.Add(closeX, Close);
        if (tabRecipes != null) clickRouter.Add(tabRecipes, () => SetTab(false));
        if (tabSpells != null) clickRouter.Add(tabSpells, () => SetTab(true));
        if (btnCraft != null) clickRouter.Add(btnCraft, OnCraftClicked);

        if (dimBg != null) dimBg.style.display = DisplayStyle.None;
        if (panelRoot != null) panelRoot.pickingMode = PickingMode.Ignore;
        built = true;
    }

    private void SetTab(bool spells)
    {
        isSpellMode = spells;
        selectedRecipe = null;
        selectedSpell = null;
        selectedRow = null;

        if (tabRecipes != null)
        {
            if (!spells) tabRecipes.AddToClassList("craft-tab--active");
            else tabRecipes.RemoveFromClassList("craft-tab--active");
        }
        if (tabSpells != null)
        {
            if (spells) tabSpells.AddToClassList("craft-tab--active");
            else tabSpells.RemoveFromClassList("craft-tab--active");
        }

        if (spells) PopulateSpells();
        else PopulateRecipes();

        ClearDetails();
    }

    private void PopulateRecipes()
    {
        if (itemsList == null) return;
        itemsList.contentContainer.Clear();
        clickRouter?.RemoveDead();

        List<RecipeDefinition> recipes = CraftingManager.Instance.GetAvailableRecipes();
        foreach (var recipe in recipes)
        {
            RecipeDefinition captured = recipe;
            var row = new VisualElement();
            row.AddToClassList("craft-row");

            var label = new Label(ItemCatalog.GetDisplayName(recipe.resultItemName));
            label.AddToClassList("craft-row__name");
            row.Add(label);

            clickRouter.Add(row, () => SelectRecipe(captured, row));
            itemsList.contentContainer.Add(row);
        }
    }

    private void PopulateSpells()
    {
        if (itemsList == null) return;
        itemsList.contentContainer.Clear();
        clickRouter?.RemoveDead();

        List<SpellDefinition> spells = CraftingManager.Instance.GetAvailableSpellRecipes();
        var progress = GameCore.Instance.CurrentProgress;

        foreach (var spell in spells)
        {
            bool alreadyCrafted = progress != null && progress.crafting.craftedSpells.Contains(spell.spellId);
            SpellDefinition captured = spell;

            var row = new VisualElement();
            row.AddToClassList("craft-row");
            if (alreadyCrafted) row.AddToClassList("craft-row--locked");

            var label = new Label($"{spell.displayName} [{spell.element.ToRussianName()}]{(alreadyCrafted ? " ★" : "")}");
            label.AddToClassList("craft-row__name");
            row.Add(label);

            clickRouter.Add(row, () => SelectSpell(captured, row));
            itemsList.contentContainer.Add(row);
        }
    }

    private void SelectRecipe(RecipeDefinition recipe, VisualElement row)
    {
        selectedRecipe = recipe;
        selectedSpell = null;
        UpdateSelectedRow(row);
        UpdateDetailsPanel();
    }

    private void SelectSpell(SpellDefinition spell, VisualElement row)
    {
        selectedSpell = spell;
        selectedRecipe = null;
        UpdateSelectedRow(row);
        UpdateDetailsPanel();
    }

    private void UpdateSelectedRow(VisualElement row)
    {
        if (selectedRow != null) selectedRow.RemoveFromClassList("craft-row--selected");
        selectedRow = row;
        if (selectedRow != null) selectedRow.AddToClassList("craft-row--selected");
    }

    private void ClearDetails()
    {
        if (detailName != null) detailName.text = isSpellMode ? "Выбери заклинание" : "Выбери рецепт";
        if (detailResult != null) detailResult.text = string.Empty;
        if (detailIngredients != null) detailIngredients.text = string.Empty;
        if (btnCraftLabel != null) btnCraftLabel.text = "Создать";
        if (btnCraft != null) btnCraft.SetEnabled(false);
    }

    private void UpdateDetailsPanel()
    {
        if (isSpellMode && selectedSpell != null)
        {
            if (detailName != null) detailName.text = $"{selectedSpell.displayName} [{selectedSpell.element.ToRussianName()}]";
            if (detailResult != null) detailResult.text = selectedSpell.description ?? string.Empty;
            if (detailIngredients != null) detailIngredients.text = FormatIngredients(selectedSpell.recipeIngredients);

            var progress = GameCore.Instance.CurrentProgress;
            bool alreadyCrafted = progress != null && progress.crafting.craftedSpells.Contains(selectedSpell.spellId);
            bool canCraft = !alreadyCrafted && CraftingManager.Instance.CanCraftSpell(selectedSpell);

            if (btnCraftLabel != null) btnCraftLabel.text = alreadyCrafted ? "Изучено" : "Создать";
            if (btnCraft != null) btnCraft.SetEnabled(canCraft);
        }
        else if (!isSpellMode && selectedRecipe != null)
        {
            if (detailName != null) detailName.text = ItemCatalog.GetDisplayName(selectedRecipe.resultItemName);
            if (detailResult != null) detailResult.text = $"Результат: {selectedRecipe.resultCount}× {ItemCatalog.GetDisplayName(selectedRecipe.resultItemName)}";
            if (detailIngredients != null) detailIngredients.text = FormatIngredients(selectedRecipe.ingredients);

            if (btnCraftLabel != null) btnCraftLabel.text = "Создать";
            if (btnCraft != null) btnCraft.SetEnabled(CraftingManager.Instance.CanCraft(selectedRecipe));
        }
        else
        {
            ClearDetails();
        }
    }

    private void OnCraftClicked()
    {
        if (isSpellMode && selectedSpell != null)
        {
            if (CraftingManager.Instance.TryCraftSpell(selectedSpell))
            {
                PopulateSpells();
                UpdateDetailsPanel();
            }
        }
        else if (!isSpellMode && selectedRecipe != null)
        {
            if (CraftingManager.Instance.TryCraft(selectedRecipe))
            {
                UpdateDetailsPanel();
            }
        }
    }

    private static string FormatIngredients(List<InventorySlot> ingredients)
    {
        if (ingredients == null || ingredients.Count == 0) return "Нет ингредиентов";

        var storage = InventoryService.Instance.HomeStorage;
        var sb = new System.Text.StringBuilder();

        for (int i = 0; i < ingredients.Count; i++)
        {
            int have = storage != null ? storage.GetItemCount(ingredients[i].itemName) : 0;
            int need = ingredients[i].count;
            sb.AppendLine($"{ItemCatalog.GetDisplayName(ingredients[i].itemName)}: {have}/{need}");
        }

        return sb.ToString();
    }

    private void RefreshLevelDisplay(int level, int _)
    {
        if (craftLevel != null) craftLevel.text = $"Уровень крафта: {level}";
    }

    private void RefreshXpDisplay(int currentXp, int nextXp)
    {
        if (craftXp != null) craftXp.text = $"Опыт: {currentXp}/{nextXp}";
    }
}
