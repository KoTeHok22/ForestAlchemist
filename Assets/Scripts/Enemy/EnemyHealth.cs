using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyHealth : MonoBehaviour, IDamageable
{
    public int CurrentHealth { get; private set; }
    public int MaxHealth { get; private set; }
    public bool IsAlive => CurrentHealth > 0;

    public event Action<int> OnDamaged;
    public event Action OnDeath;

    public void Initialize(int maxHealth)
    {
        MaxHealth = Mathf.Max(1, maxHealth);
        CurrentHealth = MaxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (!IsAlive || amount <= 0)
        {
            return;
        }

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        OnDamaged?.Invoke(CurrentHealth);
        AudioHooks.SfxAtPoint(AudioClipId.SfxEnemyOrcHit, transform.position);

        if (CurrentHealth <= 0)
        {
            OnDeath?.Invoke();
        }
    }
}
