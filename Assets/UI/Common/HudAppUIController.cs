using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// App UI HUD controller. Recreates the legacy uGUI HUD: round portrait with an
/// expedition-attempt badge, three HP hearts, stamina / mana / shield bars and a
/// 10-slot hotbar. Binds to the same services/events the old display scripts used.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public sealed class HudAppUIController : MonoBehaviour
{
    private UIDocument document;
    private VisualElement root;

    private Label portraitBadge;
    private readonly VisualElement[] hearts = new VisualElement[3];
    private VisualElement staminaFill;
    private VisualElement manaFill;
    private VisualElement shieldFill;
    private VisualElement hotbarContainer;

    private readonly VisualElement[] slotIcons = new VisualElement[HotbarManager.SlotCount];
    private readonly VisualElement[] slotCooldowns = new VisualElement[HotbarManager.SlotCount];

    private PlayerHealth health;
    private PlayerSpellCaster spellCaster;
    private PlayerBuffReceiver buffReceiver;
    private PlayerTopDownController playerController;

    private void OnEnable()
    {
        if (document == null) document = GetComponent<UIDocument>();
        root = document.rootVisualElement;
        if (root == null) return;

        CacheElements();
        BuildHotbar();
        RefreshHotbarAll();
        SubscribeHotbar();
    }

    private void OnDisable()
    {
        UnsubscribeHotbar();
        UnsubscribeStats();
    }

    private void Start()
    {
        ResolvePlayerRefs();
        SubscribeStats();
        RefreshExpeditionBadge();
    }

    private void Update()
    {
        // Player objects may spawn after the HUD; lazily resolve and refresh.
        if (playerController == null || health == null)
        {
            ResolvePlayerRefs();
            SubscribeStats();
        }

        if (playerController != null) SetFill(staminaFill, playerController.StaminaNormalized);
        UpdateCooldowns();
    }

    private void CacheElements()
    {
        portraitBadge = root.Q<Label>("portrait-badge");
        hearts[0] = root.Q<VisualElement>("heart-0");
        hearts[1] = root.Q<VisualElement>("heart-1");
        hearts[2] = root.Q<VisualElement>("heart-2");
        staminaFill = root.Q<VisualElement>("bar-stamina-fill");
        manaFill = root.Q<VisualElement>("bar-mana-fill");
        shieldFill = root.Q<VisualElement>("bar-shield-fill");
        hotbarContainer = root.Q<VisualElement>("hud-hotbar");
    }

    // ---------- Hotbar ----------

    private void BuildHotbar()
    {
        if (hotbarContainer == null) return;
        hotbarContainer.Clear();

        var strip = new VisualElement();
        strip.AddToClassList("hud-hotbar__strip");
        strip.pickingMode = PickingMode.Ignore;
        hotbarContainer.Add(strip);

        for (int i = 0; i < HotbarManager.SlotCount; i++)
        {
            var slot = new VisualElement();
            slot.AddToClassList("hotbar-slot");
            slot.pickingMode = PickingMode.Ignore;

            var icon = new VisualElement();
            icon.AddToClassList("hotbar-slot__icon");
            icon.pickingMode = PickingMode.Ignore;
            slot.Add(icon);
            slotIcons[i] = icon;

            var cd = new VisualElement();
            cd.AddToClassList("hotbar-slot__cooldown");
            cd.pickingMode = PickingMode.Ignore;
            slot.Add(cd);
            slotCooldowns[i] = cd;

            var hotkey = new Label((((i + 1) % 10)).ToString());
            hotkey.AddToClassList("hotbar-slot__hotkey");
            hotkey.pickingMode = PickingMode.Ignore;
            slot.Add(hotkey);

            strip.Add(slot);
        }
    }

    private void RefreshHotbarAll()
    {
        for (int i = 0; i < HotbarManager.SlotCount; i++) RefreshSlot(i);
    }

    private void RefreshSlot(int index)
    {
        if (index < 0 || index >= HotbarManager.SlotCount) return;
        if (slotIcons[index] == null) return;

        string itemId = HotbarManager.Instance.GetSlotItem(index);
        Sprite icon = GetItemIcon(itemId);
        slotIcons[index].style.backgroundImage = icon != null ? new StyleBackground(icon) : new StyleBackground();
    }

    private Sprite GetItemIcon(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return null;

        Sprite icon = Resources.Load<Sprite>($"Game/Icons/{itemId}");
        if (icon != null) return icon;

        if (spellCaster == null) spellCaster = FindFirstObjectByType<PlayerSpellCaster>();
        SpellDefinition spell = spellCaster != null ? spellCaster.ResolveSpell(itemId) : null;
        return spell != null ? spell.icon : null;
    }

    private void UpdateCooldowns()
    {
        for (int i = 0; i < HotbarManager.SlotCount; i++)
        {
            if (slotCooldowns[i] == null) continue;
            if (HotbarManager.Instance.IsOnCooldown(i))
            {
                float remaining = 1f - HotbarManager.Instance.GetCooldownProgress(i);
                slotCooldowns[i].style.height = Length.Percent(Mathf.Clamp01(remaining) * 100f);
            }
            else
            {
                slotCooldowns[i].style.height = Length.Percent(0f);
            }
        }
    }

    private void SubscribeHotbar()
    {
        if (HotbarManager.Instance == null) return;
        HotbarManager.Instance.OnSlotUsed += OnSlotUsed;
    }

    private void UnsubscribeHotbar()
    {
        if (HotbarManager.Instance == null) return;
        HotbarManager.Instance.OnSlotUsed -= OnSlotUsed;
    }

    private void OnSlotUsed(int index) => RefreshSlot(index);

    // ---------- Stats (HP / mana / shield) ----------

    private void ResolvePlayerRefs()
    {
        if (health == null) health = FindFirstObjectByType<PlayerHealth>();
        if (spellCaster == null) spellCaster = FindFirstObjectByType<PlayerSpellCaster>();
        if (buffReceiver == null) buffReceiver = FindFirstObjectByType<PlayerBuffReceiver>();
        if (playerController == null) playerController = FindFirstObjectByType<PlayerTopDownController>();
    }

    private bool statsSubscribed;

    private void SubscribeStats()
    {
        if (statsSubscribed) return;

        if (health != null)
        {
            health.OnHealthChanged += OnHealthChanged;
            OnHealthChanged(health.CurrentHealth, health.MaxHealth);
        }
        if (spellCaster != null)
        {
            spellCaster.OnManaChanged += OnManaChanged;
            OnManaChanged(spellCaster.CurrentMana, spellCaster.MaxMana);
        }
        if (buffReceiver != null)
        {
            buffReceiver.OnShieldChanged += OnShieldChanged;
            buffReceiver.OnShieldBroken += OnShieldBroken;
            OnShieldBroken();
        }

        statsSubscribed = health != null && spellCaster != null;
    }

    private void UnsubscribeStats()
    {
        if (health != null) health.OnHealthChanged -= OnHealthChanged;
        if (spellCaster != null) spellCaster.OnManaChanged -= OnManaChanged;
        if (buffReceiver != null)
        {
            buffReceiver.OnShieldChanged -= OnShieldChanged;
            buffReceiver.OnShieldBroken -= OnShieldBroken;
        }
        statsSubscribed = false;
    }

    private void OnHealthChanged(int current, int max)
    {
        for (int i = 0; i < hearts.Length; i++)
        {
            if (hearts[i] == null) continue;
            bool filled = i < current;
            hearts[i].EnableInClassList("heart--empty", !filled);
        }
    }

    private void OnManaChanged(float current, float max) => SetFill(manaFill, max > 0f ? current / max : 0f);

    private void OnShieldChanged(float normalized) => SetFill(shieldFill, normalized);

    private void OnShieldBroken() => SetFill(shieldFill, 0f);

    private static void SetFill(VisualElement fill, float normalized)
    {
        if (fill == null) return;
        fill.style.width = Length.Percent(Mathf.Clamp01(normalized) * 100f);
    }

    private void RefreshExpeditionBadge()
    {
        if (portraitBadge == null) return;
        var progress = GameCore.Instance != null ? GameCore.Instance.CurrentProgress : null;
        int attempts = progress != null ? progress.stats.successfulExpeditions + progress.stats.totalDeaths : 0;
        portraitBadge.text = attempts.ToString();
    }
}
