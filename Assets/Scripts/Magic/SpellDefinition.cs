using UnityEngine;
using System.Collections.Generic;

public enum SpellType
{
    Projectile,
    SelfBuff,
    AreaOfEffect,
    Dash
}

[CreateAssetMenu(fileName = "NewSpell", menuName = "Alchemy/Spell")]
public sealed class SpellDefinition : ScriptableObject
{
    [Header("Identity")]
    public string spellId;
    public string displayName;
    [TextArea] public string description;
    public ElementType element = ElementType.None;
    public SpellType spellType = SpellType.Projectile;

    [Header("Stats")]
    public int damage = 15;
    public float cooldown = 3f;
    public float manaCost = 20f;
    public float range = 8f;
    public float radius = 1f;
    public float duration = 0f;
    public float speed = 10f;

    [Header("Projectile")]
    public GameObject projectilePrefab;
    public float projectileLifetime = 3f;

    [Header("Visuals")]
    public Sprite icon;
    public Color elementColor = Color.white;

    [Header("Crafting")]
    public int requiredCraftingLevel = 1;
    public List<InventorySlot> recipeIngredients = new List<InventorySlot>();
}
