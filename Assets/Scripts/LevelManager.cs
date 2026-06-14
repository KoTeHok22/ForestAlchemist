using UnityEngine;
using UnityEngine.UI;

public sealed class LevelManager : MonoBehaviour
{
    [Header("Quest Display")]
    [SerializeField] private LevelQuestHudDisplay questHud;
    [SerializeField] private LevelQuestIconProvider iconProvider;

    [Header("Inventory")]
    [SerializeField] private ExpeditionInventoryUI expeditionInventoryUI;
    [SerializeField] private ResourceGatherer resourceGatherer;
    [SerializeField] private OrcBloodDropHandler bloodDropHandler;
    [SerializeField] private GameObject gatherPanel;
    [SerializeField] private Scrollbar gatherProgressBar;

    [Header("Visibility")]
    [SerializeField] private VisibilitySystem visibilitySystem;

    [Header("Weather")]
    [SerializeField] private WeatherSystem weatherSystem;

    private PlayerInventory inventory;
    private PlayerQuestService questService;

    public PlayerQuestService GetQuestService() => questService;
    public PlayerInventory GetInventory() => inventory;

    private void Awake()
    {
        EnsureRuntimeComponents();

        inventory = ExpeditionManager.Instance.ExpeditionInventory;
        questService = new PlayerQuestService(
            new JsonPlayerQuestRepository(),
            new JsonQuestRepository()
        );

        WireInventorySystems();

        if (questHud != null)
            questHud.Initialize(questService, iconProvider, inventory);

        if (visibilitySystem == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                visibilitySystem = player.GetComponent<VisibilitySystem>();
                if (visibilitySystem == null) visibilitySystem = player.AddComponent<VisibilitySystem>();
            }
        }

        if (weatherSystem == null)
        {
            weatherSystem = FindFirstObjectByType<WeatherSystem>();
        }

