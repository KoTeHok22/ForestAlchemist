using System;
using UnityEngine;

public sealed class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float regenPerSecond = 2f;
    [SerializeField] private float regenDelay = 3f;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public bool IsAlive => CurrentHealth > 0;

    public event Action<int> OnDamaged;
    public event Action OnDeath;
    public event Action<int, int> OnHealthChanged;

    private float timeSinceLastDamage;
    private float regenAccumulator;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    private void Update()
    {
        if (!IsAlive)
            return;

        timeSinceLastDamage += Time.deltaTime;

        if (timeSinceLastDamage >= regenDelay && CurrentHealth < maxHealth)
        {
            regenAccumulator += regenPerSecond * Time.deltaTime;
            int regenAmount = Mathf.FloorToInt(regenAccumulator);
            if (regenAmount > 0)
            {
                regenAccumulator -= regenAmount;
                CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + regenAmount);
                OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
            }
        }
    }

    public void TakeDamage(int amount)
    {
        if (!IsAlive || amount <= 0)
            return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        timeSinceLastDamage = 0f;
        regenAccumulator = 0f;

        OnDamaged?.Invoke(CurrentHealth);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (CurrentHealth <= 0)
            OnDeath?.Invoke();
    }
}
