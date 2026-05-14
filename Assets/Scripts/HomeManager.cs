using UnityEngine;

public sealed class HomeManager : MonoBehaviour
{
    [SerializeField] private InventoryDisplay homeInventoryDisplay;
    [SerializeField] private LevelQuestIconProvider iconProvider;
    [SerializeField] private GardenVisuals gardenVisuals;
    [SerializeField] private GardenHarvestInteraction gardenHarvest;
    [SerializeField] private CraftingUI craftingUI;
    [SerializeField] private ExpeditionPreparationUI expeditionPreparationUI;

    private void Awake()
    {
        if (homeInventoryDisplay == null) homeInventoryDisplay = Object.FindAnyObjectByType<InventoryDisplay>();
        if (iconProvider == null) iconProvider = Object.FindAnyObjectByType<LevelQuestIconProvider>();
        if (gardenVisuals == null) gardenVisuals = Object.FindAnyObjectByType<GardenVisuals>();
        if (gardenHarvest == null) gardenHarvest = Object.FindAnyObjectByType<GardenHarvestInteraction>();
        if (craftingUI == null) craftingUI = Object.FindAnyObjectByType<CraftingUI>();
        if (expeditionPreparationUI == null) expeditionPreparationUI = Object.FindAnyObjectByType<ExpeditionPreparationUI>();

        if (homeInventoryDisplay != null)
        {
            homeInventoryDisplay.Initialize(InventoryService.Instance.HomeStorage, iconProvider);
        }

        if (gardenVisuals != null)
        {
            gardenVisuals.UpdateVisuals();
        }

        if (gardenHarvest != null)
        {
            gardenHarvest.ResetHarvestState();
        }
    }

    private void Start()
    {
        GameCore.Instance.SaveProgress();
    }

    public void OpenCrafting()
    {
        if (craftingUI != null) craftingUI.Open();
    }

    public void CloseCrafting()
    {
        if (craftingUI != null) craftingUI.Close();
    }

    public void OpenExpeditionPreparation()
    {
        if (expeditionPreparationUI != null)
        {
            expeditionPreparationUI.Open();
        }
    }
}
