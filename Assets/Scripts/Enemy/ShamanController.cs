using UnityEngine;

[DisallowMultipleComponent]
public sealed class ShamanController : MonoBehaviour
{
    [SerializeField] private float attackRange = 6f;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float projectileSpeed = 5f;
    [SerializeField] private GameObject projectilePrefab;

    private EnemyConfig config;
    private Transform playerTarget;
    private EnemyStateMachine stateMachine;
    private float cooldownTimer;

    public void Initialize(EnemyConfig config, Transform baseTransform, Transform playerTarget, IPlayerScoreService scoreService)
    {
        this.config = config;
        this.playerTarget = playerTarget;
        attackRange = config.shamanAttackRange;
        attackCooldown = config.shamanAttackCooldown;
        projectileSpeed = config.shamanProjectileSpeed;
        projectilePrefab = config.shamanProjectilePrefab;
    }

    public void TryAttack()
    {
        if (playerTarget == null) return;
        if (cooldownTimer > 0f) return;

        float distance = Vector2.Distance(transform.position, playerTarget.position);
        if (distance > attackRange) return;

        cooldownTimer = attackCooldown;
        FireProjectile();
    }

    private void FireProjectile()
    {
        if (playerTarget == null) return;

        Vector2 direction = ((Vector2)playerTarget.position - (Vector2)transform.position).normalized;

        if (projectilePrefab != null)
        {
            GameObject proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            Rigidbody2D rb = proj.GetComponent<Rigidbody2D>();
            if (rb == null) rb = proj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.linearVelocity = direction * projectileSpeed;

            CircleCollider2D col = proj.GetComponent<CircleCollider2D>();
            if (col == null) col = proj.AddComponent<CircleCollider2D>();
            col.isTrigger = true;

            SpriteRenderer sr = proj.GetComponent<SpriteRenderer>();
            if (sr == null) sr = proj.AddComponent<SpriteRenderer>();
            sr.color = config != null ? ResolveProjectileColor(config.element) : new Color(0.8f, 0.45f, 1f, 1f);
            sr.sortingOrder = 6;

            EnemyProjectile enemyProj = proj.AddComponent<EnemyProjectile>();
            enemyProj.damage = config.attackDamage;
            enemyProj.element = config.element;
            Destroy(proj, 5f);
        }
    }

    private void Update()
    {
        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;
        TryAttack();
    }

    private static Color ResolveProjectileColor(ElementType element)
    {
        return element switch
        {
            ElementType.Fire => new Color(1f, 0.45f, 0.25f, 1f),
            ElementType.Water => new Color(0.45f, 0.75f, 1f, 1f),
            ElementType.Earth => new Color(0.62f, 0.48f, 0.28f, 1f),
            ElementType.Air => new Color(0.85f, 0.92f, 1f, 1f),
            _ => new Color(0.8f, 0.45f, 1f, 1f)
        };
    }
}

[RequireComponent(typeof(Rigidbody2D))]
public sealed class EnemyProjectile : MonoBehaviour
{
    public int damage = 10;
    public ElementType element = ElementType.None;

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth == null) playerHealth = other.GetComponentInParent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        if (!other.isTrigger)
        {
            Destroy(gameObject);
        }
    }
}