        QuestManager.Instance.ResetExpeditionProgress();
    }

    private void EnsureRuntimeComponents()
    {
        if (resourceGatherer == null)
        {
            resourceGatherer = FindFirstObjectByType<ResourceGatherer>();
        }

        if (resourceGatherer == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                resourceGatherer = player.GetComponent<ResourceGatherer>();
                if (resourceGatherer == null)
                {
                    resourceGatherer = player.AddComponent<ResourceGatherer>();
                }

                if (player.GetComponent<GatherProgressDisplay>() == null)
                {
                    player.AddComponent<GatherProgressDisplay>();
                }

                GatherProgressDisplay gatherDisplay = player.GetComponent<GatherProgressDisplay>();
                if (gatherDisplay != null)
                {
                    gatherDisplay.Configure(gatherPanel, gatherProgressBar);
                }
            }
        }

        if (resourceGatherer != null)
        {
            GatherProgressDisplay gatherDisplay = resourceGatherer.GetComponent<GatherProgressDisplay>();
            if (gatherDisplay == null)
            {
                gatherDisplay = resourceGatherer.gameObject.AddComponent<GatherProgressDisplay>();
            }

            gatherDisplay.Configure(gatherPanel, gatherProgressBar);
            resourceGatherer.ConfigureProgressDisplay(gatherDisplay);
        }

        GatherableResourceInteraction.AttachToActiveSceneObjects();

        if (bloodDropHandler == null)
        {
            bloodDropHandler = FindFirstObjectByType<OrcBloodDropHandler>();
        }

        if (bloodDropHandler == null)
        {
            GameObject levelManagerObject = gameObject;
            bloodDropHandler = levelManagerObject.GetComponent<OrcBloodDropHandler>();
            if (bloodDropHandler == null)
            {
                bloodDropHandler = levelManagerObject.AddComponent<OrcBloodDropHandler>();
            }
        }

        if (expeditionInventoryUI == null)
        {
            expeditionInventoryUI = FindFirstObjectByType<ExpeditionInventoryUI>();
        }

        if (iconProvider == null)
        {
            iconProvider = FindFirstObjectByType<LevelQuestIconProvider>();
        }

        if (iconProvider == null)
        {
            iconProvider = gameObject.GetComponent<LevelQuestIconProvider>();
            if (iconProvider == null)
            {
                iconProvider = gameObject.AddComponent<LevelQuestIconProvider>();
            }
        }

        if (questHud == null)
        {
            questHud = FindFirstObjectByType<LevelQuestHudDisplay>();
        }
    }

    private void Start()
    {
        EnemyController.OnAnyEnemyDied += HandleEnemyDied;
        SubscribeToPlayerDeath();
        WireInventorySystems();

        ExpeditionManager expedition = ExpeditionManager.Instance;
        if (expedition != null)
        {
            expedition.OnExpeditionStarted += WireInventorySystems;
        }

        if (weatherSystem != null)
        {
            weatherSystem.OnWeatherChanged += HandleWeatherChanged;
        }
    }

    private void WireInventorySystems()
    {
        inventory = ExpeditionManager.Instance?.ExpeditionInventory;
        if (inventory == null)
        {
            ExpeditionItemTrace.Log("LevelManager.Wire", "ExpeditionInventory=NULL");
            return;
        }

        ExpeditionItemTrace.LogInventory(
            "LevelManager.Wire",
            inventory,
            $"gatherer={(resourceGatherer != null ? resourceGatherer.GetHashCode().ToString() : "NULL")} ui={(expeditionInventoryUI != null ? expeditionInventoryUI.GetHashCode().ToString() : "NULL")}");

        if (resourceGatherer != null)
        {
            resourceGatherer.Initialize(inventory);
        }

        if (bloodDropHandler != null)
        {
            bloodDropHandler.Initialize(inventory);
        }

        if (expeditionInventoryUI != null)
        {
            expeditionInventoryUI.Initialize(inventory, iconProvider);
        }
        else
        {
            ExpeditionInventoryUI runtimeUi = FindFirstObjectByType<ExpeditionInventoryUI>();
            if (runtimeUi != null)
            {
                runtimeUi.Initialize(inventory, iconProvider);
            }
        }

        if (questHud != null)
        {
            questHud.Initialize(questService, iconProvider, inventory);
        }
    }

    private void OnDestroy()
    {
        EnemyController.OnAnyEnemyDied -= HandleEnemyDied;

        ExpeditionManager expedition = ExpeditionManager.Instance;
        if (expedition != null)
        {
            expedition.OnExpeditionStarted -= WireInventorySystems;
        }

        if (weatherSystem != null)
        {
            weatherSystem.OnWeatherChanged -= HandleWeatherChanged;
        }
    }

    private void SubscribeToPlayerDeath()
    {
        PlayerHealth player = Object.FindAnyObjectByType<PlayerHealth>();
        if (player != null)
        {
            player.OnDeath += HandlePlayerDeath;
        }
    }

    private void HandlePlayerDeath()
    {
        ExpeditionManager.Instance.EndExpedition(ExpeditionResult.Death);
    }

    private readonly System.Collections.Generic.HashSet<int> trackedEnemies = new System.Collections.Generic.HashSet<int>();

    private void HandleEnemyDied(EnemyController enemy)
    {
        Debug.Log($"[LevelManager] Enemy Died: {enemy.name}");
        trackedEnemies.Remove(enemy.GetInstanceID());
        enemy.OnEnemyDied -= HandleEnemyDied;

        if (enemy.Config != null)
        {
            PlayerInventory pack = inventory ?? ExpeditionManager.Instance?.ExpeditionInventory;
            if (pack == null)
            {
                return;
            }

            inventory = pack;
            Debug.Log($"[LevelManager] Dropping loot for {enemy.Config.enemyName}. Loot table count: {enemy.Config.lootTable.Count}");

            if (enemy.Config.lootTable.Count == 0)
            {
                Debug.Log("[LevelManager] Loot table empty, adding default Orc Blood.");
                inventory.AddItem(ItemCatalog.OrcBlood, 1);
                QuestManager.Instance.ReportItemCollected(ItemCatalog.OrcBlood, 1);
            }
            else
            {
                foreach (var loot in enemy.Config.lootTable)
                {
                    if (UnityEngine.Random.value <= loot.chance)
                    {
                        int amount = UnityEngine.Random.Range(loot.minAmount, loot.maxAmount + 1);
                        if (amount > 0)
                        {
                            Debug.Log($"[LevelManager] Dropped {amount}x {loot.itemName}");
                            inventory.AddItem(loot.itemName, amount);
                            QuestManager.Instance.ReportItemCollected(loot.itemName, amount);
                        }
                    }
                }
            }

            if (enemy.Config.isBoss && !string.IsNullOrEmpty(enemy.Config.bossTrophyItemId))
            {
                inventory.AddItem(enemy.Config.bossTrophyItemId, 1);
                QuestManager.Instance.ReportItemCollected(enemy.Config.bossTrophyItemId, 1);
                Debug.Log($"[LevelManager] Boss trophy dropped: {enemy.Config.bossTrophyItemId}");
                QuestManager.Instance.ReportBossDefeated(enemy.Config.enemyName);
            }

            QuestManager.Instance.ReportEnemyKilled(enemy.Config.enemyName);
        }

        if (bloodDropHandler != null)
            bloodDropHandler.HandleEnemyKilled();
    }

    private void HandleWeatherChanged(WeatherSystem.WeatherType weather)
    {
        if (QuestManager.Instance == null) return;
        QuestManager.Instance.ReportWeatherChanged(weather);
    }
}
