using UnityEngine;

/// <summary>
/// Applies saved upgrade levels to the local player instance.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerStatApplicator : MonoBehaviour
{
    private PlayerTopDownController movement;
    private PlayerCombatController combat;
    private PlayerSpellCaster spellCaster;
    private bool subscribed;

    private void Awake()
    {
        movement = GetComponent<PlayerTopDownController>();
        combat = GetComponent<PlayerCombatController>();
        spellCaster = GetComponent<PlayerSpellCaster>();
    }

    private void OnEnable()
    {
        ApplyFromService();
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (subscribed) return;
        PlayerUpgradeService service = PlayerUpgradeService.Instance;
        if (service == null) return;
        service.OnUpgradesChanged += ApplyFromService;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed) return;
        PlayerUpgradeService service = PlayerUpgradeService.Instance;
        if (service != null)
        {
            service.OnUpgradesChanged -= ApplyFromService;
        }

        subscribed = false;
    }

    public void ApplyFromService()
    {
        PlayerUpgradeService service = PlayerUpgradeService.Instance;
        if (service == null) return;

        if (movement != null)
        {
            movement.ConfigureStamina(service.GetMaxStamina());
            movement.SetSpeedMultiplier(service.GetMoveSpeedMultiplier());
        }

        if (combat != null)
        {
            combat.SetAttackDamage(service.GetMeleeDamage());
        }

        if (spellCaster != null)
        {
            spellCaster.ConfigureMana(service.GetMaxMana());
            spellCaster.SetSpellDamageMultiplier(service.GetSpellDamageMultiplier());
        }
    }
}
