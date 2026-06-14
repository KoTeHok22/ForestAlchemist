using System.Collections.Generic;

/// <summary>
/// Keeps serialized progress slot lists aligned with live PlayerInventory state.
/// </summary>
public static class InventoryProgressSync
{
    public static List<InventorySlot> CloneSlots(IEnumerable<InventorySlot> source)
    {
        List<InventorySlot> clone = new List<InventorySlot>();
        if (source == null)
        {
            return clone;
        }

        foreach (InventorySlot slot in source)
        {
            if (slot == null || string.IsNullOrEmpty(slot.itemName) || slot.count <= 0)
            {
                continue;
            }

            clone.Add(new InventorySlot
            {
                itemName = slot.itemName,
                count = slot.count
            });
        }

        return clone;
    }

    public static void WriteToProgress(PlayerInventory inventory, HomeStorageData target)
    {
        if (inventory == null || target == null)
        {
            return;
        }

        List<InventorySlot> next = CloneSlots(inventory.GetAllSlots());
        target.slots = next;
        ExpeditionItemTrace.Log(
            "ProgressSync.Write",
            $"target={(target == null ? "NULL" : "HomeStorageData")} wrote={DescribeSlotList(next)}");
    }

    private static string DescribeSlotList(List<InventorySlot> slots)
    {
        if (slots == null || slots.Count == 0)
        {
            return "(empty)";
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlot slot = slots[i];
            if (slot == null || string.IsNullOrEmpty(slot.itemName) || slot.count <= 0)
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append(", ");
            }

            builder.Append(ItemCatalog.Normalize(slot.itemName));
            builder.Append('=');
            builder.Append(slot.count);
        }

        return builder.Length > 0 ? builder.ToString() : "(empty)";
    }
}
