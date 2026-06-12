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
    private VisualElement questsContainer;

    private readonly VisualElement[] slotRoots = new VisualElement[HotbarManager.SlotCount];
    private readonly VisualElement[] slotIcons = new VisualElement[HotbarManager.SlotCount];
    private readonly VisualElement[] slotCooldowns = new VisualElement[HotbarManager.SlotCount];

    private PlayerHealth health;
    private PlayerSpellCaster spellCaster;
    private PlayerBuffReceiver buffReceiver;
    private PlayerTopDownController playerController;

    private PlayerQuestService subscribedQuestService;
    private bool questManagerSubscribed;

    private bool hotbarSubscribed;
    private bool visualsInitialized;

    private void OnEnable()
    {
        SubscribeHotbar();
        TryInitVisuals();
    }

    private void OnDisable()
    {
        UnsubscribeHotbar();
        UnsubscribeStats();
        UnsubscribeQuests();
    }

    private void Start()
    {
        TryInitVisuals();
        ResolvePlayerRefs();
        SubscribeStats();
        RefreshExpeditionBadge();
        SubscribeQuestManager();
        EnsureQuestService();
    }

    private void TryInitVisuals()
    {
        if (visualsInitialized) return;
        if (document == null) document = GetComponent<UIDocument>();
        root = document != null ? document.rootVisualElement : null;
        if (root == null) return;

        visualsInitialized = true;
        CacheElements();
        BuildHotbar();
        RefreshHotbarAll();
    }

    private void Update()
    {
        if (!visualsInitialized) TryInitVisuals();

        // Player objects may spawn after the HUD; lazily resolve and refresh.
        if (playerController == null || health == null)
        {
            ResolvePlayerRefs();
            SubscribeStats();
        }

        // Inventory containers may also appear later (expedition pack on entering Level).
        SubscribeInventoryEvents();

        // Quest service is created by LevelManager / QuestBoardGenerator after the HUD.
        EnsureQuestService();

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
        questsContainer = root.Q<VisualElement>("hud-quests");
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
            slotRoots[i] = slot;

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
        if (!visualsInitialized) return;
        for (int i = 0; i < HotbarManager.SlotCount; i++) RefreshSlot(i);
    }

    private void RefreshSlot(int index)
    {
        if (index < 0 || index >= HotbarManager.SlotCount) return;
        if (slotIcons[index] == null) return;

        string itemId = HotbarManager.Instance.GetSlotItem(index);
        bool owned = PlayerOwnsSlotItem(itemId);
        Sprite icon = owned ? GetItemIcon(itemId) : null;

        // Hide the whole slot when the player hasn't crafted/picked up this item.
        if (slotRoots[index] != null)
            slotRoots[index].style.display = (owned && icon != null) ? DisplayStyle.Flex : DisplayStyle.None;

        slotIcons[index].style.backgroundImage = icon != null ? new StyleBackground(icon) : new StyleBackground();
    }

    private bool PlayerOwnsSlotItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return false;

        // Spells: only show if the player has crafted them.
        if (itemId.StartsWith("spell_"))
        {
            var progress = GameCore.Instance != null ? GameCore.Instance.CurrentProgress : null;
            return progress != null
                && progress.crafting != null
                && progress.crafting.craftedSpells != null
                && progress.crafting.craftedSpells.Contains(itemId);
        }

        // Consumables: show only if the player actually has at least one in inventory.
        // In expedition we read the expedition pack; at Home, the home storage.
        if (ExpeditionManager.Instance != null && ExpeditionManager.Instance.IsInExpedition)
        {
            var pack = ExpeditionManager.Instance.ExpeditionInventory;
            return pack != null && pack.GetItemCount(itemId) > 0;
        }

        var storage = InventoryService.Instance != null ? InventoryService.Instance.HomeStorage : null;
        return storage != null && storage.GetItemCount(itemId) > 0;
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
        if (hotbarSubscribed) return;
        hotbarSubscribed = true;

        if (HotbarManager.Instance != null)
            HotbarManager.Instance.OnSlotUsed += OnSlotUsed;

        if (CraftingManager.Instance != null)
        {
            CraftingManager.Instance.OnSpellCrafted += OnAnySpellCrafted;
            CraftingManager.Instance.OnRecipeCrafted += OnAnyRecipeCrafted;
        }

        SubscribeInventoryEvents();
    }

    private void UnsubscribeHotbar()
    {
        if (!hotbarSubscribed) return;
        hotbarSubscribed = false;

        if (HotbarManager.Instance != null)
            HotbarManager.Instance.OnSlotUsed -= OnSlotUsed;

        if (CraftingManager.Instance != null)
        {
            CraftingManager.Instance.OnSpellCrafted -= OnAnySpellCrafted;
            CraftingManager.Instance.OnRecipeCrafted -= OnAnyRecipeCrafted;
        }

        UnsubscribeInventoryEvents();
    }

    private PlayerInventory subscribedHomeStorage;
    private PlayerInventory subscribedExpeditionInventory;

    private void SubscribeInventoryEvents()
    {
        var storage = InventoryService.Instance != null ? InventoryService.Instance.HomeStorage : null;
        if (storage != null && storage != subscribedHomeStorage)
        {
            if (subscribedHomeStorage != null) subscribedHomeStorage.OnInventoryChanged -= OnInventoryChanged;
            storage.OnInventoryChanged += OnInventoryChanged;
            subscribedHomeStorage = storage;
        }

        var pack = ExpeditionManager.Instance != null ? ExpeditionManager.Instance.ExpeditionInventory : null;
        if (pack != null && pack != subscribedExpeditionInventory)
        {
            if (subscribedExpeditionInventory != null) subscribedExpeditionInventory.OnInventoryChanged -= OnInventoryChanged;
            pack.OnInventoryChanged += OnInventoryChanged;
            subscribedExpeditionInventory = pack;
        }
    }

    private void UnsubscribeInventoryEvents()
    {
        if (subscribedHomeStorage != null) subscribedHomeStorage.OnInventoryChanged -= OnInventoryChanged;
        if (subscribedExpeditionInventory != null) subscribedExpeditionInventory.OnInventoryChanged -= OnInventoryChanged;
        subscribedHomeStorage = null;
        subscribedExpeditionInventory = null;
    }

    // Inventory drives both the hotbar (item ownership) and CollectItem quest progress.
    private void OnInventoryChanged()
    {
        TryInitVisuals();
        RefreshHotbarAll();
        RefreshQuests();
    }

    private void OnSlotUsed(int index) { RefreshSlot(index); }
    private void OnAnySpellCrafted(SpellDefinition _) { TryInitVisuals(); RefreshHotbarAll(); }
    private void OnAnyRecipeCrafted(RecipeDefinition _) { TryInitVisuals(); RefreshHotbarAll(); }

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

    // ---------- Active quest tracker (top-right) ----------

    // The quest service is owned by the active scene: LevelManager during an
    // expedition, QuestBoardGenerator at Home. Both write the same save, so the
    // accepted-quest list stays consistent across scenes.
    private PlayerQuestService ResolveQuestService()
    {
        LevelManager level = FindFirstObjectByType<LevelManager>();
        if (level != null)
        {
            PlayerQuestService levelService = level.GetQuestService();
            if (levelService != null) return levelService;
        }

        QuestBoardGenerator board = FindFirstObjectByType<QuestBoardGenerator>();
        return board != null ? board.Service : null;
    }

    private void SubscribeQuestManager()
    {
        if (questManagerSubscribed) return;
        QuestManager qm = QuestManager.Instance;
        if (qm == null) return;

        qm.OnQuestProgressUpdated += OnQuestProgressUpdated;
        qm.OnQuestCompleted += OnQuestCompleted;
        questManagerSubscribed = true;
    }

    private void EnsureQuestService()
    {
        PlayerQuestService current = ResolveQuestService();
        if (current == subscribedQuestService)
        {
            return;
        }

        if (subscribedQuestService != null)
            subscribedQuestService.OnQuestsChanged -= RefreshQuests;

        subscribedQuestService = current;
        if (subscribedQuestService != null)
            subscribedQuestService.OnQuestsChanged += RefreshQuests;

        RefreshQuests();
    }

    private void UnsubscribeQuests()
    {
        if (subscribedQuestService != null)
            subscribedQuestService.OnQuestsChanged -= RefreshQuests;
        subscribedQuestService = null;

        if (questManagerSubscribed && QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestProgressUpdated -= OnQuestProgressUpdated;
            QuestManager.Instance.OnQuestCompleted -= OnQuestCompleted;
        }
        questManagerSubscribed = false;
    }

    private void OnQuestProgressUpdated(string questId, int value) => RefreshQuests();
    private void OnQuestCompleted(string questId) => RefreshQuests();

    private void RefreshQuests()
    {
        if (questsContainer == null) return;
        questsContainer.Clear();

        PlayerQuestService service = subscribedQuestService ?? ResolveQuestService();
        if (service == null) return;

        List<QuestData> active = service.GetActiveQuests();
        if (active == null || active.Count == 0) return;

        foreach (QuestData quest in active)
        {
            if (quest == null) continue;

            var row = new VisualElement();
            row.AddToClassList("quest-row");
            row.pickingMode = PickingMode.Ignore;

            var title = new Label(string.IsNullOrEmpty(quest.description) ? "Задание" : quest.description);
            title.AddToClassList("quest-row__title");
            title.pickingMode = PickingMode.Ignore;
            row.Add(title);

            var progress = new Label(GetQuestProgressText(quest));
            progress.AddToClassList("quest-row__progress");
            progress.pickingMode = PickingMode.Ignore;
            row.Add(progress);

            questsContainer.Add(row);
        }
    }

    private string GetQuestProgressText(QuestData quest)
    {
        int required = Mathf.Max(1, quest.requiredCount);
        int current;

        // CollectItem progress is the count carried in the expedition pack, but only
        // while an expedition is underway. At Home (no pack yet) it reads back the
        // tracked progress, which is 0 before the run — otherwise the home stockpile
        // would falsely satisfy "collect X" objectives the moment the chest holds them.
        bool inExpedition = ExpeditionManager.Instance != null && ExpeditionManager.Instance.IsInExpedition;
        if (quest.type == QuestType.CollectItem && inExpedition)
        {
            PlayerInventory pack = ExpeditionManager.Instance.ExpeditionInventory;
            string targetId = quest.GetResolvedTargetId();
            current = pack != null && !string.IsNullOrEmpty(targetId)
                ? pack.GetItemCount(targetId)
                : QuestManager.Instance.GetProgress(quest.id);
        }
        else
        {
            current = QuestManager.Instance.GetProgress(quest.id);
        }

        current = Mathf.Clamp(current, 0, required);
        return current >= required ? $"Готово ✓   {current}/{required}" : $"{current} / {required}";
    }
}
