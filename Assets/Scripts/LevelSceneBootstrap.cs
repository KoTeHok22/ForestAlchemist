using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

public sealed class LevelSceneBootstrap : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private WeatherSystem weatherSystem;
    [SerializeField] private PlayerSpellCaster spellCaster;

    private void Awake()
    {
        EnsureServices();
        EnsureExpeditionSession();
        EnsureEventSystem();
        SpawnPlayerIfNeeded();
        EnsureLevelManager();
        EnsureWeatherSystem();
        EnsureHealthDisplay();

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

    private static void EnsureExpeditionSession()
    {
        if (SceneManager.GetActiveScene().name == "Level" && !ExpeditionManager.Instance.IsInExpedition)
        {
            ExpeditionManager.Instance.EnterCurrentLevelExpedition();
        }
    }

    private static void EnsureHealthDisplay()
    {
        GameObject healthPanel = GameObject.Find("Canvas/Main/PlayerInfo/HealthPanel");
        if (healthPanel == null)
        {
            return;
        }

        if (healthPanel.GetComponent<PlayerHealthDisplay>() == null)
        {
            healthPanel.AddComponent<PlayerHealthDisplay>();
        }
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
    }
}
