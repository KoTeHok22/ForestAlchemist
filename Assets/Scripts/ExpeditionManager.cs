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
    public event Action OnExpeditionStarted;

    public int CurrentSeed { get; private set; }
    public bool IsInExpedition { get; private set; }
    public PlayerInventory ExpeditionInventory { get; private set; }
    public float CurrentVisibility { get; private set; }
    public bool ReturnUnlocked { get; private set; }
    public string ActiveReturnMethod { get; private set; }
    public ExpeditionResult PendingResult { get; private set; }

    private ExpeditionStats lastResultStats = new ExpeditionStats();
    private Action expeditionInventoryChangedHandler;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        ReloadFromCurrentAccount();
    }

    public void ReloadFromCurrentAccount()
    {
        if (ExpeditionInventory != null)
        {
            ExpeditionInventory.OnInventoryChanged -= UpdateVisibility;
            if (expeditionInventoryChangedHandler != null)
            {
                ExpeditionInventory.OnInventoryChanged -= expeditionInventoryChangedHandler;
            }
        }

        GameProgressData progress = GameCore.Instance.CurrentProgress;
        PlayerInventorySave save = new PlayerInventorySave
        {
            slots = progress?.expeditionInventory?.slots ?? new List<InventorySlot>()
        };

        ExpeditionInventory = new PlayerInventory(save);
        ExpeditionInventory.OnInventoryChanged += UpdateVisibility;
        expeditionInventoryChangedHandler = () =>
        {
            GameProgressData currentProgress = GameCore.Instance.CurrentProgress;
            if (currentProgress?.expeditionInventory == null)
            {
                return;
            }

            currentProgress.expeditionInventory.slots = ExpeditionInventory.GetAllSlots();
            GameProgressUtility.Touch(currentProgress);
            GameCore.Instance.SaveProgress();
        };
        ExpeditionInventory.OnInventoryChanged += expeditionInventoryChangedHandler;
        UpdateVisibility();
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
        PrepareForExpedition();
        CurrentSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        IsInExpedition = true;
        PendingResult = ExpeditionResult.None;
        ReturnUnlocked = false;
        ActiveReturnMethod = string.Empty;
        CurrentVisibility = 0f;
        SceneManager.LoadScene("Level");
        OnExpeditionStarted?.Invoke();
    }

    public void EnterCurrentLevelExpedition()
    {
        if (IsInExpedition)
        {
            return;
        }

        PrepareForExpedition();
        CurrentSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        IsInExpedition = true;
        PendingResult = ExpeditionResult.None;
        ReturnUnlocked = false;
        ActiveReturnMethod = string.Empty;
        CurrentVisibility = 0f;
        OnExpeditionStarted?.Invoke();
    }

    public void EndExpedition(ExpeditionResult result)
    {
        if (!IsInExpedition) return;

        GameProgressData progress = GameCore.Instance.CurrentProgress;
        int reachedThreatLevel = progress != null ? progress.orcs.threatLevel : 0;

        if (result == ExpeditionResult.Success && !ReturnUnlocked)
        {
            Debug.Log("[Expedition] Return is locked. Use a portal, a return scroll, or an evacuation point.");
            return;
        }

        IsInExpedition = false;

        if (result == ExpeditionResult.Success)
        {
            TransferLootToHome();
            if (progress != null)
            {
                progress.stats.successfulExpeditions++;
                progress.stats.deepestThreatReached = Mathf.Max(progress.stats.deepestThreatReached, reachedThreatLevel);
            }

            GardenService.Instance.AdvanceGrowth();
            OrcEvolutionService.Instance.Evolve(false);
        }
        else if (result == ExpeditionResult.Death)
        {
            ExpeditionInventory.Clear();

            if (progress != null)
            {
                progress.stats.totalDeaths++;
                progress.stats.deepestThreatReached = Mathf.Max(progress.stats.deepestThreatReached, reachedThreatLevel);
            }

            OrcEvolutionService.Instance.Evolve(true);
        }

        lastResultStats = CreateStatsSnapshot(progress);

        ReturnUnlocked = false;
        ActiveReturnMethod = string.Empty;
        PendingResult = result;

        GameCore.Instance.SaveProgress();
        OnExpeditionEnded?.Invoke(result);
        StartCoroutine(LoadHomeSceneNextFrame());
    }

    public bool TryConsumePendingResult(out ExpeditionResult result)
    {
        result = PendingResult;
        if (result == ExpeditionResult.None)
        {
            return false;
        }

        PendingResult = ExpeditionResult.None;
        return true;
    }

    public bool TryUnlockReturn(string methodId)
    {
        if (!IsInExpedition)
        {
            return false;
        }

        ReturnUnlocked = true;
        ActiveReturnMethod = methodId ?? string.Empty;
        return true;
    }

    public bool CanReturn() => IsInExpedition && ReturnUnlocked;

    public ExpeditionStats GetLastResultStatsSnapshot()
    {
        return new ExpeditionStats
        {
            successfulExpeditions = lastResultStats.successfulExpeditions,
            totalDeaths = lastResultStats.totalDeaths,
            deepestThreatReached = lastResultStats.deepestThreatReached
        };
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

    private void PrepareForExpedition()
    {
        ExpeditionInventory.Clear();

        var progress = GameCore.Instance.CurrentProgress;
        var homeStorage = InventoryService.Instance.HomeStorage;
        if (progress?.loadout?.consumables == null || homeStorage == null)
        {
            return;
        }

        for (int i = 0; i < progress.loadout.consumables.Count; i++)
        {
            InventorySlot slot = progress.loadout.consumables[i];
            if (slot == null || string.IsNullOrEmpty(slot.itemName) || slot.count <= 0)
            {
                continue;
            }

            int available = homeStorage.GetItemCount(slot.itemName);
            int amountToTake = Mathf.Min(available, slot.count);
            if (amountToTake <= 0)
            {
                continue;
            }

            homeStorage.RemoveItem(slot.itemName, amountToTake);
            ExpeditionInventory.AddItem(slot.itemName, amountToTake);
        }
    }

    private System.Collections.IEnumerator LoadHomeSceneNextFrame()
    {
        yield return null;
        SceneManager.LoadScene("Home");
    }

    private static ExpeditionStats CreateStatsSnapshot(GameProgressData progress)
    {
        if (progress == null)
        {
            return new ExpeditionStats();
        }

        return new ExpeditionStats
        {
            successfulExpeditions = progress.stats.successfulExpeditions,
            totalDeaths = progress.stats.totalDeaths,
            deepestThreatReached = progress.stats.deepestThreatReached
        };
    }
}
