using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;

public enum ExpeditionResult
{
    None,
    Success,
    Death,
    Abandoned
}

public sealed class ExpeditionManager : MonoBehaviour
{
    private static ExpeditionManager instance;
    public static ExpeditionManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("ExpeditionManager");
                instance = go.AddComponent<ExpeditionManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    public event Action<ExpeditionResult> OnExpeditionEnded;

    public int CurrentSeed { get; private set; }
    public bool IsInExpedition { get; private set; }
    public PlayerInventory ExpeditionInventory { get; private set; }
    public float CurrentVisibility { get; private set; }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        ExpeditionInventory = new PlayerInventory("expedition_inventory.json");
        ExpeditionInventory.OnInventoryChanged += UpdateVisibility;
    }

    private void UpdateVisibility()
    {
        float visibility = 0f;
        foreach (var slot in ExpeditionInventory.GetAllSlots())
        {
            visibility += slot.count * 0.1f;
        }
        CurrentVisibility = visibility;
    }

    public void StartExpedition()
    {
        CurrentSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        IsInExpedition = true;
        ExpeditionInventory.Clear();
        SceneManager.LoadScene("Level");
    }

    public void EndExpedition(ExpeditionResult result)
    {
        if (!IsInExpedition) return;
        IsInExpedition = false;

        if (result == ExpeditionResult.Success)
        {
            TransferLootToHome();
            GameCore.Instance.CurrentProgress.stats.successfulExpeditions++;
            GardenService.Instance.AdvanceGrowth();
            OrcEvolutionService.Instance.Evolve(false);
        }
        else if (result == ExpeditionResult.Death)
        {
            ExpeditionInventory.Clear();
            GameCore.Instance.CurrentProgress.stats.totalDeaths++;
            OrcEvolutionService.Instance.Evolve(true);
        }

        GameCore.Instance.SaveProgress();
        OnExpeditionEnded?.Invoke(result);
        SceneManager.LoadScene("Home");
    }

    private void TransferLootToHome()
    {
        var homeStorage = InventoryService.Instance.HomeStorage;
        if (homeStorage == null) return;

        foreach (var expeditionSlot in ExpeditionInventory.GetAllSlots())
        {
            homeStorage.AddItem(expeditionSlot.itemName, expeditionSlot.count);
        }
        ExpeditionInventory.Clear();
    }
}
