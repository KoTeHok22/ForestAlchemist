using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewGardenHarvestTable", menuName = "Garden/Harvest Table")]
public sealed class GardenHarvestTable : ScriptableObject
{
    [System.Serializable]
    public class StageDrops
    {
        public int stage;
        public GardenHarvestEntry[] drops;
    }

    [SerializeField] private StageDrops[] stageDrops;

    public GardenHarvestEntry[] GetDropsForStage(int stage)
    {
        if (stageDrops == null) return new GardenHarvestEntry[0];

        for (int i = 0; i < stageDrops.Length; i++)
        {
            if (stageDrops[i].stage == stage) return stageDrops[i].drops;
        }

        return new GardenHarvestEntry[0];
    }
}

[System.Serializable]
public struct GardenHarvestEntry
{
    public string itemName;
    public float chance;
    public int minAmount;
    public int maxAmount;
}
