using UnityEngine;

/// <summary>Earth shield aura that follows the caster for a limited time.</summary>
public sealed class SpellVfxShieldAura : MonoBehaviour
{
    private Transform followTarget;
    private float remaining;

    public void Configure(Transform target, float duration)
    {
        followTarget = target;
        remaining = Mathf.Max(0.1f, duration);
    }

    private void LateUpdate()
    {
        if (followTarget != null)
        {
            transform.position = followTarget.position;
        }

        remaining -= Time.deltaTime;
        if (remaining <= 0f)
        {
            Destroy(gameObject);
        }
    }
}
