using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class SpellProjectile : MonoBehaviour
{
    private SpellDefinition spell;
    private Vector2 direction;
    private float lifetime;
    private Rigidbody2D rb;

    public void Initialize(SpellDefinition spellDef, Vector2 dir)
    {
        spell = spellDef;
        direction = dir.normalized;
        lifetime = spell.projectileLifetime > 0 ? spell.projectileLifetime : 3f;

        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.freezeRotation = true;
        rb.linearVelocity = direction * Mathf.Max(1f, spell.speed);

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = spell.icon;
        sr.color = spell.elementColor;
        sr.sortingOrder = 5;

        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col == null) col = gameObject.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.2f;

        gameObject.layer = LayerMask.NameToLayer("Default");
    }

    private void Update()
    {
        lifetime -= Time.deltaTime;
        if (lifetime <= 0f) Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject == gameObject) return;

        GameObject root = other.gameObject;
        IDamageable damageable = root.GetComponent<IDamageable>();
        if (damageable == null) damageable = root.GetComponentInParent<IDamageable>();

        if (damageable != null)
        {
            PlayerHealth playerHealth = root.GetComponent<PlayerHealth>();
            if (playerHealth == null) playerHealth = root.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null) { Destroy(gameObject); return; }

            ElementType targetElement = ElementType.None;
            EnemyController enemy = root.GetComponent<EnemyController>();
            if (enemy == null) enemy = root.GetComponentInParent<EnemyController>();
            if (enemy != null && enemy.Config != null) targetElement = enemy.Config.element;

            float multiplier = spell.element.GetMultiplierAgainst(targetElement);
            int finalDamage = Mathf.RoundToInt(spell.damage * multiplier);
            damageable.TakeDamage(finalDamage);
        }

        if (other.gameObject.layer != LayerMask.NameToLayer("Enemy"))
        {
            Destroy(gameObject);
        }
    }
}
