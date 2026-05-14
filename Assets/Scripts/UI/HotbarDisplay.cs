using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public sealed class HotbarDisplay : MonoBehaviour
{
    [SerializeField] private Image[] slotIcons;
    [SerializeField] private Image[] cooldownOverlays;
    [SerializeField] private TMP_Text[] slotLabels;
    [SerializeField] private Sprite emptySlotSprite;

    private void Start()
    {
        RefreshAll();
    }

    private void Update()
    {
        UpdateCooldowns();
    }

    private void OnEnable()
    {
        HotbarManager.Instance.OnSlotUsed += OnSlotUsed;
        HotbarManager.Instance.OnSlotCooldownStarted += OnCooldownStarted;
        HotbarManager.Instance.OnSlotCooldownFinished += OnCooldownFinished;
    }

    private void OnDisable()
    {
        if (HotbarManager.Instance != null)
        {
            HotbarManager.Instance.OnSlotUsed -= OnSlotUsed;
            HotbarManager.Instance.OnSlotCooldownStarted -= OnCooldownStarted;
            HotbarManager.Instance.OnSlotCooldownFinished -= OnCooldownFinished;
        }
    }

    private void RefreshAll()
    {
        for (int i = 0; i < HotbarManager.SlotCount; i++)
        {
            RefreshSlot(i);
        }
    }

    private void RefreshSlot(int index)
    {
        if (index < 0 || index >= HotbarManager.SlotCount) return;

        string itemId = HotbarManager.Instance.GetSlotItem(index);

        if (slotIcons != null && index < slotIcons.Length && slotIcons[index] != null)
        {
            Sprite icon = GetItemIcon(itemId);
            slotIcons[index].sprite = icon ?? emptySlotSprite;
            slotIcons[index].enabled = true;
        }

        if (slotLabels != null && index < slotLabels.Length && slotLabels[index] != null)
        {
            slotLabels[index].text = itemId != null && !string.IsNullOrEmpty(itemId) ? ((index + 1) % 10).ToString() : string.Empty;
        }
    }

    private Sprite GetItemIcon(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return null;

        Sprite icon = Resources.Load<Sprite>($"Game/Icons/{itemId}");
        return icon;
    }

    private void UpdateCooldowns()
    {
        if (cooldownOverlays == null) return;

        for (int i = 0; i < cooldownOverlays.Length; i++)
        {
            if (cooldownOverlays[i] == null) continue;

            if (HotbarManager.Instance.IsOnCooldown(i))
            {
                cooldownOverlays[i].fillAmount = 1f - HotbarManager.Instance.GetCooldownProgress(i);
                cooldownOverlays[i].gameObject.SetActive(true);
            }
            else
            {
                cooldownOverlays[i].fillAmount = 0f;
                cooldownOverlays[i].gameObject.SetActive(false);
            }
        }
    }

    private void OnSlotUsed(int index)
    {
        RefreshSlot(index);
    }

    private void OnCooldownStarted(int index, float duration)
    {
        if (cooldownOverlays != null && index < cooldownOverlays.Length && cooldownOverlays[index] != null)
        {
            cooldownOverlays[index].gameObject.SetActive(true);
        }
    }

    private void OnCooldownFinished(int index)
    {
        if (cooldownOverlays != null && index < cooldownOverlays.Length && cooldownOverlays[index] != null)
        {
            cooldownOverlays[index].gameObject.SetActive(false);
        }
    }
}
