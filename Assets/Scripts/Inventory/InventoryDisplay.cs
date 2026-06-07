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
    private bool runtimeUiBuilt;

    private void Awake()
    {
        BuildRuntimeUiIfNeeded();
    }

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

    public void Open()
    {
        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    public void Toggle()
    {
        gameObject.SetActive(!gameObject.activeSelf);
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
        {
            BuildRuntimeUiIfNeeded();
        }

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

    private void BuildRuntimeUiIfNeeded()
    {
        if (runtimeUiBuilt || abilitiesContainer != null)
        {
            runtimeUiBuilt = true;
            return;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            return;
        }

        GameObject panel = new GameObject("RuntimeInventory", typeof(RectTransform), typeof(Image), typeof(GridLayoutGroup));
        panel.transform.SetParent(canvas.transform, false);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = new Vector2(18f, 18f);
        rect.sizeDelta = new Vector2(324f, 150f);

        Image background = panel.GetComponent<Image>();
        background.color = new Color(0.08f, 0.1f, 0.12f, 0.85f);

        GridLayoutGroup layout = panel.GetComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(48f, 48f);
        layout.spacing = new Vector2(8f, 8f);
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 5;

        abilitiesContainer = panel.transform;
        for (int i = 0; i < 10; i++)
        {
            CreateRuntimeSlot(abilitiesContainer, i + 1);
        }

        runtimeUiBuilt = true;
    }

    private static void CreateRuntimeSlot(Transform parent, int index)
    {
        GameObject slot = new GameObject($"Slot{index}", typeof(RectTransform), typeof(Image));
        slot.transform.SetParent(parent, false);

        Image image = slot.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0f);

        GameObject label = new GameObject("Count", typeof(RectTransform));
        label.transform.SetParent(slot.transform, false);
        TextMeshProUGUI text = label.AddComponent<TextMeshProUGUI>();
        text.fontSize = 14f;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.BottomRight;

        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(4f, 4f);
        textRect.offsetMax = new Vector2(-4f, -4f);
    }
}
