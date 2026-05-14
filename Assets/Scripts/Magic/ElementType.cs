public enum ElementType
{
    None,
    Fire,
    Water,
    Earth,
    Air
}

public static class ElementExtensions
{
    public static ElementType GetCounter(this ElementType element)
    {
        switch (element)
        {
            case ElementType.Fire: return ElementType.Water;
            case ElementType.Water: return ElementType.Earth;
            case ElementType.Earth: return ElementType.Air;
            case ElementType.Air: return ElementType.Fire;
            default: return ElementType.None;
        }
    }

    public static float GetMultiplierAgainst(this ElementType attacker, ElementType defender)
    {
        if (attacker == ElementType.None || defender == ElementType.None) return 1f;
        if (attacker.GetCounter() == defender) return 1.5f;
        if (defender.GetCounter() == attacker) return 0.5f;
        return 1f;
    }

    public static string ToRussianName(this ElementType element)
    {
        switch (element)
        {
            case ElementType.Fire: return "Огонь";
            case ElementType.Water: return "Вода";
            case ElementType.Earth: return "Земля";
            case ElementType.Air: return "Воздух";
            default: return "Нет";
        }
    }
}
