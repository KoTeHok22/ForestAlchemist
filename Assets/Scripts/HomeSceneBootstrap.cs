using UnityEngine;

public sealed class HomeSceneBootstrap : MonoBehaviour
{
    [SerializeField] private HomeManager homeManager;
    [SerializeField] private CraftingUI craftingUI;
    [SerializeField] private ShopUI shopUI;
    [SerializeField] private GardenHarvestInteraction gardenHarvest;
    [SerializeField] private ExpeditionPreparationUI expeditionPreparationUI;
    [SerializeField] private HomeStorageUI homeStorageUI;

    private void Awake()
    {
        EnsureHomeManager();
        EnsureServices();
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

        if (expeditionPreparationUI == null)
        {
            GameObject go = new GameObject("ExpeditionPreparationUI");
            expeditionPreparationUI = go.AddComponent<ExpeditionPreparationUI>();
        }

        if (homeStorageUI == null)
        {
            homeStorageUI = FindFirstObjectByType<HomeStorageUI>();
        }

        if (homeStorageUI == null)
        {
            GameObject go = new GameObject("HomeStorageUI");
            homeStorageUI = go.AddComponent<HomeStorageUI>();
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
    }

    public void OpenCrafting() => craftingUI?.Open();
    public void OpenShop() => shopUI?.Open();
}
