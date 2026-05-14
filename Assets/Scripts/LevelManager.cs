using UnityEngine;

public sealed class LevelManager : MonoBehaviour
{
    [Header("Quest Display")]
    [SerializeField] private LevelQuestHudDisplay questHud;
    [SerializeField] private LevelQuestIconProvider iconProvider;

    [Header("Inventory")]
    [SerializeField] private InventoryDisplay inventoryDisplay;
    [SerializeField] private ResourceGatherer resourceGatherer;
    [SerializeField] private OrcBloodDropHandler bloodDropHandler;

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
        inventory = ExpeditionManager.Instance.ExpeditionInventory;
        questService = new PlayerQuestService(
            new JsonPlayerQuestRepository(),
            new JsonQuestRepository()
        );

        if (resourceGatherer != null)
            resourceGatherer.Initialize(inventory);

        if (bloodDropHandler != null)
            bloodDropHandler.Initialize(inventory);

        if (inventoryDisplay != null)
            inventoryDisplay.Initialize(inventory, iconProvider);

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

    private void Start()
    {
        EnemyController.OnAnyEnemyDied += HandleEnemyDied;
        SubscribeToPlayerDeath();

        if (weatherSystem != null)
        {
            weatherSystem.OnWeatherChanged += HandleWeatherChanged;
        }
    }

    private void OnDestroy()
    {
        EnemyController.OnAnyEnemyDied -= HandleEnemyDied;

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

        if (enemy.Config != null && inventory != null)
        {
            Debug.Log($"[LevelManager] Dropping loot for {enemy.Config.enemyName}. Loot table count: {enemy.Config.lootTable.Count}");

            if (enemy.Config.lootTable.Count == 0)
            {
                Debug.Log("[LevelManager] Loot table empty, adding default Orc Blood.");
                inventory.AddItem("КровьОрка", 1);
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
                        }
                    }
                }
            }

            if (enemy.Config.isBoss && !string.IsNullOrEmpty(enemy.Config.bossTrophyItemId))
            {
                inventory.AddItem(enemy.Config.bossTrophyItemId, 1);
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
        QuestManager.Instance.ReportWeatherChanged(weather);
    }
}
