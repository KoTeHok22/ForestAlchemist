using System;
using UnityEngine;

/// <summary>
/// Persistent character stat upgrades bought on Home with Кровь орка.
/// Levels are saved in GameProgressData.upgrades and applied at runtime via
/// <see cref="PlayerStatApplicator"/>.
/// </summary>
public sealed class PlayerUpgradeService : MonoBehaviour
{
    public const int MaxLevel = 10;
    public const string CurrencyId = ItemCatalog.OrcBlood;

    private static PlayerUpgradeService instance;
    public static PlayerUpgradeService Instance => ResolveInstance();

    public event Action OnUpgradesChanged;

    [Header("Base values (match default player components)")]
    [SerializeField] private int baseMeleeDamage = 20;
    [SerializeField] private float baseMaxStamina = 100f;
    [SerializeField] private float baseMaxMana = 100f;
    [SerializeField] private float baseMoveSpeedMultiplier = 1f;
    [SerializeField] private float baseSpellDamageMultiplier = 1f;

    [Header("Per-level bonuses")]
    [SerializeField] private float spellDamagePerLevel = 0.08f;
    [SerializeField] private int meleeDamagePerLevel = 3;
    [SerializeField] private float staminaPerLevel = 10f;
    [SerializeField] private float manaPerLevel = 10f;
    [SerializeField] private float moveSpeedPerLevel = 0.05f;

    [Header("Upgrade costs (Кровь орка)")]
    [SerializeField] private int spellCostBase = 4;
    [SerializeField] private int meleeCostBase = 3;
    [SerializeField] private int staminaCostBase = 3;
    [SerializeField] private int manaCostBase = 3;
    [SerializeField] private int moveSpeedCostBase = 5;
    [SerializeField] private int costStepPerLevel = 2;

    private static PlayerUpgradeService ResolveInstance()
    {
        if (RuntimeSingletonGuard.IsShuttingDown) return null;
        if (instance == null)
        {
            instance = FindAnyObjectByType<PlayerUpgradeService>(FindObjectsInactive.Include);
        }

        if (instance == null)
        {
            GameObject go = new GameObject("PlayerUpgradeService");
            instance = go.AddComponent<PlayerUpgradeService>();
            DontDestroyOnLoad(go);
        }

        return instance;
    }

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

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public int GetLevel(PlayerUpgradeStat stat)
    {
        PlayerUpgradeData data = GameCore.Instance?.CurrentProgress?.upgrades;
        if (data == null) return 0;

        return stat switch
        {
            PlayerUpgradeStat.SpellPower => Mathf.Clamp(data.spellPowerLevel, 0, MaxLevel),
            PlayerUpgradeStat.MeleePower => Mathf.Clamp(data.meleePowerLevel, 0, MaxLevel),
            PlayerUpgradeStat.Stamina => Mathf.Clamp(data.staminaLevel, 0, MaxLevel),
            PlayerUpgradeStat.Mana => Mathf.Clamp(data.manaLevel, 0, MaxLevel),
            PlayerUpgradeStat.MoveSpeed => Mathf.Clamp(data.moveSpeedLevel, 0, MaxLevel),
            _ => 0
        };
    }

    public bool IsMaxed(PlayerUpgradeStat stat) => GetLevel(stat) >= MaxLevel;

    public int GetUpgradeCost(PlayerUpgradeStat stat)
    {
        if (IsMaxed(stat)) return 0;
        int level = GetLevel(stat);
        return GetCostBase(stat) + level * costStepPerLevel;
    }

    public int GetBlood()
    {
        var storage = InventoryService.Instance?.HomeStorage;
        return storage != null ? storage.GetItemCount(CurrencyId) : 0;
    }

    public bool CanUpgrade(PlayerUpgradeStat stat)
    {
        return !IsMaxed(stat) && GetBlood() >= GetUpgradeCost(stat);
    }

    public bool TryUpgrade(PlayerUpgradeStat stat)
    {
        if (!CanUpgrade(stat)) return false;

        PlayerUpgradeData data = GameCore.Instance?.CurrentProgress?.upgrades;
        var storage = InventoryService.Instance?.HomeStorage;
        if (data == null || storage == null) return false;

        int cost = GetUpgradeCost(stat);
        if (!storage.RemoveItem(CurrencyId, cost)) return false;

        switch (stat)
        {
            case PlayerUpgradeStat.SpellPower: data.spellPowerLevel++; break;
            case PlayerUpgradeStat.MeleePower: data.meleePowerLevel++; break;
            case PlayerUpgradeStat.Stamina: data.staminaLevel++; break;
            case PlayerUpgradeStat.Mana: data.manaLevel++; break;
            case PlayerUpgradeStat.MoveSpeed: data.moveSpeedLevel++; break;
        }

        GameProgressUtility.Touch(GameCore.Instance.CurrentProgress);
        GameCore.Instance.SaveProgress();
        OnUpgradesChanged?.Invoke();
        AudioHooks.Sfx(AudioClipId.SfxHomeStatUpgradePurchase);
        return true;
    }

