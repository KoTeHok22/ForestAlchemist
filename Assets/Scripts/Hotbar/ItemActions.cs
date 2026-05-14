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
        ExpeditionManager.Instance.EndExpedition(ExpeditionResult.Success);
    }
}

public sealed class HealthPotionAction : IItemAction
{
    public void Execute()
    {
        PlayerHealth health = Object.FindFirstObjectByType<PlayerHealth>();
        if (health != null) health.Heal(30);
    }
}

public sealed class ManaPotionAction : IItemAction
{
    public void Execute()
    {
        PlayerSpellCaster caster = Object.FindFirstObjectByType<PlayerSpellCaster>();
        if (caster != null) caster.RestoreMana(40);
    }
}

public sealed class ShieldScrollAction : IItemAction
{
    public void Execute()
    {
        PlayerBuffReceiver buffReceiver = Object.FindFirstObjectByType<PlayerBuffReceiver>();
        if (buffReceiver == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) buffReceiver = player.AddComponent<PlayerBuffReceiver>();
        }
        if (buffReceiver != null) buffReceiver.ApplyShield(50, 8f);
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
            case ItemCatalog.HealthPotion: return new HealthPotionAction();
            case ItemCatalog.ManaPotion: return new ManaPotionAction();
            case ItemCatalog.ShieldScroll: return new ShieldScrollAction();
            default: return null;
        }
    }

    public static bool IsConsumable(string itemId)
    {
        itemId = ItemCatalog.Normalize(itemId);
        return itemId == ItemCatalog.ReturnScroll || itemId == ItemCatalog.HealthPotion || itemId == ItemCatalog.ManaPotion || itemId == ItemCatalog.ShieldScroll;
    }

    public static bool IsSpell(string itemId)
    {
        return itemId != null && itemId.StartsWith("spell_");
    }
}
