using UnityEngine;

public sealed class HomeSceneBootstrap : MonoBehaviour
{
    [SerializeField] private HomeManager homeManager;
    [SerializeField] private CraftingUI craftingUI;
    [SerializeField] private ShopUI shopUI;
    [SerializeField] private GardenHarvestInteraction gardenHarvest;
    [SerializeField] private ExpeditionPreparationUI expeditionPreparationUI;
    [SerializeField] private HomeStorageUI homeStorageUI;
    [SerializeField] private StatUpgradeUI statUpgradeUI;

    private void Awake()
    {
        HomeUIBlocker.ForceReset();
        EnsureHomeManager();
        EnsureServices();
        EnsureStatUpgrade();
        EnsurePlayerStats();
    }

    private void EnsureHomeManager()
    {
        if (homeManager == null) homeManager = FindFirstObjectByType<HomeManager>();
        if (homeManager == null)
        {
            GameObject go = new GameObject("HomeManager");
            homeManager = go.AddComponent<HomeManager>();
        }

        if (craftingUI == null)
        {
            craftingUI = FindFirstObjectByType<CraftingUI>();
        }

        if (shopUI == null)
        {
            shopUI = FindFirstObjectByType<ShopUI>();
        }

        if (gardenHarvest == null)
        {
            gardenHarvest = FindFirstObjectByType<GardenHarvestInteraction>();
        }

        if (expeditionPreparationUI == null)
        {
            expeditionPreparationUI = FindFirstObjectByType<ExpeditionPreparationUI>();
        }

        if (homeStorageUI == null)
        {
            homeStorageUI = FindFirstObjectByType<HomeStorageUI>();
        }
    }

    private void EnsureServices()
    {
        InventoryService.Instance.GetHashCode();
        ShopService.Instance.GetHashCode();
        CraftingManager.Instance.GetHashCode();
        CraftingProgressionService.Instance.GetHashCode();
        ExpeditionManager.Instance.GetHashCode();
        GardenService.Instance.GetHashCode();
        OrcEvolutionService.Instance.GetHashCode();
        GameCore.Instance.GetHashCode();
        PlayerUpgradeService.Instance.GetHashCode();
    }

    private void EnsureStatUpgrade()
    {
        if (statUpgradeUI == null)
        {
            statUpgradeUI = FindFirstObjectByType<StatUpgradeUI>();
        }

        if (statUpgradeUI == null)
        {
            GameObject uiHost = new GameObject("StatUpgradeUI");
            statUpgradeUI = uiHost.AddComponent<StatUpgradeUI>();
            uiHost.AddComponent<StatUpgradeToggleInput>();
        }
        else if (statUpgradeUI.GetComponent<StatUpgradeToggleInput>() == null)
        {
            statUpgradeUI.gameObject.AddComponent<StatUpgradeToggleInput>();
        }
    }

    private static void EnsurePlayerStats()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        if (player.GetComponent<PlayerStatApplicator>() == null)
        {
            player.AddComponent<PlayerStatApplicator>();
        }
    }

    public void OpenCrafting() => craftingUI?.Open();
    public void OpenShop() => shopUI?.Open();
    public void OpenStatUpgrade() => statUpgradeUI?.Open();
}
