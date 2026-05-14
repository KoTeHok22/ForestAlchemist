using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public sealed class ShopUI : MonoBehaviour
{
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private Transform itemsContainer;
    [SerializeField] private GameObject itemButtonTemplate;
    [SerializeField] private TMP_Text bloodCountText;
    [SerializeField] private Button closeButton;

    private void Awake()
    {
        if (shopPanel != null) shopPanel.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (itemButtonTemplate != null) itemButtonTemplate.SetActive(false);

        ShopService.Instance.OnItemPurchased += Refresh;
    }

    private void OnDestroy()
    {
        if (ShopService.Instance != null)
            ShopService.Instance.OnItemPurchased -= Refresh;
    }

    public void Open()
    {
        if (itemsContainer == null || itemButtonTemplate == null)
        {
            return;
        }

        if (shopPanel != null) shopPanel.SetActive(true);
        Refresh(null);
    }

    public void Close()
    {
        if (shopPanel != null) shopPanel.SetActive(false);
    }

    private void Refresh(string _)
    {
        UpdateBloodCount();
        PopulateItems();
    }

    private void UpdateBloodCount()
    {
        if (bloodCountText == null) return;
        var homeStorage = InventoryService.Instance.HomeStorage;
        int blood = homeStorage != null ? homeStorage.GetItemCount("КровьОрка") : 0;
        bloodCountText.text = $"Кровь Орка: {blood}";
    }

    private void PopulateItems()
    {
        ClearContainer();

        List<ShopService.ShopItem> items = ShopService.Instance.GetAvailableItems();
        for (int i = 0; i < items.Count; i++)
        {
            ShopService.ShopItem shopItem = items[i];
            GameObject btn = Instantiate(itemButtonTemplate, itemsContainer);
            btn.SetActive(true);
            btn.name = $"ShopItem_{shopItem.itemId}";

            TMP_Text label = btn.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = $"{shopItem.displayName} ({shopItem.priceInBlood} крови)";

            Button button = btn.GetComponent<Button>();
            if (button != null)
            {
                string capturedId = shopItem.itemId;
                button.onClick.AddListener(() => OnPurchaseClicked(capturedId));
            }
        }
    }

    private void OnPurchaseClicked(string itemId)
    {
        ShopService.Instance.TryPurchase(itemId);
    }

    private void ClearContainer()
    {
        if (itemsContainer == null) return;
        for (int i = itemsContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = itemsContainer.GetChild(i);
            if (child.gameObject == itemButtonTemplate) continue;
            Destroy(child.gameObject);
        }
    }
}
