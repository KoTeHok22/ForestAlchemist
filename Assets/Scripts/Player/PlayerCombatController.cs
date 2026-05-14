using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class PlayerCombatController : MonoBehaviour
{
    [Header("Combat")]
    [SerializeField] private int attackDamage = 20;
    [SerializeField] private float attackRange = 1.6f;
    [SerializeField] private float attackRadius = 0.8f;
    [SerializeField] private float attackCooldown = 0.3f;
    [SerializeField] private LayerMask enemyLayers = Physics2D.DefaultRaycastLayers;

    private float attackCooldownTimer;
    private Camera cachedCamera;
    private PlayerTopDownController movementController;

    private void Awake()
    {
        movementController = GetComponent<PlayerTopDownController>();
        cachedCamera = Camera.main;

        if (enemyLayers.value == 0)
        {
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0)
            {
                enemyLayers = 1 << enemyLayer;
            }
        }
    }

    private void Update()
    {
        if (attackCooldownTimer > 0f)
        {
            attackCooldownTimer -= Time.deltaTime;
        }

        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
        {
            return;
        }

        if (attackCooldownTimer > 0f)
        {
            return;
        }

        Vector2 attackDirection = ResolveAttackDirection();
        bool attackAccepted = movementController == null || movementController.TryStartAttack();
        if (!attackAccepted)
        {
            return;
        }

        attackCooldownTimer = Mathf.Max(0.05f, attackCooldown);
        ProcessAttackHit(attackDirection);
    }

    private Vector2 ResolveAttackDirection()
    {
        if (cachedCamera == null)
        {
            cachedCamera = Camera.main;
        }

        if (cachedCamera == null || Mouse.current == null)
        {
            return movementController != null ? movementController.LookDirection : Vector2.down;
        }

        Vector3 mouseWorld = cachedCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        mouseWorld.z = transform.position.z;
        Vector2 direction = mouseWorld - transform.position;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = movementController != null ? movementController.LookDirection : Vector2.down;
        }

        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = Vector2.down;
        }

        return direction.normalized;
    }

    private void ProcessAttackHit(Vector2 attackDirection)
    {
        Vector2 hitCenter = (Vector2)transform.position + attackDirection * Mathf.Max(0f, attackRange);
        
        // Also hit resources
        Collider2D[] resourceHits = Physics2D.OverlapCircleAll(hitCenter, Mathf.Max(0.05f, attackRadius));
        var gatherer = GetComponent<ResourceGatherer>();
        if (gatherer != null)
        {
            foreach (var hit in resourceHits)
            {
                gatherer.OnHitResource(hit.gameObject);
            }
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(hitCenter, Mathf.Max(0.05f, attackRadius), enemyLayers);
Debug.Log($"[PlayerCombat] hitsFound={hits.Length}", this);

        if (hits.Length == 0)
        {
            Collider2D[] allHits = Physics2D.OverlapCircleAll(hitCenter, Mathf.Max(0.05f, attackRadius));
            Debug.Log($"[PlayerCombat] allLayersHitsFound={allHits.Length} (no mask)", this);
            for (int j = 0; j < allHits.Length && j < 3; j++)
            {
                Debug.Log($"[PlayerCombat]   hit[{j}]: '{allHits[j].gameObject.name}' layer={allHits[j].gameObject.layer} ({LayerMask.LayerToName(allHits[j].gameObject.layer)})", this);
            }
        }

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null || hit.gameObject == gameObject)
            {
                continue;
            }

            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable == null)
            {
                damageable = hit.GetComponentInParent<IDamageable>();
            }

            if (damageable != null)
            {
                damageable.TakeDamage(attackDamage);
            }
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector2 direction = movementController != null ? movementController.LookDirection : Vector2.down;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = Vector2.down;
        }

        Vector3 center = transform.position + (Vector3)(direction.normalized * Mathf.Max(0f, attackRange));
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, Mathf.Max(0.05f, attackRadius));
    }
#endif
}
