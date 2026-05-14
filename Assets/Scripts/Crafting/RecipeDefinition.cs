using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Recipe", menuName = "Alchemy/Recipe")]
public sealed class RecipeDefinition : ScriptableObject
{
    public string recipeId;
    public string resultItemName;
    public int resultCount = 1;
    public List<InventorySlot> ingredients = new List<InventorySlot>();
    public int requiredCraftingLevel = 1;
}
