using UnityEngine;

public enum WorldBiomeType
{
    Forest,
    Meadow,
    Swamp,
    Ruins,
    DeepWood
}

public static class WorldBiome
{
    public static WorldBiomeType Resolve(Vector2Int chunkCoordinate, int seed)
    {
        Vector2Int region = new Vector2Int(
            FloorDiv(chunkCoordinate.x, 2),
            FloorDiv(chunkCoordinate.y, 2));

        float sample = Sample01(region, seed, 41);
        if (sample < 0.18f) return WorldBiomeType.Meadow;
        if (sample < 0.36f) return WorldBiomeType.Swamp;
        if (sample < 0.52f) return WorldBiomeType.Ruins;
        if (sample < 0.72f) return WorldBiomeType.DeepWood;
        return WorldBiomeType.Forest;
    }

    public static Color GetGroundTint(WorldBiomeType biome)
    {
        return biome switch
        {
            WorldBiomeType.Meadow => new Color(1.05f, 1.02f, 0.82f, 1f),
            WorldBiomeType.Swamp => new Color(0.72f, 0.88f, 0.78f, 1f),
            WorldBiomeType.Ruins => new Color(0.9f, 0.84f, 0.74f, 1f),
            WorldBiomeType.DeepWood => new Color(0.62f, 0.78f, 0.62f, 1f),
            _ => Color.white
        };
    }

    public static float GetSpawnChanceMultiplier(WorldBiomeType biome)
    {
        return biome switch
        {
            WorldBiomeType.Meadow => 1.1f,
            WorldBiomeType.Swamp => 0.85f,
            WorldBiomeType.Ruins => 0.95f,
            WorldBiomeType.DeepWood => 1.15f,
            _ => 1f
        };
    }

    public static float GetEnemyBaseChanceMultiplier(WorldBiomeType biome)
    {
        return biome switch
        {
            WorldBiomeType.Ruins => 1.35f,
            WorldBiomeType.DeepWood => 1.25f,
            WorldBiomeType.Swamp => 1.1f,
            WorldBiomeType.Meadow => 0.75f,
            _ => 1f
        };
    }

    public static bool AllowsObject(string objectName, WorldBiomeType biome)
    {
        string lowered = objectName.ToLowerInvariant();

        if (ContainsAny(lowered, "стат", "statue"))
        {
            return biome == WorldBiomeType.Ruins || biome == WorldBiomeType.Swamp;
        }

        if (ContainsAny(lowered, "камен", "rock", "stone"))
        {
            return biome != WorldBiomeType.Meadow || SampleObjectBias(lowered, biome) > 0.35f;
        }

        if (ContainsAny(lowered, "сакур", "sakura", "яблон", "apple", "цвет", "flower", "жемч"))
        {
            return biome == WorldBiomeType.Meadow || biome == WorldBiomeType.Forest || biome == WorldBiomeType.Swamp;
        }

        if (ContainsAny(lowered, "дуб", "oak"))
        {
            return biome == WorldBiomeType.DeepWood || biome == WorldBiomeType.Forest;
        }

        if (ContainsAny(lowered, "дерев", "tree", "rast"))
        {
            return biome != WorldBiomeType.Ruins;
        }

        return true;
    }

    public static string ToRussianName(WorldBiomeType biome)
    {
        return biome switch
        {
            WorldBiomeType.Meadow => "Луг",
            WorldBiomeType.Swamp => "Болото",
            WorldBiomeType.Ruins => "Руины",
            WorldBiomeType.DeepWood => "Чаща",
            _ => "Лес"
        };
    }

    private static float SampleObjectBias(string objectName, WorldBiomeType biome)
    {
        int hash = objectName.GetHashCode() ^ ((int)biome * 397);
        hash = (hash ^ (hash >> 13)) * 1274126177;
        return (hash & 0xFFFF) / 65535f;
    }

    private static bool ContainsAny(string value, params string[] tokens)
    {
        for (int i = 0; i < tokens.Length; i++)
        {
            if (value.Contains(tokens[i]))
            {
                return true;
            }
        }

        return false;
    }

    private static int FloorDiv(int value, int divisor)
    {
        if (value >= 0)
        {
            return value / divisor;
        }

        return (value - divisor + 1) / divisor;
    }

    private static float Sample01(Vector2Int coordinate, int seed, int salt)
    {
        uint hash = 2166136261u;
        hash = (hash ^ (uint)coordinate.x) * 16777619u;
        hash = (hash ^ (uint)coordinate.y) * 16777619u;
        hash = (hash ^ (uint)seed) * 16777619u;
        hash = (hash ^ (uint)salt) * 16777619u;
        return (hash & 0x00FFFFFF) / 16777215f;
    }
}
