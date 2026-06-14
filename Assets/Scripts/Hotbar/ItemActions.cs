using UnityEngine;

public interface IItemAction
{
    void Execute();
}

public sealed class ReturnScrollAction : IItemAction
{
    public void Execute()
    {
        ExpeditionManager.Instance.TryUnlockReturn(ItemCatalog.ReturnScroll);
        AudioHooks.Bridge?.PlayReturnScroll();
        ExpeditionManager.Instance.EndExpedition(ExpeditionResult.Success);
    }
}

/// <summary>
/// Generic consumable that combines the three player-affecting effects (instant
/// heal, timed shield, mana restore). Used by every potion/scroll/elixir — the
/// numbers are what separate a basic potion from an imba tonic.
/// </summary>
public sealed class BuffConsumableAction : IItemAction
{
    private readonly int heal;
    private readonly int shield;
    private readonly float shieldDuration;
    private readonly int mana;

    public BuffConsumableAction(int heal, int shield, float shieldDuration, int mana)
    {
        this.heal = heal;
        this.shield = shield;
        this.shieldDuration = shieldDuration;
        this.mana = mana;
    }

    public void Execute()
    {
        bool usedScroll = shield > 0 && heal <= 0 && mana <= 0;
        if (usedScroll)
        {
            AudioHooks.Sfx(AudioClipId.SfxBuffScrollUnfurl);
        }
        else
        {
            AudioHooks.Sfx(AudioClipId.SfxBuffConsumableDrink);
        }

        if (heal > 0)
        {
            PlayerHealth health = Object.FindFirstObjectByType<PlayerHealth>();
            if (health != null) health.Heal(heal);
        }

        if (mana > 0)
        {
            PlayerSpellCaster caster = Object.FindFirstObjectByType<PlayerSpellCaster>();
            if (caster != null) caster.RestoreMana(mana);
        }

        if (shield > 0)
        {
            PlayerBuffReceiver buffReceiver = Object.FindFirstObjectByType<PlayerBuffReceiver>();
            if (buffReceiver == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) buffReceiver = player.AddComponent<PlayerBuffReceiver>();
            }
            if (buffReceiver != null) buffReceiver.ApplyShield(shield, shieldDuration);
        }
    }
}

public static class ItemActionRegistry
{
    public static IItemAction GetAction(string itemId)
    {
        itemId = ItemCatalog.Normalize(itemId);
        if (string.IsNullOrEmpty(itemId)) return null;

        switch (itemId)
        {
            case ItemCatalog.ReturnScroll: return new ReturnScrollAction();

            // Базовые расходники
            case ItemCatalog.HealthPotion:        return new BuffConsumableAction(30, 0, 0f, 0);
            case ItemCatalog.ManaPotion:          return new BuffConsumableAction(0, 0, 0f, 40);
            case ItemCatalog.ShieldScroll:        return new BuffConsumableAction(0, 50, 8f, 0);
            case ItemCatalog.StaminaElixir:       return new BuffConsumableAction(20, 0, 0f, 40);

            // Улучшенные расходники
            case ItemCatalog.GreaterHealthPotion: return new BuffConsumableAction(70, 0, 0f, 0);
            case ItemCatalog.GreaterManaPotion:   return new BuffConsumableAction(0, 0, 0f, 90);
            case ItemCatalog.EnhancedShieldScroll:return new BuffConsumableAction(0, 100, 12f, 0);
            case ItemCatalog.EarthAmulet:         return new BuffConsumableAction(0, 80, 20f, 0);

            // Имбовые расходники из покупных трофеев
            case ItemCatalog.LifebloomElixir:     return new BuffConsumableAction(140, 0, 0f, 50);
            case ItemCatalog.ShamanWard:          return new BuffConsumableAction(0, 160, 14f, 70);
            case ItemCatalog.WarchiefBrew:        return new BuffConsumableAction(120, 120, 12f, 60);
            case ItemCatalog.BloodcrownTonic:     return new BuffConsumableAction(150, 220, 16f, 80);

            default: return null;
        }
    }

    public static bool IsConsumable(string itemId)
    {
        // Every non-spell item that has a usable action is a consumable.
        return !IsSpell(itemId) && GetAction(itemId) != null;
    }

    public static bool IsSpell(string itemId)
    {
        return itemId != null && itemId.StartsWith("spell_");
    }
}
