using UnityEngine;

public sealed class LevelSceneBootstrap : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private WeatherSystem weatherSystem;
    [SerializeField] private PlayerSpellCaster spellCaster;

    private void Awake()
    {
        EnsureServices();
        SpawnPlayerIfNeeded();
        EnsureLevelManager();
        EnsureWeatherSystem();

        QuestManager.Instance.ResetExpeditionProgress();
    }

    private void SpawnPlayerIfNeeded()
    {
        GameObject existing = GameObject.FindGameObjectWithTag("Player");
        if (existing != null)
        {
            EnsurePlayerRuntimeComponents(existing);
            return;
        }

        if (playerPrefab != null)
        {
            GameObject player = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
            player.tag = "Player";
            player.name = "Player";
            EnsurePlayerRuntimeComponents(player);
        }
    }

    private static void EnsurePlayerRuntimeComponents(GameObject player)
    {
        if (player.GetComponent<PlayerSpellCaster>() == null)
        {
            player.AddComponent<PlayerSpellCaster>();
        }

        if (player.GetComponent<PlayerBuffReceiver>() == null)
        {
            player.AddComponent<PlayerBuffReceiver>();
        }

        if (player.GetComponent<VisibilitySystem>() == null)
        {
            player.AddComponent<VisibilitySystem>();
        }
    }

    private void EnsureLevelManager()
    {
        if (levelManager == null) levelManager = FindFirstObjectByType<LevelManager>();
        if (levelManager == null)
        {
            GameObject go = new GameObject("LevelManager");
            levelManager = go.AddComponent<LevelManager>();
        }
    }

    private void EnsureWeatherSystem()
    {
        if (weatherSystem == null) weatherSystem = FindFirstObjectByType<WeatherSystem>();
        if (weatherSystem == null)
        {
            GameObject go = new GameObject("WeatherSystem");
            weatherSystem = go.AddComponent<WeatherSystem>();
        }
    }

    private void EnsureServices()
    {
        GameCore.Instance.GetHashCode();
        ExpeditionManager.Instance.GetHashCode();
        InventoryService.Instance.GetHashCode();
        QuestManager.Instance.GetHashCode();
    }
}
