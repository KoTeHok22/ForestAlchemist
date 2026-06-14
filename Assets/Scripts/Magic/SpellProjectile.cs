using UnityEngine;

/// <summary>
/// Particle-only spell projectile. Always flies along its direction; the player is
/// ignored for both collision and damage.
/// </summary>
public sealed class SpellProjectile : MonoBehaviour
{
    private const float MinTravelBeforeHit = 0.2f;

    private int enemyMask;

    private SpellDefinition spell;
    private Vector2 direction;
    private float lifetime;
    private float maxRange;
    private float hitRadius;
    private float moveSpeed;
    private float distanceTraveled;
    private Vector2 position;
    private Transform caster;
    private PlayerSpellCaster spellCaster;
    private bool impactPlayed;
    private int frameIndex;
    private int instanceId;

    public void Initialize(SpellDefinition spellDef, Vector2 dir, Transform casterTransform)
    {
        instanceId = GetInstanceID();
        spell = spellDef;
        direction = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.down;
        caster = casterTransform;
        spellCaster = casterTransform != null ? casterTransform.GetComponent<PlayerSpellCaster>() : null;
        lifetime = spell.projectileLifetime > 0f ? spell.projectileLifetime : 3f;
        maxRange = Mathf.Max(4f, spell.range);
        hitRadius = SpellVfxLibrary.GetProjectileHitRadius(spell);
        moveSpeed = Mathf.Max(12f, spell.speed);
        distanceTraveled = 0f;
        frameIndex = 0;
        enemyMask = LayerMask.GetMask("Enemy");

        Vector2 casterPos = caster != null ? (Vector2)caster.position : Vector2.zero;
        position = SpellCombatFilters.ResolveProjectileSpawn(caster, direction);
        transform.position = new Vector3(position.x, position.y, 0f);

        int childCountBefore = transform.childCount;
        SpellVfxLibrary.BuildProjectileVisual(transform, spell.element, spell.elementColor, spell);
        SpellVfxLibrary.PlayCastBurst(position, spell.element, spell.elementColor);

        SpellProjectileDebug.Log(
            $"INIT id={instanceId} spell={spell.spellId} " +
            $"caster={(caster != null ? caster.name : "null")} casterPos={casterPos} " +
            $"spawn={position} dir={direction} speed={moveSpeed} range={maxRange} " +
            $"lifetime={lifetime} hitR={hitRadius} enemyMask={enemyMask} " +
            $"timeScale={Time.timeScale} scene={gameObject.scene.name} " +
            $"vfxChildren={transform.childCount} (before={childCountBefore}) active={gameObject.activeInHierarchy}",
            this);

        LogParticleState("after INIT");
    }

    private void OnEnable()
    {
        SpellProjectileDebug.Log($"OnEnable id={instanceId} name={name} scene={gameObject.scene.name}", this);
    }

    private void OnDisable()
    {
        SpellProjectileDebug.Log($"OnDisable id={instanceId} traveled={distanceTraveled:F2} impactPlayed={impactPlayed}", this);
    }

    private void OnDestroy()
    {
        SpellProjectileDebug.Log(
            $"OnDestroy id={instanceId} traveled={distanceTraveled:F2} pos={position} impactPlayed={impactPlayed}",
            this);
    }

    private void Update()
    {
        frameIndex++;
        float dt = Time.deltaTime;

        if (dt <= 0f)
        {
            if (frameIndex == 1 || frameIndex % 60 == 0)
            {
                SpellProjectileDebug.LogWarning(
                    $"STALLED id={instanceId} frame={frameIndex} dt={dt} timeScale={Time.timeScale} — game paused?",
                    this);
            }
            return;
        }

        lifetime -= dt;

        Vector2 previous = position;
        position += direction * moveSpeed * dt;
        float step = Vector2.Distance(previous, position);
        distanceTraveled += step;
        transform.position = new Vector3(position.x, position.y, 0f);

        if (frameIndex == 1 || frameIndex % SpellProjectileDebug.PositionLogIntervalFrames == 0)
        {
            float distToCaster = caster != null ? Vector2.Distance(position, caster.position) : -1f;
            SpellProjectileDebug.Log(
                $"TICK id={instanceId} frame={frameIndex} pos={position} step={step:F3} " +
                $"traveled={distanceTraveled:F2}/{maxRange:F1} lifetime={lifetime:F2} " +
                $"distToCaster={distToCaster:F2} tfPos={transform.position}",
                this);
        }

        if (distanceTraveled >= MinTravelBeforeHit)
        {
            TryHitAtCurrentPosition();
        }

        if (impactPlayed)
        {
            return;
        }

        if (distanceTraveled >= maxRange)
        {
            SpellProjectileDebug.Log(
                $"DESPAWN range id={instanceId} traveled={distanceTraveled:F2} pos={position}",
                this);
            DespawnWithoutImpact();
            return;
        }

        if (lifetime <= 0f)
        {
            SpellProjectileDebug.Log(
                $"DESPAWN lifetime id={instanceId} traveled={distanceTraveled:F2} pos={position}",
                this);
            DespawnWithoutImpact();
        }
    }

