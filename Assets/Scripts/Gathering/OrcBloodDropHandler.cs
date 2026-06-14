using UnityEngine;

public sealed class OrcBloodDropHandler : MonoBehaviour
{
    private PlayerInventory inventory;
    private const string BloodItemName = ItemCatalog.OrcBlood;

    public void Initialize(PlayerInventory inventory)
    {
        this.inventory = inventory ?? ExpeditionManager.Instance?.ExpeditionInventory;
    }

    public void HandleEnemyKilled()
    {
        PlayerInventory pack = inventory ?? ExpeditionManager.Instance?.ExpeditionInventory;
        if (pack == null)
        {
            return;
        }

        inventory = pack;
        pack.AddItem(BloodItemName);
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.ReportItemCollected(BloodItemName, 1);
        }
    }
}
