using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyController : MonoBehaviour
{
    private EnemyConfig config;
    public EnemyConfig Config => config;
    private EnemyHealth health;
private EnemyAnimationController animationController;
    private EnemyHPBar hpBar;
    private EnemyStateMachine stateMachine;
    private IPlayerScoreService scoreService;
    private bool isDead;

    public event Action<EnemyController> OnEnemyDied;
    public static event Action<EnemyController> OnAnyEnemyDied;

    public void Initialize(EnemyConfig config, Transform baseTransform, Transform playerTarget, IPlayerScoreService scoreService, float statMultiplier = 1f)
    {
        this.config = config;
        this.scoreService = scoreService;

        gameObject.name = !string.IsNullOrEmpty(config.titleOverride) ? config.titleOverride : config.enemyName;
        transform.localScale = config.spriteScale;

        int scaledHealth = Mathf.RoundToInt(config.maxHealth * statMultiplier);
        Debug.Log($"[EnemyController] Spawned '{config.enemyName}' HP:{scaledHealth}(x{statMultiplier}) at {transform.position}, layer={gameObject.layer} ({LayerMask.LayerToName(gameObject.layer)})", this);

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
        spriteRenderer.sortingOrder = 1;
        spriteRenderer.color = ResolveTint(config);

        DepthSortingConfigurator.ConfigureSingle(gameObject);

        CapsuleCollider2D collider = GetComponent<CapsuleCollider2D>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<CapsuleCollider2D>();
        }
        collider.size = new Vector2(0.19f, 0.28f);
        collider.offset = new Vector2(0.02f, 0.03f);

        health = GetComponent<EnemyHealth>();
        if (health == null)
        {
            health = gameObject.AddComponent<EnemyHealth>();
        }
        health.Initialize(scaledHealth);

        animationController = GetComponent<EnemyAnimationController>();
        if (animationController == null)
        {
            animationController = gameObject.AddComponent<EnemyAnimationController>();
        }
        animationController.Initialize(config, spriteRenderer);

        hpBar = GetComponent<EnemyHPBar>();
        if (hpBar == null)
        {
            hpBar = gameObject.AddComponent<EnemyHPBar>();
        }
        hpBar.Initialize(0.35f);

        EnsureSpecialLoot(config);

        Rigidbody2D body = GetComponent<Rigidbody2D>();
        if (body == null)
        {
            body = gameObject.AddComponent<Rigidbody2D>();
        }
        body.bodyType = RigidbodyType2D.Kinematic;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.freezeRotation = true;

        int playerLayer = LayerMask.NameToLayer("Default");
        int myLayer = gameObject.layer;
        if (playerLayer >= 0 && myLayer >= 0)
        {
            Physics2D.IgnoreLayerCollision(myLayer, playerLayer, true);
        }

        stateMachine = GetComponent<EnemyStateMachine>();
        if (stateMachine == null)
        {
            stateMachine = gameObject.AddComponent<EnemyStateMachine>();
        }
        stateMachine.Initialize(config, baseTransform, playerTarget, body, animationController, health);

        health.OnDamaged += HandleDamaged;
        health.OnDeath += HandleDeath;

        stateMachine.ChangeState(new EnemyPatrolState(stateMachine));
    }

    private void HandleDamaged(int currentHealth)
    {
        hpBar.Show();
        float normalized = config.maxHealth > 0 ? (float)currentHealth / config.maxHealth : 0f;
        hpBar.UpdateHealth(normalized);
    }

    private void HandleDeath()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        hpBar.Hide();

        stateMachine.ChangeState(new EnemyDeathState(stateMachine, OnDeathAnimationFinished));
    }

    private void OnDeathAnimationFinished()
    {
        if (scoreService != null)
        {
            scoreService.AddScore(config.scoreReward);
        }

        OnEnemyDied?.Invoke(this);
        OnAnyEnemyDied?.Invoke(this);
        Destroy(gameObject, 0.5f);
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.OnDamaged -= HandleDamaged;
            health.OnDeath -= HandleDeath;
        }
    }

    private static Color ResolveTint(EnemyConfig config)
    {
        if (config == null)
        {
            return Color.white;
        }

        if (config.tintColor != default)
        {
            return config.tintColor;
        }

        if (config.isBoss)
        {
            return new Color(1f, 0.72f, 0.3f, 1f);
        }

        if (config.isShaman)
        {
            return new Color(0.7f, 0.45f, 1f, 1f);
        }

        return config.element switch
        {
            ElementType.Fire => new Color(1f, 0.5f, 0.35f, 1f),
            ElementType.Water => new Color(0.45f, 0.7f, 1f, 1f),
            ElementType.Earth => new Color(0.58f, 0.42f, 0.22f, 1f),
            ElementType.Air => new Color(0.82f, 0.9f, 1f, 1f),
            _ => Color.white
        };
    }

    private static void EnsureSpecialLoot(EnemyConfig config)
    {
        if (config == null)
        {
            return;
        }

        if (config.lootTable == null)
        {
            config.lootTable = new System.Collections.Generic.List<LootEntry>();
        }

        if (config.isShaman && !HasLoot(config, ItemCatalog.ShamanTalisman))
        {
            config.lootTable.Add(new LootEntry { itemName = ItemCatalog.ShamanTalisman, chance = 0.75f, minAmount = 1, maxAmount = 1 });
        }

        if (!config.isBoss && !config.isShaman && !HasLoot(config, ItemCatalog.GreenOrcDrop))
        {
            config.lootTable.Add(new LootEntry { itemName = ItemCatalog.GreenOrcDrop, chance = 0.45f, minAmount = 1, maxAmount = 1 });
        }

        if (config.isBoss)
        {
            if (string.IsNullOrEmpty(config.bossTrophyItemId))
            {
                config.bossTrophyItemId = ItemCatalog.WarchiefTrophy;
            }

            if (!HasLoot(config, ItemCatalog.OrcBlood))
            {
                config.lootTable.Add(new LootEntry { itemName = ItemCatalog.OrcBlood, chance = 1f, minAmount = 4, maxAmount = 6 });
            }

            if (!HasLoot(config, ItemCatalog.WarchiefTrophy))
            {
                config.lootTable.Add(new LootEntry { itemName = ItemCatalog.WarchiefTrophy, chance = 1f, minAmount = 1, maxAmount = 1 });
            }
        }
        else if (config.isShaman && !HasLoot(config, ItemCatalog.OrcBlood))
        {
            config.lootTable.Add(new LootEntry { itemName = ItemCatalog.OrcBlood, chance = 1f, minAmount = 2, maxAmount = 3 });
        }
    }

    private static bool HasLoot(EnemyConfig config, string itemName)
    {
        for (int i = 0; i < config.lootTable.Count; i++)
        {
            if (config.lootTable[i].itemName == itemName)
            {
                return true;
            }
        }

        return false;
    }
}
