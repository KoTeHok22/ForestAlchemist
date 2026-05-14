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

        gameObject.name = config.enemyName;
        transform.localScale = config.spriteScale;

        int scaledHealth = Mathf.RoundToInt(config.maxHealth * statMultiplier);
        Debug.Log($"[EnemyController] Spawned '{config.enemyName}' HP:{scaledHealth}(x{statMultiplier}) at {transform.position}, layer={gameObject.layer} ({LayerMask.LayerToName(gameObject.layer)})", this);

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
        spriteRenderer.sortingOrder = 1;

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
}
