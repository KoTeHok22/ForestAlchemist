using UnityEngine;

/// <summary>Destroys the host GameObject after a delay (used for one-shot spell VFX).</summary>
public sealed class SpellVfxLifetime : MonoBehaviour
{
    [SerializeField] private float lifetime = 1f;

    public void Configure(float seconds)
    {
        lifetime = Mathf.Max(0.05f, seconds);
    }

    private void Update()
    {
        lifetime -= Time.deltaTime;
        if (lifetime <= 0f)
        {
            Destroy(gameObject);
        }
    }
}
