using System;

public interface IDamageable
{
    int CurrentHealth { get; }
    int MaxHealth { get; }
    bool IsAlive { get; }

    event Action<int> OnDamaged;
    event Action OnDeath;

    void TakeDamage(int amount);
}
