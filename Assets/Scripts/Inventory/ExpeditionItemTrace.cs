using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Debug trace for expedition loot: gather → PlayerInventory → UI refresh.
/// Filter console by "[ItemTrace]".
/// </summary>
public static class ExpeditionItemTrace
{
    public static void Log(string stage, string message)
    {
        Debug.Log($"[ItemTrace][{stage}] {message}");
    }

    public static void LogInventory(string stage, PlayerInventory pack, string extra = null)
    {
        if (pack == null)
        {
            Log(stage, $"inventory=NULL {extra}".Trim());
            return;
        }

        Log(stage, $"inventory label={pack.DebugLabel} hash={pack.GetHashCode()} slots={DescribeSlots(pack)} {extra}".Trim());
    }

    public static string DescribeSlots(PlayerInventory pack)
    {
        if (pack == null)
        {
            return "(null)";
        }

        List<InventorySlot> slots = pack.GetAllSlots();
        if (slots == null || slots.Count == 0)
        {
            return "(empty)";
        }

        StringBuilder builder = new StringBuilder();
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
