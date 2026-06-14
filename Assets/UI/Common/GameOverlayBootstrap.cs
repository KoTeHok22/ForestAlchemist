using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Instantiates the shared App UI game overlay (Pause + Load [+ HUD]) on scene
/// start and disables the legacy uGUI Canvas panels it replaces. Attach to any
/// in-game scene (Home / Level). Idempotent: only one overlay is created.
/// </summary>
public sealed class GameOverlayBootstrap : MonoBehaviour
{
    private const string OverlayResourcePath = "UI/GameOverlayUI";

    [SerializeField] private GameObject overlayPrefab;

    private void Awake()
    {
        if (FindFirstObjectByType<PauseAppUIController>() == null)
        {
            GameObject prefab = overlayPrefab != null ? overlayPrefab : Resources.Load<GameObject>(OverlayResourcePath);
            if (prefab != null)
            {
                Instantiate(prefab);
            }
            else
            {
                Debug.LogWarning("[GameOverlayBootstrap] overlay prefab not found at Resources/" + OverlayResourcePath);
            }
        }

        DisableLegacyCanvasPanels();
    }

    private void Start()
    {
        // Some legacy quest trackers build their runtime uGUI panel in Awake/Initialize,
        // which can land after our Awake. Re-run the disable in Start, once every
        // Awake has executed, so the App UI HUD tracker stays the only one shown.
        DisableLegacyCanvasPanels();
    }

    private static void DisableLegacyCanvasPanels()
    {
        // The legacy PauseMenuController lives on the Canvas itself and listens
        // for ESC in Update(); disable the component so it does not fight the
        // App UI pause for input.
        PauseMenuController legacyPause = FindFirstObjectByType<PauseMenuController>();
        if (legacyPause != null)
        {
            legacyPause.enabled = false;
        }

        DisableByPath("Canvas/Pause");
        DisableByPath("Canvas/Load");

        // Legacy HUD sub-objects replaced by the App UI HUD.
        DisableByPath("Canvas/Main/PlayerInfo");
        DisableByPath("Canvas/Main/Abilities");
        DisableByPath("Canvas/Main/Tasks");

        // Legacy expedition result (replaced by ExpeditionResultAppUI).
        ExpeditionResultUI legacyResult = FindFirstObjectByType<ExpeditionResultUI>();
        if (legacyResult != null) legacyResult.enabled = false;
        DisableByPath("Canvas/ExpeditionResultPanel");

        // Legacy shop panel (ShopUI component repurposed to App UI; only the old panel is hidden).
        DisableByPath("Canvas/ShopUI/ShopPanel");

        // Legacy quest board panel (replaced by App UI DeskBoardAppUI).
        DisableByPath("Canvas/Desk");

        // Legacy active-quest trackers (replaced by the App UI HUD quest tracker).
        // Disabling the components stops them re-rendering; the runtime uGUI panel
        // some of them build is hidden by name below.
        DisableComponent<ActiveQuestPanel>();
        DisableComponent<ActiveQuestHudDisplay>();
        DisableComponent<LevelQuestHudDisplay>();
        DisableByPath("RuntimeQuestPanel");

        // Legacy expedition inventory (replaced by App UI ExpeditionInventoryUI).
        DisableLegacyExpeditionInventory();

        DisableLegacyWeatherDisplay();
    }

    private static void DisableLegacyWeatherDisplay()
    {
        DisableByPath("Canvas/Weather");

        WeatherDisplay[] displays = Object.FindObjectsByType<WeatherDisplay>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < displays.Length; i++)
        {
            WeatherDisplay display = displays[i];
            if (display == null || display.GetComponent<UIDocument>() != null)
            {
                continue;
            }

            if (display.GetComponentInParent<Canvas>() == null)
            {
                continue;
            }

            display.enabled = false;
            display.gameObject.SetActive(false);
        }
    }

    private static void DisableLegacyExpeditionInventory()
    {
        DisableByPath("Canvas/Main/InventoryDisplay");
        DisableByPath("Canvas/InventoryDisplay");

        InventoryDisplay[] legacyDisplays = Object.FindObjectsByType<InventoryDisplay>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < legacyDisplays.Length; i++)
        {
            InventoryDisplay display = legacyDisplays[i];
            if (display == null) continue;
            if (display.GetComponentInParent<Canvas>() == null) continue;

            display.enabled = false;
            display.gameObject.SetActive(false);
        }
    }

    private static void DisableComponent<T>() where T : Behaviour
    {
        T component = FindFirstObjectByType<T>();
        if (component != null)
        {
            component.enabled = false;
        }
    }

    private static void DisableByPath(string path)
    {
        GameObject go = GameObject.Find(path);
        if (go != null)
        {
            go.SetActive(false);
        }
    }
}