    private void TryHitAtCurrentPosition()
    {
        if (impactPlayed)
        {
            return;
        }

        if (enemyMask == 0)
        {
            if (frameIndex % 30 == 0)
            {
                SpellProjectileDebug.LogWarning($"HIT skip id={instanceId} enemyMask=0 (layer missing?)", this);
            }
            return;
        }

        if (!SpellCombatFilters.CanRegisterHit(position, caster))
        {
            if (frameIndex % 30 == 0)
            {
                float dist = caster != null ? Vector2.Distance(position, caster.position) : -1f;
                SpellProjectileDebug.Log(
                    $"HIT skip id={instanceId} too close to caster dist={dist:F2}",
                    this);
            }
            return;
        }

        Collider2D[] overlaps = Physics2D.OverlapCircleAll(position, hitRadius, enemyMask);
        if (overlaps.Length == 0)
        {
            return;
        }

        SpellProjectileDebug.Log(
            $"HIT scan id={instanceId} pos={position} overlaps={overlaps.Length} radius={hitRadius}",
            this);

        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider2D collider = overlaps[i];
            if (collider == null)
            {
                continue;
            }

            bool isEnemy = SpellCombatFilters.IsEnemyCollider(collider);
            SpellProjectileDebug.Log(
                $"  overlap[{i}] obj={collider.name} layer={LayerMask.LayerToName(collider.gameObject.layer)} " +
                $"isEnemy={isEnemy} colPos={collider.transform.position}",
                this);

            if (!isEnemy)
            {
                continue;
            }

            ApplyDamageToCollider(collider);
            SpellProjectileDebug.Log(
                $"IMPACT id={instanceId} target={collider.name} pos={position} traveled={distanceTraveled:F2}",
                this);
            PlayImpactAndDestroy();
            return;
        }
    }

    private void ApplyDamageToCollider(Collider2D collider)
    {
        IDamageable damageable = collider.GetComponent<IDamageable>();
        if (damageable == null) damageable = collider.GetComponentInParent<IDamageable>();
        if (damageable == null)
        {
            SpellProjectileDebug.LogWarning($"DAMAGE skip id={instanceId} no IDamageable on {collider.name}", this);
            return;
        }

        ElementType targetElement = ElementType.None;
        EnemyController enemy = collider.GetComponent<EnemyController>();
        if (enemy == null) enemy = collider.GetComponentInParent<EnemyController>();
        if (enemy != null && enemy.Config != null) targetElement = enemy.Config.element;

        float multiplier = spell.element.GetMultiplierAgainst(targetElement);
        int rawDamage = Mathf.RoundToInt(spell.damage * multiplier);
        int finalDamage = spellCaster != null ? spellCaster.ScaleSpellDamage(rawDamage) : rawDamage;
        damageable.TakeDamage(finalDamage);

        SpellProjectileDebug.Log($"DAMAGE id={instanceId} {finalDamage} to {collider.name}", this);

        if (spell.radius > 0.5f)
        {
            ApplySplashDamage(position, finalDamage);
        }
    }

    private void ApplySplashDamage(Vector2 center, int primaryDamage)
    {
        if (enemyMask == 0) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(center, spell.radius, enemyMask);
        for (int i = 0; i < hits.Length; i++)
        {
            if (!SpellCombatFilters.IsEnemyCollider(hits[i]))
            {
                continue;
            }

            IDamageable splashTarget = hits[i].GetComponent<IDamageable>();
            if (splashTarget == null) splashTarget = hits[i].GetComponentInParent<IDamageable>();
            if (splashTarget == null) continue;

            ElementType targetElement = ElementType.None;
            EnemyController enemy = hits[i].GetComponent<EnemyController>();
            if (enemy == null) enemy = hits[i].GetComponentInParent<EnemyController>();
            if (enemy != null && enemy.Config != null) targetElement = enemy.Config.element;

            float multiplier = spell.element.GetMultiplierAgainst(targetElement);
            int splashDamage = Mathf.RoundToInt(primaryDamage * 0.45f * multiplier);
            if (splashDamage > 0)
            {
                splashTarget.TakeDamage(splashDamage);
            }
        }
    }

    private void PlayImpactAndDestroy()
    {
        if (impactPlayed) return;
        impactPlayed = true;

        float vfxScale = SpellVfxLibrary.GetVfxIntensity(spell);
        SpellVfxLibrary.PlayImpact(position, spell.element, spell.elementColor, vfxScale);
        if (spell != null)
        {
            AudioHooks.Bridge?.PlaySpellImpact(spell.spellId);
        }

        SpellProjectileDebug.Log($"DESTROY impact id={instanceId} pos={position}", this);
        Destroy(gameObject);
    }

    private void DespawnWithoutImpact()
    {
        if (impactPlayed) return;
        impactPlayed = true;
        SpellProjectileDebug.Log($"DESTROY silent id={instanceId} pos={position} traveled={distanceTraveled:F2}", this);
        Destroy(gameObject);
    }

    private void LogParticleState(string phase)
    {
        ParticleSystem[] systems = GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < systems.Length; i++)
        {
            ParticleSystem ps = systems[i];
            if (ps == null) continue;
            var main = ps.main;
            SpellProjectileDebug.Log(
                $"VFX {phase} id={instanceId} ps={ps.name} playing={ps.isPlaying} " +
                $"particles={ps.particleCount} space={main.simulationSpace} " +
                $"maxSize={main.startSize.constantMax}",
                this);
        }

        if (systems.Length == 0)
        {
            SpellProjectileDebug.LogWarning($"VFX {phase} id={instanceId} NO ParticleSystem children!", this);
        }
    }
}
