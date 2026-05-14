using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(PlayerTopDownController))]
public sealed class PlayerSpellCaster : MonoBehaviour
{
    [SerializeField] private float maxMana = 100f;
    [SerializeField] private float manaRegenPerSecond = 5f;
    [SerializeField] private SpellDefinition[] startingSpells;

    private Dictionary<string, SpellCooldown> cooldowns = new Dictionary<string, SpellCooldown>();
    private List<SpellDefinition> unlockedSpells = new List<SpellDefinition>();
    private float currentMana;
    private PlayerTopDownController movement;
    private Camera cachedCamera;

    public float CurrentMana => currentMana;
    public float MaxMana => maxMana;
    public float ManaNormalized => maxMana > 0f ? currentMana / maxMana : 0f;
    public System.Action<float, float> OnManaChanged;

    private void Awake()
    {
        movement = GetComponent<PlayerTopDownController>();
        cachedCamera = Camera.main;
        currentMana = maxMana;
        LoadUnlockedSpells();
    }

    private void Start()
    {
        if (startingSpells == null)
        {
            return;
        }

        for (int i = 0; i < startingSpells.Length; i++)
        {
            if (startingSpells[i] != null && !unlockedSpells.Contains(startingSpells[i]))
            {
                unlockedSpells.Add(startingSpells[i]);
            }
        }

        OnManaChanged?.Invoke(currentMana, maxMana);
    }

    private void Update()
    {
        RegenerateMana(Time.deltaTime);
        TickCooldowns(Time.deltaTime);
    }

    public bool TryCast(SpellDefinition spell)
    {
        if (spell == null) return false;
        if (!unlockedSpells.Contains(spell)) return false;
        if (IsOnCooldown(spell.spellId)) return false;
        if (currentMana < spell.manaCost) return false;

        currentMana -= spell.manaCost;
        StartCooldown(spell.spellId, spell.cooldown);
        OnManaChanged?.Invoke(currentMana, maxMana);

        ExecuteSpell(spell);
        return true;
    }

    public bool TryCastFromHotbar(int slotIndex)
    {
        var progress = GameCore.Instance.CurrentProgress;
        if (progress == null || slotIndex < 0 || slotIndex >= progress.hotbar.slotItemIds.Count) return false;

        string itemId = progress.hotbar.slotItemIds[slotIndex];
        if (string.IsNullOrEmpty(itemId)) return false;

        SpellDefinition spell = ResolveSpell(itemId);
        if (spell != null) return TryCast(spell);

        return false;
    }

    public void UnlockSpell(SpellDefinition spell)
    {
        if (spell == null || unlockedSpells.Contains(spell)) return;
        unlockedSpells.Add(spell);
        SaveUnlockedSpells();
    }

    public bool IsUnlocked(SpellDefinition spell)
    {
        return spell != null && unlockedSpells.Contains(spell);
    }

    public bool IsOnCooldown(string spellId)
    {
        SpellCooldown cd;
        return cooldowns.TryGetValue(spellId, out cd) && cd.remaining > 0f;
    }

    public float GetCooldownRemaining(string spellId)
    {
        SpellCooldown cd;
        if (cooldowns.TryGetValue(spellId, out cd)) return Mathf.Max(0f, cd.remaining);
        return 0f;
    }

    public float GetCooldownProgress(string spellId)
    {
        SpellCooldown cd;
        if (cooldowns.TryGetValue(spellId, out cd) && cd.total > 0f)
            return 1f - (cd.remaining / cd.total);
        return 1f;
    }

    public SpellDefinition ResolveSpell(string itemId)
    {
        if (itemId == null) return null;
        if (startingSpells != null)
        {
            for (int i = 0; i < startingSpells.Length; i++)
            {
                if (startingSpells[i] != null && startingSpells[i].spellId == itemId) return startingSpells[i];
            }
        }
        SpellDefinition[] allSpells = Resources.LoadAll<SpellDefinition>("Game/Spells");
        for (int i = 0; i < allSpells.Length; i++)
        {
            if (allSpells[i].spellId == itemId) return allSpells[i];
        }
        return null;
    }

    public List<SpellDefinition> GetUnlockedSpells() => new List<SpellDefinition>(unlockedSpells);

    public void RestoreMana(int amount)
    {
        if (amount <= 0) return;
        currentMana = Mathf.Min(maxMana, currentMana + amount);
        OnManaChanged?.Invoke(currentMana, maxMana);
    }

    private void ExecuteSpell(SpellDefinition spell)
    {
        Vector2 direction = movement != null ? movement.LookDirection : Vector2.down;

        switch (spell.spellType)
        {
            case SpellType.Projectile:
                SpawnProjectile(spell, direction);
                break;
            case SpellType.Dash:
                ExecuteDash(spell, direction);
                break;
            case SpellType.SelfBuff:
                ApplySelfBuff(spell);
                break;
            case SpellType.AreaOfEffect:
                SpawnAoE(spell);
                break;
        }
    }

