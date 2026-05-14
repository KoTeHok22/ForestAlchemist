using System.Collections.Generic;

public static class ItemCatalog
{
    public const string OrcBlood = "orc_blood";
    public const string SakuraSapling = "sakura_sapling";
    public const string OakSapling = "oak_sapling";
    public const string AppleSapling = "apple_sapling";
    public const string RareFlower = "rare_flower";
    public const string GreenOrcDrop = "green_orc_drop";
    public const string ShamanTalisman = "shaman_talisman";
    public const string WarchiefTrophy = "warchief_trophy";

    public const string HealthPotion = "health_potion";
    public const string ManaPotion = "mana_potion";
    public const string ShieldScroll = "shield_scroll";
    public const string ReturnScroll = "return_scroll";

    private static readonly Dictionary<string, string> DisplayNames = new Dictionary<string, string>
    {
        { OrcBlood, "Кровь орка" },
        { SakuraSapling, "Саженец сакуры" },
        { OakSapling, "Саженец дуба" },
        { AppleSapling, "Саженец яблони" },
        { RareFlower, "Редкий цветок" },
        { GreenOrcDrop, "Трофей зелёного орка" },
        { ShamanTalisman, "Шаманский талисман" },
        { WarchiefTrophy, "Трофей вождя" },
        { HealthPotion, "Зелье здоровья" },
        { ManaPotion, "Зелье маны" },
        { ShieldScroll, "Свиток щита" },
        { ReturnScroll, "Свиток возврата" },
        { "spell_firebolt", "Огненный болт" },
        { "spell_waterspring", "Источник воды" },
        { "spell_stoneskin", "Каменная кожа" },
        { "spell_airdash", "Порыв ветра" }
    };

    private static readonly Dictionary<string, string> LegacyAliases = new Dictionary<string, string>
    {
        { "КровьОрка", OrcBlood },
        { "Кровь орка", OrcBlood },
        { "СаженецСакуры", SakuraSapling },
        { "СаженецДуба", OakSapling },
        { "СаженецЯблони", AppleSapling },
        { "РедкийЦветок", RareFlower },
        { "ДропСЗеленогоОрка", GreenOrcDrop },
        { "ШаманскийТалисман", ShamanTalisman },
        { "ТрофейВождя", WarchiefTrophy }
    };

    public static string Normalize(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return string.Empty;
        }

        if (LegacyAliases.TryGetValue(itemId, out string mapped))
        {
            return mapped;
        }

        return itemId;
    }

    public static string GetDisplayName(string itemId)
    {
        string normalized = Normalize(itemId);
        return DisplayNames.TryGetValue(normalized, out string displayName) ? displayName : normalized;
    }
}
