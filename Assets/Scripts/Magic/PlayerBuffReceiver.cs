using UnityEngine;

public sealed class PlayerBuffReceiver : MonoBehaviour
{
    private float shieldAmount;
    private float shieldDuration;
    private float shieldTimer;
    private bool shieldActive;

    public bool HasShield => shieldActive && shieldAmount > 0;
    public float ShieldNormalized => shieldDuration > 0f ? shieldTimer / shieldDuration : 0f;

    public event System.Action<float> OnShieldChanged;
    public event System.Action OnShieldBroken;

    private void Update()
    {
        if (!shieldActive) return;

        shieldTimer -= Time.deltaTime;
        if (shieldTimer <= 0f)
        {
            shieldActive = false;
            shieldAmount = 0f;
            AudioHooks.Sfx(AudioClipId.SfxShieldBreak);
            OnShieldBroken?.Invoke();
            OnShieldChanged?.Invoke(0f);
        }
    }

    public void ApplyShield(int amount, float duration)
    {
        shieldAmount = amount;
        shieldDuration = duration;
        shieldTimer = duration;
        shieldActive = true;
        AudioHooks.Sfx(AudioClipId.SfxShieldApply);
        OnShieldChanged?.Invoke(1f);
    }

    public int AbsorbDamage(int incomingDamage)
    {
        if (!shieldActive || shieldAmount <= 0) return incomingDamage;

        float absorbed = Mathf.Min(shieldAmount, incomingDamage);
        shieldAmount -= absorbed;
        int remaining = Mathf.RoundToInt(incomingDamage - absorbed);
        if (absorbed > 0f)
        {
            AudioHooks.Sfx(AudioClipId.SfxShieldHitAbsorb);
        }

        OnShieldChanged?.Invoke(shieldDuration > 0f ? shieldTimer / shieldDuration : 0f);

        if (shieldAmount <= 0)
        {
            shieldActive = false;
            AudioHooks.Sfx(AudioClipId.SfxShieldBreak);
            OnShieldBroken?.Invoke();
        }

        return remaining;
    }
}
