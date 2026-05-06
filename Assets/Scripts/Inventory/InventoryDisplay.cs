using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class InventoryDisplay : MonoBehaviour
{
    [SerializeField] private Transform abilitiesContainer;
    [SerializeField] private Sprite emptySlotSprite;

    private PlayerInventory inventory;
    private Image[] slotImages;
    private IQuestItemIconProvider iconProvider;

    public void Initialize(PlayerInventory inventory, IQuestItemIconProvider iconProvider)
    {
        this.inventory = inventory;
        this.iconProvider = iconProvider;

        CacheSlots();
        inventory.OnInventoryChanged += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        if (inventory != null)
            inventory.OnInventoryChanged -= Refresh;
    }

    public void Refresh()
    {
        if (slotImages == null)
            CacheSlots();

        for (int i = 0; i < slotImages.Length; i++)
        {
            Color color = slotImages[i].color;
            color.a = 0f;
            slotImages[i].color = color;
        }

        List<InventorySlot> slots = inventory.GetAllSlots();
        int index = 0;

        foreach (InventorySlot slot in slots)
        {
            for (int j = 0; j < slot.count && index < slotImages.Length; j++)
            {
                Sprite icon = iconProvider?.GetIcon(slot.itemName);
                if (icon != null)
                {
                    slotImages[index].sprite = icon;
                    Color color = slotImages[index].color;
                    color.a = 1f;
                    slotImages[index].color = color;
                }
                index++;
            }
        }
    }

    private void CacheSlots()
    {
        if (abilitiesContainer == null)
            return;

        int count = abilitiesContainer.childCount;
        slotImages = new Image[count];
        for (int i = 0; i < count; i++)
        {
            slotImages[i] = abilitiesContainer.GetChild(i).GetComponent<Image>();
        }
    }
}
