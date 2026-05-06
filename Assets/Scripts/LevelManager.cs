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

    private PlayerInventory inventory;
    private PlayerQuestService questService;

    private void Awake()
    {
        inventory = new PlayerInventory();
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
    }

    private void Start()
    {
        SubscribeToEnemyDeaths();
    }

    private void SubscribeToEnemyDeaths()
    {
        EnemyBaseController[] bases = FindObjectsByType<EnemyBaseController>(FindObjectsSortMode.None);
        foreach (EnemyBaseController baseCtrl in bases)
        {
            // Listen via the existing event system - enemies are dynamically spawned so we
            // need to hook into the death flow. We'll use a global approach.
        }

        // Subscribe to all existing and future enemy deaths via a coroutine poll
        StartCoroutine(PollEnemyDeaths());
    }

    private System.Collections.IEnumerator PollEnemyDeaths()
    {
        while (true)
        {
            EnemyController[] enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
            foreach (EnemyController enemy in enemies)
            {
                if (!trackedEnemies.Contains(enemy.GetInstanceID()))
                {
                    trackedEnemies.Add(enemy.GetInstanceID());
                    enemy.OnEnemyDied += HandleEnemyDied;
                }
            }

            yield return new WaitForSeconds(1f);
        }
    }

    private readonly System.Collections.Generic.HashSet<int> trackedEnemies = new System.Collections.Generic.HashSet<int>();

    private void HandleEnemyDied(EnemyController enemy)
    {
        trackedEnemies.Remove(enemy.GetInstanceID());
        enemy.OnEnemyDied -= HandleEnemyDied;

        if (bloodDropHandler != null)
            bloodDropHandler.HandleEnemyKilled();
    }
}
