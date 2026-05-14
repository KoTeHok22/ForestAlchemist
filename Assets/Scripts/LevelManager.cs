using UnityEngine;
using TMPro;

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

    private TMP_Text expeditionHintText;
    private TMP_Text combatFeedText;
    private float combatFeedTimer;

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
        EnsureExpeditionHint();

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
                inventory.AddItem(ItemCatalog.OrcBlood, 1);
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
                ShowCombatFeed($"Босс повержен. Получен трофей: {enemy.Config.bossTrophyItemId}");
            }
            else if (enemy.Config.isShaman)
            {
                ShowCombatFeed("Шаман орков повержен. Его талисман может выпасть в добычу.");
            }
            else
            {
                ShowCombatFeed($"Побеждён враг: {enemy.Config.enemyName}");
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

    private void Update()
    {
        if (expeditionHintText == null)
        {
            return;
        }

        expeditionHintText.text = ExpeditionManager.Instance.CanReturn()
            ? $"Выход доступен: {ExpeditionManager.Instance.ActiveReturnMethod}. Можно возвращаться с добычей."
            : "Выход закрыт. Ищи портал, свиток возврата или точку эвакуации.";

        if (combatFeedText != null && combatFeedTimer > 0f)
        {
            combatFeedTimer -= Time.deltaTime;
            if (combatFeedTimer <= 0f)
            {
                combatFeedText.text = string.Empty;
            }
        }
    }

    private void EnsureExpeditionHint()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            return;
        }

        GameObject hintObject = new GameObject("ExpeditionHint", typeof(RectTransform));
        hintObject.transform.SetParent(canvas.transform, false);
        RectTransform rect = hintObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 18f);
        rect.sizeDelta = new Vector2(780f, 44f);

        expeditionHintText = hintObject.AddComponent<TextMeshProUGUI>();
        expeditionHintText.fontSize = 20f;
        expeditionHintText.alignment = TextAlignmentOptions.Center;
        expeditionHintText.color = new Color(1f, 0.96f, 0.82f, 1f);
        expeditionHintText.enableWordWrapping = true;

        GameObject feedObject = new GameObject("CombatFeed", typeof(RectTransform));
        feedObject.transform.SetParent(canvas.transform, false);
        RectTransform feedRect = feedObject.GetComponent<RectTransform>();
        feedRect.anchorMin = new Vector2(0.5f, 0f);
        feedRect.anchorMax = new Vector2(0.5f, 0f);
        feedRect.pivot = new Vector2(0.5f, 0f);
        feedRect.anchoredPosition = new Vector2(0f, 70f);
        feedRect.sizeDelta = new Vector2(760f, 40f);

        combatFeedText = feedObject.AddComponent<TextMeshProUGUI>();
        combatFeedText.fontSize = 18f;
        combatFeedText.alignment = TextAlignmentOptions.Center;
        combatFeedText.color = new Color(0.95f, 0.8f, 0.35f, 1f);
        combatFeedText.enableWordWrapping = true;
    }

    private void ShowCombatFeed(string message)
    {
        if (combatFeedText == null || string.IsNullOrEmpty(message))
        {
            return;
        }

        combatFeedText.text = message;
        combatFeedTimer = 4f;
    }
}
