using UnityEngine;

/// <summary>
/// Spell projectiles ignore the player completely. Spawn is placed in front of the
/// caster body; only enemy colliders count as valid targets.
/// </summary>
public static class SpellCombatFilters
{
    private const float MinSpawnPadding = 0.2f;
    private const float FallbackSpawnDistance = 0.55f;
    private const float MinHitDistanceFromCaster = 0.45f;

    public static bool IsPlayerCollider(Collider2D collider)
    {
        if (collider == null)
        {
            return false;
        }

        if (collider.CompareTag("Player"))
        {
            return true;
        }

        if (collider.GetComponent<PlayerHealth>() != null)
        {
            return true;
        }

        if (collider.GetComponentInParent<PlayerHealth>() != null)
        {
            return true;
        }

        return false;
    }

    public static bool IsEnemyCollider(Collider2D collider)
    {
        if (collider == null || IsPlayerCollider(collider))
        {
            return false;
        }

        if (collider.GetComponent<EnemyController>() != null)
        {
            return true;
        }

        return collider.GetComponentInParent<EnemyController>() != null;
    }

    public static bool CanRegisterHit(Vector2 hitPoint, Transform caster)
    {
        if (caster == null)
        {
            return true;
        }

        return Vector2.Distance(hitPoint, caster.position) >= MinHitDistanceFromCaster;
    }

    public static Vector2 ResolveProjectileSpawn(Transform caster, Vector2 direction)
    {
        Vector2 normalized = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.down;
        Vector2 origin = caster != null ? (Vector2)caster.position : Vector2.zero;
        float bodyRadius = GetBodyRadius(caster);
        float distance = bodyRadius + MinSpawnPadding;
        float usedDistance = Mathf.Max(distance, FallbackSpawnDistance);
        Vector2 spawn = origin + normalized * usedDistance;

        SpellProjectileDebug.Log(
            $"SpawnResolve caster={(caster != null ? caster.name : "null")} origin={origin} " +
            $"bodyR={bodyRadius:F2} dist={usedDistance:F2} spawn={spawn}");

        return spawn;
    }

    private static float GetBodyRadius(Transform caster)
    {
        if (caster == null)
        {
            return FallbackSpawnDistance;
        }

        bool found = false;
        Bounds bounds = new Bounds(caster.position, Vector3.zero);
        Collider2D[] colliders = caster.GetComponentsInChildren<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D collider = colliders[i];
            if (collider == null || !collider.enabled || collider.isTrigger)
            {
                continue;
            }

            if (!found)
            {
                bounds = collider.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(collider.bounds);
            }
        }

        if (!found)
        {
            return FallbackSpawnDistance;
        }

        return Mathf.Max(bounds.extents.x, bounds.extents.y);
    }
}