    private void SpawnProjectile(SpellDefinition spell, Vector2 direction)
    {
        if (spell.projectilePrefab != null)
        {
            GameObject proj = Instantiate(spell.projectilePrefab, transform.position, Quaternion.identity);
            SpellProjectile projectile = proj.GetComponent<SpellProjectile>();
            if (projectile == null) projectile = proj.AddComponent<SpellProjectile>();

            projectile.Initialize(spell, direction);
            return;
        }

        Vector2 origin = (Vector2)transform.position + direction * 0.5f;
        RaycastHit2D[] hits = Physics2D.CircleCastAll(origin, 0.3f, direction, spell.range, LayerMask.GetMask("Enemy"));
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D collider = hits[i].collider;
            if (collider == null)
            {
                continue;
            }

            IDamageable damageable = collider.GetComponent<IDamageable>();
            if (damageable == null) damageable = collider.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                int finalDamage = Mathf.RoundToInt(spell.damage * spell.element.GetMultiplierAgainst(GetTargetElement(collider.gameObject)));
                damageable.TakeDamage(finalDamage);
            }
        }
    }

    private void ExecuteDash(SpellDefinition spell, Vector2 direction)
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Vector2 target = rb.position + direction * spell.range;
            rb.MovePosition(target);
        }
    }

    private void ApplySelfBuff(SpellDefinition spell)
    {
        PlayerHealth health = GetComponent<PlayerHealth>();
        if (health != null && spell.element == ElementType.Water)
        {
            // Water self-buff: heal
            int healAmount = Mathf.RoundToInt(spell.damage * 0.5f);
            // PlayerHealth doesn't have Heal, we'll add it
            health.Heal(healAmount);
        }
        else if (spell.element == ElementType.Earth)
        {
            PlayerBuffReceiver buffReceiver = GetComponent<PlayerBuffReceiver>();
            if (buffReceiver == null) buffReceiver = gameObject.AddComponent<PlayerBuffReceiver>();
            buffReceiver.ApplyShield(spell.damage, spell.duration > 0 ? spell.duration : 5f);
        }
    }

    private void SpawnAoE(SpellDefinition spell)
    {
        Vector2 center = (Vector2)transform.position + (movement != null ? movement.LookDirection : Vector2.down) * 1.5f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, Mathf.Max(0.5f, spell.radius), LayerMask.GetMask("Enemy"));
        for (int i = 0; i < hits.Length; i++)
        {
            IDamageable damageable = hits[i].GetComponent<IDamageable>();
            if (damageable == null) damageable = hits[i].GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                int finalDamage = Mathf.RoundToInt(spell.damage * spell.element.GetMultiplierAgainst(GetTargetElement(hits[i].gameObject)));
                damageable.TakeDamage(finalDamage);
            }
        }
    }

    private ElementType GetTargetElement(GameObject target)
    {
        EnemyController enemy = target.GetComponent<EnemyController>();
        if (enemy == null) enemy = target.GetComponentInParent<EnemyController>();
        if (enemy != null && enemy.Config != null) return enemy.Config.element;
        return ElementType.None;
    }

    private void RegenerateMana(float deltaTime)
    {
        if (currentMana >= maxMana) return;
        currentMana = Mathf.Min(maxMana, currentMana + manaRegenPerSecond * deltaTime);
        OnManaChanged?.Invoke(currentMana, maxMana);
    }

    private void TickCooldowns(float deltaTime)
    {
        List<string> keys = new List<string>(cooldowns.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            SpellCooldown cd = cooldowns[keys[i]];
            cd.remaining -= deltaTime;
            cooldowns[keys[i]] = cd;
        }
    }

    private void StartCooldown(string spellId, float duration)
    {
        cooldowns[spellId] = new SpellCooldown { remaining = duration, total = duration };
    }

    private void LoadUnlockedSpells()
    {
        unlockedSpells.Clear();
        var progress = GameCore.Instance.CurrentProgress;
        if (progress == null) return;

        if (progress.crafting.craftedSpells == null)
        {
            progress.crafting.craftedSpells = new List<string>();
        }

        if (progress.crafting.craftedSpells.Count == 0 && progress.crafting.unlockedRecipes != null && progress.crafting.unlockedRecipes.Count > 0)
        {
            for (int i = 0; i < progress.crafting.unlockedRecipes.Count; i++)
            {
                string id = progress.crafting.unlockedRecipes[i];
                if (!string.IsNullOrEmpty(id) && id.StartsWith("spell_") && !progress.crafting.craftedSpells.Contains(id))
                {
                    progress.crafting.craftedSpells.Add(id);
                }
            }
        }

        SpellDefinition[] allSpells = Resources.LoadAll<SpellDefinition>("Game/Spells");
        for (int i = 0; i < allSpells.Length; i++)
        {
            if (progress.crafting.craftedSpells.Contains(allSpells[i].spellId))
            {
                unlockedSpells.Add(allSpells[i]);
            }
        }

        AddFallbackSpells(progress);
    }

    private void SaveUnlockedSpells()
    {
        var progress = GameCore.Instance.CurrentProgress;
        if (progress == null) return;

        if (progress.crafting.craftedSpells == null)
        {
            progress.crafting.craftedSpells = new List<string>();
        }

        progress.crafting.craftedSpells.Clear();
        for (int i = 0; i < unlockedSpells.Count; i++)
        {
            progress.crafting.craftedSpells.Add(unlockedSpells[i].spellId);
        }
        GameCore.Instance.SaveProgress();
    }

    private void AddFallbackSpells(GameProgressData progress)
    {
        if (progress.crafting.craftedSpells == null || progress.crafting.craftedSpells.Count == 0)
        {
            return;
        }

        for (int i = 0; i < progress.crafting.craftedSpells.Count; i++)
        {
            SpellDefinition spell = ResolveSpell(progress.crafting.craftedSpells[i]);
            if (spell != null && !unlockedSpells.Contains(spell))
            {
                unlockedSpells.Add(spell);
            }
        }
    }

    private struct SpellCooldown
    {
        public float remaining;
        public float total;
    }
}
