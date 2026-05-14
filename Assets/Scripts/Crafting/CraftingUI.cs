using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public sealed class CraftingUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject craftingPanel;
    [SerializeField] private GameObject recipesContainer;
    [SerializeField] private GameObject spellsContainer;

    [Header("Recipe Template")]
    [SerializeField] private GameObject recipeButtonTemplate;
    [SerializeField] private GameObject spellButtonTemplate;

    [Header("Details")]
    [SerializeField] private TMP_Text recipeNameText;
    [SerializeField] private TMP_Text recipeIngredientsText;
    [SerializeField] private TMP_Text recipeResultText;
    [SerializeField] private Button craftButton;
    [SerializeField] private TMP_Text craftButtonText;
    [SerializeField] private TMP_Text craftingLevelText;
    [SerializeField] private TMP_Text craftingXpText;

    [Header("Close")]
    [SerializeField] private Button closeButton;

    private RecipeDefinition selectedRecipe;
    private SpellDefinition selectedSpell;
    private bool isSpellMode;

    private void Awake()
    {
        if (craftingPanel != null) craftingPanel.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (craftButton != null) craftButton.onClick.AddListener(OnCraftClicked);
        if (recipeButtonTemplate != null) recipeButtonTemplate.SetActive(false);
        if (spellButtonTemplate != null) spellButtonTemplate.SetActive(false);
    }

    private void OnDestroy()
    {
        CraftingProgressionService.Instance.OnLevelChanged -= RefreshLevelDisplay;
        CraftingProgressionService.Instance.OnXpChanged -= RefreshXpDisplay;
    }

    public void Open()
    {
        if (recipesContainer == null || spellsContainer == null || recipeButtonTemplate == null || spellButtonTemplate == null)
        {
            return;
        }

        if (craftingPanel != null) craftingPanel.SetActive(true);
        CraftingProgressionService.Instance.OnLevelChanged += RefreshLevelDisplay;
        CraftingProgressionService.Instance.OnXpChanged += RefreshXpDisplay;
        RefreshLevelDisplay(CraftingProgressionService.Instance.GetCurrentLevel(), 0);
        RefreshXpDisplay(CraftingProgressionService.Instance.GetCurrentXp(), CraftingProgressionService.Instance.GetXpForNextLevel());
        PopulateRecipes();
        PopulateSpells();
    }

    public void Close()
    {
        if (craftingPanel != null) craftingPanel.SetActive(false);
    }

    private void PopulateRecipes()
    {
        ClearContainer(recipesContainer);
        List<RecipeDefinition> recipes = CraftingManager.Instance.GetAvailableRecipes();

        for (int i = 0; i < recipes.Count; i++)
        {
            RecipeDefinition recipe = recipes[i];
            GameObject btn = Instantiate(recipeButtonTemplate, recipesContainer.transform);
            btn.SetActive(true);
            btn.name = $"Recipe_{recipe.recipeId}";

            TMP_Text label = btn.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = recipe.resultItemName;

            Button button = btn.GetComponent<Button>();
            if (button != null)
            {
                RecipeDefinition captured = recipe;
                button.onClick.AddListener(() => SelectRecipe(captured));
            }
        }
    }

    private void PopulateSpells()
    {
        ClearContainer(spellsContainer);
        List<SpellDefinition> spells = CraftingManager.Instance.GetAvailableSpellRecipes();
        var progress = GameCore.Instance.CurrentProgress;

        for (int i = 0; i < spells.Count; i++)
        {
            SpellDefinition spell = spells[i];
            bool alreadyCrafted = progress != null && progress.crafting.unlockedRecipes.Contains(spell.spellId);

            GameObject btn = Instantiate(spellButtonTemplate, spellsContainer.transform);
            btn.SetActive(true);
            btn.name = $"Spell_{spell.spellId}";

            TMP_Text label = btn.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = $"{spell.displayName} [{spell.element.ToRussianName()}]{(alreadyCrafted ? " *" : "")}";

            Button button = btn.GetComponent<Button>();
            if (button != null)
            {
                SpellDefinition captured = spell;
                button.onClick.AddListener(() => SelectSpell(captured));
            }
        }
    }

    private void SelectRecipe(RecipeDefinition recipe)
    {
        selectedRecipe = recipe;
        selectedSpell = null;
        isSpellMode = false;
        UpdateDetailsPanel();
    }

    private void SelectSpell(SpellDefinition spell)
    {
        selectedSpell = spell;
        selectedRecipe = null;
        isSpellMode = true;
        UpdateDetailsPanel();
    }

    private void UpdateDetailsPanel()
    {
        if (isSpellMode && selectedSpell != null)
        {
            if (recipeNameText != null) recipeNameText.text = $"{selectedSpell.displayName} [{selectedSpell.element.ToRussianName()}]";
            if (recipeIngredientsText != null)
            {
                recipeIngredientsText.text = FormatIngredients(selectedSpell.recipeIngredients);
            }
            if (recipeResultText != null) recipeResultText.text = $"Заклинание: {selectedSpell.description}";
            if (craftButton != null)
            {
                var progress = GameCore.Instance.CurrentProgress;
                bool alreadyCrafted = progress != null && progress.crafting.unlockedRecipes.Contains(selectedSpell.spellId);
                craftButton.interactable = !alreadyCrafted && CraftingManager.Instance.CanCraftSpell(selectedSpell);
                if (craftButtonText != null) craftButtonText.text = alreadyCrafted ? "Изучено" : "Создать";
            }
        }
        else if (!isSpellMode && selectedRecipe != null)
        {
            if (recipeNameText != null) recipeNameText.text = selectedRecipe.resultItemName;
            if (recipeIngredientsText != null) recipeIngredientsText.text = FormatIngredients(selectedRecipe.ingredients);
            if (recipeResultText != null) recipeResultText.text = $"Результат: {selectedRecipe.resultCount}x {selectedRecipe.resultItemName}";
            if (craftButton != null)
            {
                craftButton.interactable = CraftingManager.Instance.CanCraft(selectedRecipe);
                if (craftButtonText != null) craftButtonText.text = "Создать";
            }
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

    private string FormatIngredients(List<InventorySlot> ingredients)
    {
        if (ingredients == null || ingredients.Count == 0) return "Нет ингредиентов";

        var storage = InventoryService.Instance.HomeStorage;
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        for (int i = 0; i < ingredients.Count; i++)
        {
            int have = storage != null ? storage.GetItemCount(ingredients[i].itemName) : 0;
            int need = ingredients[i].count;
            sb.AppendLine($"{ingredients[i].itemName}: {have}/{need}");
        }

        return sb.ToString();
    }

    private void RefreshLevelDisplay(int level, int xp)
    {
        if (craftingLevelText != null) craftingLevelText.text = $"Уровень крафта: {level}";
    }

    private void RefreshXpDisplay(int currentXp, int nextXp)
    {
        if (craftingXpText != null) craftingXpText.text = $"Опыт: {currentXp}/{nextXp}";
    }

    private void ClearContainer(GameObject container)
    {
        if (container == null) return;

        for (int i = container.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = container.transform.GetChild(i);
            if (child.gameObject == recipeButtonTemplate || child.gameObject == spellButtonTemplate) continue;
            Destroy(child.gameObject);
        }
    }
}
