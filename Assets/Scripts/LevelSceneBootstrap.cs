using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

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
        EnsureExpeditionInventoryUI();
        EnsureLevelManager();
        WireExpeditionInventoryUI();
        EnsureInventoryToggleInput();
        MinimapOverlayBootstrap.EnsureMinimapOverlay();
        EnsureWeatherSystem();
        EnsureWeatherDisplay();
        EnsureHealthDisplay();
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

        if (player.GetComponent<PlayerCombatController>() == null)
        {
            player.AddComponent<PlayerCombatController>();
        }

        if (player.GetComponent<PlayerStatApplicator>() == null)
        {
            player.AddComponent<PlayerStatApplicator>();
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

    private static void EnsureExpeditionInventoryUI()
    {
        ExpeditionInventoryUI ui = FindFirstObjectByType<ExpeditionInventoryUI>();
        if (ui != null) return;

        GameObject host = new GameObject("ExpeditionInventoryUI");
        host.AddComponent<ExpeditionInventoryUI>();
    }

    private static void WireExpeditionInventoryUI()
    {
        ExpeditionInventoryUI ui = FindFirstObjectByType<ExpeditionInventoryUI>();
        if (ui == null) return;

        LevelQuestIconProvider iconProvider = FindFirstObjectByType<LevelQuestIconProvider>();
        ui.Initialize(ExpeditionManager.Instance.ExpeditionInventory, iconProvider);
    }

    private static void EnsureInventoryToggleInput()
    {
        ExpeditionInventoryUI ui = FindFirstObjectByType<ExpeditionInventoryUI>();
        if (ui == null) return;

        if (ui.GetComponent<InventoryToggleInput>() == null)
        {
            ui.gameObject.AddComponent<InventoryToggleInput>();
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

        if (weatherSystem.GetComponent<WeatherVisualController>() == null)
        {
            weatherSystem.gameObject.AddComponent<WeatherVisualController>();
        }

        if (weatherSystem.GetComponent<WeatherDebugInput>() == null)
        {
            weatherSystem.gameObject.AddComponent<WeatherDebugInput>();
        }
    }

    private static void EnsureWeatherDisplay()
    {
        DisableLegacyWeatherPanel();

        WeatherDisplay[] displays = FindObjectsByType<WeatherDisplay>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < displays.Length; i++)
        {
            WeatherDisplay display = displays[i];
            if (display != null && display.GetComponent<UIDocument>() != null)
            {
                return;
            }
        }

        GameObject host = new GameObject("WeatherDisplay");
        host.AddComponent<UIDocument>();
        host.AddComponent<WeatherDisplay>();
    }

    private static void DisableLegacyWeatherPanel()
    {
        GameObject legacy = GameObject.Find("Canvas/Weather");
        if (legacy != null)
        {
            legacy.SetActive(false);
        }

        WeatherDisplay[] displays = FindObjectsByType<WeatherDisplay>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < displays.Length; i++)
        {
            WeatherDisplay display = displays[i];
            if (display == null || display.GetComponent<UIDocument>() != null)
            {
                continue;
            }

            if (display.GetComponentInParent<Canvas>() != null)
            {
                display.enabled = false;
                display.gameObject.SetActive(false);
            }
        }
    }

    private void EnsureServices()
    {
        GameCore.Instance.GetHashCode();
        ExpeditionManager.Instance.GetHashCode();
        InventoryService.Instance.GetHashCode();
        QuestManager.Instance.GetHashCode();
        PlayerUpgradeService.Instance.GetHashCode();
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
