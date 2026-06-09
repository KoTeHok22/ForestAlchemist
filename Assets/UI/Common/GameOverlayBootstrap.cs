using UnityEngine;

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
