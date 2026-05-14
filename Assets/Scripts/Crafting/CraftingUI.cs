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
    private bool runtimeUiBuilt;

    private void Awake()
    {
        BuildRuntimeUiIfNeeded();
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
        BuildRuntimeUiIfNeeded();
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
            if (label != null) label.text = ItemCatalog.GetDisplayName(recipe.resultItemName);

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
            bool alreadyCrafted = progress != null && progress.crafting.craftedSpells.Contains(spell.spellId);

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
                bool alreadyCrafted = progress != null && progress.crafting.craftedSpells.Contains(selectedSpell.spellId);
                craftButton.interactable = !alreadyCrafted && CraftingManager.Instance.CanCraftSpell(selectedSpell);
                if (craftButtonText != null) craftButtonText.text = alreadyCrafted ? "Изучено" : "Создать";
            }
        }
        else if (!isSpellMode && selectedRecipe != null)
        {
            if (recipeNameText != null) recipeNameText.text = ItemCatalog.GetDisplayName(selectedRecipe.resultItemName);
            if (recipeIngredientsText != null) recipeIngredientsText.text = FormatIngredients(selectedRecipe.ingredients);
            if (recipeResultText != null) recipeResultText.text = $"Результат: {selectedRecipe.resultCount}x {ItemCatalog.GetDisplayName(selectedRecipe.resultItemName)}";
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
            sb.AppendLine($"{ItemCatalog.GetDisplayName(ingredients[i].itemName)}: {have}/{need}");
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

    private void BuildRuntimeUiIfNeeded()
    {
        if (runtimeUiBuilt || craftingPanel != null)
        {
            runtimeUiBuilt = true;
            return;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("CraftingCanvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        craftingPanel = new GameObject("CraftingPanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        craftingPanel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = craftingPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(960f, 620f);

        craftingPanel.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.08f, 0.97f);

        VerticalLayoutGroup rootLayout = craftingPanel.GetComponent<VerticalLayoutGroup>();
        rootLayout.padding = new RectOffset(18, 18, 18, 18);
        rootLayout.spacing = 10f;
        rootLayout.childControlHeight = false;
        rootLayout.childControlWidth = true;
        rootLayout.childForceExpandHeight = false;

        TMP_Text title = CreateText(craftingPanel.transform, "Крафт и алхимия", 30, FontStyles.Bold);
        title.alignment = TextAlignmentOptions.Center;

        craftingLevelText = CreateText(craftingPanel.transform, string.Empty, 18, FontStyles.Bold);
        craftingXpText = CreateText(craftingPanel.transform, string.Empty, 18, FontStyles.Normal);

        GameObject body = new GameObject("Body", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        body.transform.SetParent(craftingPanel.transform, false);
        HorizontalLayoutGroup bodyLayout = body.GetComponent<HorizontalLayoutGroup>();
        bodyLayout.spacing = 10f;
        bodyLayout.childControlWidth = true;
        bodyLayout.childControlHeight = true;
        bodyLayout.childForceExpandWidth = true;
        bodyLayout.childForceExpandHeight = true;
        body.AddComponent<LayoutElement>().preferredHeight = 430f;

        recipesContainer = CreateListSection(body.transform, "Рецепты", 260f);
        spellsContainer = CreateListSection(body.transform, "Спеллы", 260f);
        Transform detailsSection = CreateListSection(body.transform, "Детали", 360f).transform;

        recipeNameText = CreateText(detailsSection, "Выбери рецепт", 24, FontStyles.Bold);
        recipeIngredientsText = CreateText(detailsSection, string.Empty, 18, FontStyles.Normal);
        recipeIngredientsText.enableWordWrapping = true;
        recipeResultText = CreateText(detailsSection, string.Empty, 18, FontStyles.Normal);
        recipeResultText.enableWordWrapping = true;

        craftButton = CreateButton(detailsSection, "Создать", OnCraftClicked);
        craftButtonText = craftButton.GetComponentInChildren<TMP_Text>();

        recipeButtonTemplate = CreateListButton(recipesContainer.transform, "RecipeTemplate");
        recipeButtonTemplate.SetActive(false);
        spellButtonTemplate = CreateListButton(spellsContainer.transform, "SpellTemplate");
        spellButtonTemplate.SetActive(false);

        closeButton = CreateButton(craftingPanel.transform, "Закрыть", Close);
        closeButton.GetComponent<LayoutElement>().preferredWidth = 180f;

        craftingPanel.SetActive(false);
        runtimeUiBuilt = true;
    }

    private static GameObject CreateListSection(Transform parent, string title, float width)
    {
        GameObject section = new GameObject(title, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        section.transform.SetParent(parent, false);
        section.GetComponent<Image>().color = new Color(0.14f, 0.17f, 0.14f, 0.96f);
        section.GetComponent<LayoutElement>().preferredWidth = width;

        VerticalLayoutGroup layout = section.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;

        TMP_Text titleText = CreateText(section.transform, title, 22, FontStyles.Bold);
        titleText.alignment = TextAlignmentOptions.Center;

        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        content.transform.SetParent(section.transform, false);
        content.GetComponent<LayoutElement>().preferredHeight = 360f;

        VerticalLayoutGroup contentLayout = content.GetComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 6f;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = false;
        contentLayout.childForceExpandHeight = false;
        return content;
    }

    private static GameObject CreateListButton(Transform parent, string name)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.transform.SetParent(parent, false);
        buttonObject.GetComponent<Image>().color = new Color(0.24f, 0.28f, 0.22f, 1f);
        buttonObject.GetComponent<LayoutElement>().preferredHeight = 44f;

        TMP_Text label = CreateText(buttonObject.transform, string.Empty, 18f, FontStyles.Normal);
        label.alignment = TextAlignmentOptions.Center;
        RectTransform rect = label.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(8f, 0f);
        rect.offsetMax = new Vector2(-8f, 0f);
        return buttonObject;
    }

    private static Button CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction callback)
    {
        GameObject buttonObject = CreateListButton(parent, $"Button_{label}");
        Button button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(callback);
        TMP_Text text = buttonObject.GetComponentInChildren<TMP_Text>();
        if (text != null)
        {
            text.text = label;
            text.fontStyle = FontStyles.Bold;
        }

        return button;
    }

    private static TMP_Text CreateText(Transform parent, string content, float fontSize, FontStyles style)
    {
        GameObject textObject = new GameObject("Text", typeof(RectTransform));
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = Color.white;
        text.enableWordWrapping = false;
        return text;
    }
}
