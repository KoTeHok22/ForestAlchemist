using UnityEngine;

public sealed class OrcBloodDropHandler : MonoBehaviour
{
    private PlayerInventory inventory;
    private const string BloodItemName = ItemCatalog.OrcBlood;

    public void Initialize(PlayerInventory inventory)
    {
        this.inventory = inventory;
    }

    public void HandleEnemyKilled()
    {
        if (inventory != null)
            inventory.AddItem(BloodItemName);
    }
}