    public float GetSpellDamageMultiplier()
    {
        return baseSpellDamageMultiplier + GetLevel(PlayerUpgradeStat.SpellPower) * spellDamagePerLevel;
    }

    public int GetMeleeDamage()
    {
        return baseMeleeDamage + GetLevel(PlayerUpgradeStat.MeleePower) * meleeDamagePerLevel;
    }

    public float GetMaxStamina()
    {
        return baseMaxStamina + GetLevel(PlayerUpgradeStat.Stamina) * staminaPerLevel;
    }

    public float GetMaxMana()
    {
        return baseMaxMana + GetLevel(PlayerUpgradeStat.Mana) * manaPerLevel;
    }

    public float GetMoveSpeedMultiplier()
    {
        return baseMoveSpeedMultiplier + GetLevel(PlayerUpgradeStat.MoveSpeed) * moveSpeedPerLevel;
    }

    public string GetDisplayName(PlayerUpgradeStat stat)
    {
        return stat switch
        {
            PlayerUpgradeStat.SpellPower => "Сила заклинаний",
            PlayerUpgradeStat.MeleePower => "Сила удара",
            PlayerUpgradeStat.Stamina => "Стамина",
            PlayerUpgradeStat.Mana => "Мана",
            PlayerUpgradeStat.MoveSpeed => "Скорость",
            _ => stat.ToString()
        };
    }

    public string GetDescription(PlayerUpgradeStat stat)
    {
        return stat switch
        {
            PlayerUpgradeStat.SpellPower => "Усиливает урон, лечение и щиты заклинаний.",
            PlayerUpgradeStat.MeleePower => "Увеличивает урон ближней атаки по ЛКМ.",
            PlayerUpgradeStat.Stamina => "Больше выносливости для бега и ударов.",
            PlayerUpgradeStat.Mana => "Больше маны для заклинаний.",
            PlayerUpgradeStat.MoveSpeed => "Быстрее ходьба и бег по лесу.",
            _ => string.Empty
        };
    }

    public string GetValueText(PlayerUpgradeStat stat)
    {
        int level = GetLevel(stat);
        return stat switch
        {
            PlayerUpgradeStat.SpellPower => $"×{GetSpellDamageMultiplier():0.00}",
            PlayerUpgradeStat.MeleePower => $"{GetMeleeDamage()} урона",
            PlayerUpgradeStat.Stamina => $"{Mathf.RoundToInt(GetMaxStamina())} ед.",
            PlayerUpgradeStat.Mana => $"{Mathf.RoundToInt(GetMaxMana())} ед.",
            PlayerUpgradeStat.MoveSpeed => $"×{GetMoveSpeedMultiplier():0.00}",
            _ => level.ToString()
        };
    }

    public string GetNextValueText(PlayerUpgradeStat stat)
    {
        if (IsMaxed(stat)) return "Максимум";

        int nextLevel = GetLevel(stat) + 1;
        return stat switch
        {
            PlayerUpgradeStat.SpellPower =>
                $"×{baseSpellDamageMultiplier + nextLevel * spellDamagePerLevel:0.00}",
            PlayerUpgradeStat.MeleePower =>
                $"{baseMeleeDamage + nextLevel * meleeDamagePerLevel} урона",
            PlayerUpgradeStat.Stamina =>
                $"{Mathf.RoundToInt(baseMaxStamina + nextLevel * staminaPerLevel)} ед.",
            PlayerUpgradeStat.Mana =>
                $"{Mathf.RoundToInt(baseMaxMana + nextLevel * manaPerLevel)} ед.",
            PlayerUpgradeStat.MoveSpeed =>
                $"×{baseMoveSpeedMultiplier + nextLevel * moveSpeedPerLevel:0.00}",
            _ => nextLevel.ToString()
        };
    }

    public static string GetIconResourcePath(PlayerUpgradeStat stat)
    {
        return stat switch
        {
            PlayerUpgradeStat.SpellPower => "Game/Icons/spell_firebolt",
            PlayerUpgradeStat.MeleePower => "Game/Icons/warchief_trophy",
            PlayerUpgradeStat.Stamina => "Game/Icons/stamina_elixir",
            PlayerUpgradeStat.Mana => "Game/Icons/mana_potion",
            PlayerUpgradeStat.MoveSpeed => "Game/Icons/spell_airdash",
            _ => "Game/Icons/health_potion"
        };
    }

    public static PlayerUpgradeStat[] AllStats { get; } =
    {
        PlayerUpgradeStat.SpellPower,
        PlayerUpgradeStat.MeleePower,
        PlayerUpgradeStat.Stamina,
        PlayerUpgradeStat.Mana,
        PlayerUpgradeStat.MoveSpeed
    };

    private int GetCostBase(PlayerUpgradeStat stat)
    {
        return stat switch
        {
            PlayerUpgradeStat.SpellPower => spellCostBase,
            PlayerUpgradeStat.MeleePower => meleeCostBase,
            PlayerUpgradeStat.Stamina => staminaCostBase,
            PlayerUpgradeStat.Mana => manaCostBase,
            PlayerUpgradeStat.MoveSpeed => moveSpeedCostBase,
            _ => 3
        };
    }
}
