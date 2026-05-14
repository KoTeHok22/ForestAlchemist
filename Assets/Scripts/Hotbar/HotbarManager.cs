using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public sealed class HotbarManager : MonoBehaviour
{
    private static HotbarManager instance;
    public static HotbarManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("HotbarManager");
                instance = go.AddComponent<HotbarManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    public const int SlotCount = 10;

    public event System.Action<int> OnSlotUsed;
    public event System.Action<int, float> OnSlotCooldownStarted;
    public event System.Action<int> OnSlotCooldownFinished;

    private Dictionary<int, float> slotCooldowns = new Dictionary<int, float>();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        for (int i = 0; i < 9; i++)
        {
            if (Keyboard.current[GetDigitKey(i)].wasPressedThisFrame)
            {
                UseSlot(i);
            }
        }

        if (Keyboard.current[Key.Digit0].wasPressedThisFrame)
        {
            UseSlot(9);
        }

        TickCooldowns(Time.deltaTime);
    }

    public void UseSlot(int index)
    {
        if (index < 0 || index >= SlotCount) return;
        if (IsOnCooldown(index)) return;

        var progress = GameCore.Instance.CurrentProgress;
        if (progress == null || index >= progress.hotbar.slotItemIds.Count) return;

        string itemId = progress.hotbar.slotItemIds[index];
        if (string.IsNullOrEmpty(itemId)) return;

        PlayerSpellCaster spellCaster = FindFirstObjectByType<PlayerSpellCaster>();
        if (spellCaster != null)
        {
            SpellDefinition spell = spellCaster.ResolveSpell(itemId);
            if (spell != null)
            {
                if (spellCaster.TryCast(spell))
                {
                    StartCooldown(index, spell.cooldown);
                    OnSlotUsed?.Invoke(index);
                    return;
                }
            }
        }

        IItemAction action = ItemActionRegistry.GetAction(itemId);
        if (action != null)
        {
            if (ExpeditionManager.Instance.IsInExpedition)
            {
                if (ItemActionRegistry.IsConsumable(itemId))
                {
                    if (ExpeditionManager.Instance.ExpeditionInventory.RemoveItem(itemId, 1))
                    {
                        action.Execute();
                        StartCooldown(index, itemId == "return_scroll" ? 5f : 2f);
                        OnSlotUsed?.Invoke(index);
                    }
                }
                else
                {
                    action.Execute();
                    OnSlotUsed?.Invoke(index);
                }
            }
        }
    }

    public void SetSlotItem(int index, string itemId)
    {
        var progress = GameCore.Instance.CurrentProgress;
        if (progress == null) return;

        while (progress.hotbar.slotItemIds.Count <= index)
            progress.hotbar.slotItemIds.Add(string.Empty);

        progress.hotbar.slotItemIds[index] = itemId;
        GameCore.Instance.SaveProgress();
    }

    public string GetSlotItem(int index)
    {
        var progress = GameCore.Instance.CurrentProgress;
        if (progress == null || index < 0 || index >= progress.hotbar.slotItemIds.Count) return string.Empty;
        return progress.hotbar.slotItemIds[index];
    }

    public bool IsOnCooldown(int index)
    {
        float remaining;
        return slotCooldowns.TryGetValue(index, out remaining) && remaining > 0f;
    }

    public float GetCooldownRemaining(int index)
    {
        float remaining;
        if (slotCooldowns.TryGetValue(index, out remaining)) return Mathf.Max(0f, remaining);
        return 0f;
    }

    public float GetCooldownProgress(int index)
    {
        if (!slotCooldowns.ContainsKey(index)) return 1f;
        float remaining = slotCooldowns[index];
        if (remaining <= 0f) return 1f;

        string itemId = GetSlotItem(index);
        PlayerSpellCaster spellCaster = FindFirstObjectByType<PlayerSpellCaster>();
        if (spellCaster != null)
        {
            SpellDefinition spell = spellCaster.ResolveSpell(itemId);
            if (spell != null && spell.cooldown > 0f)
            {
                return 1f - Mathf.Clamp01(remaining / spell.cooldown);
            }
        }

        return 0f;
    }

    private void StartCooldown(int index, float duration)
    {
        slotCooldowns[index] = duration;
        OnSlotCooldownStarted?.Invoke(index, duration);
    }

    private void TickCooldowns(float deltaTime)
    {
        List<int> keys = new List<int>(slotCooldowns.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            float remaining = slotCooldowns[keys[i]] - deltaTime;
            if (remaining <= 0f)
            {
                slotCooldowns.Remove(keys[i]);
                OnSlotCooldownFinished?.Invoke(keys[i]);
            }
            else
            {
                slotCooldowns[keys[i]] = remaining;
            }
        }
    }

    private Key GetDigitKey(int index)
    {
        return (Key)((int)Key.Digit1 + index);
    }
}
