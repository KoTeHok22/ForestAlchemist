using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public sealed class InventoryDisplay : MonoBehaviour
{
    [SerializeField] private Transform abilitiesContainer;
    [SerializeField] private Sprite emptySlotSprite;

    private PlayerInventory inventory;
    private Image[] slotImages;
    private TMP_Text[] slotTexts;
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
            slotImages[i].sprite = emptySlotSprite;

            if (slotTexts != null && i < slotTexts.Length && slotTexts[i] != null)
            {
                slotTexts[i].text = string.Empty;
            }
        }

        List<InventorySlot> slots = inventory.GetAllSlots();
        int index = 0;

        foreach (InventorySlot slot in slots)
        {
            if (index >= slotImages.Length)
            {
                break;
            }

            Sprite icon = iconProvider?.GetIcon(slot.itemName);
            slotImages[index].sprite = icon ?? emptySlotSprite;

            Color color = slotImages[index].color;
            color.a = 1f;
            slotImages[index].color = color;

            if (slotTexts != null && index < slotTexts.Length && slotTexts[index] != null)
            {
                slotTexts[index].text = slot.count > 1 ? slot.count.ToString() : slot.itemName;
            }

            index++;
        }
    }

    private void CacheSlots()
    {
        if (abilitiesContainer == null)
            return;

        int count = abilitiesContainer.childCount;
        slotImages = new Image[count];
        slotTexts = new TMP_Text[count];
        for (int i = 0; i < count; i++)
        {
            Transform child = abilitiesContainer.GetChild(i);
            slotImages[i] = child.GetComponent<Image>();
            slotTexts[i] = child.GetComponentInChildren<TMP_Text>(true);
        }
    }
}
