using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public sealed class MinimapOverlayBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapAfterSceneLoad()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != "Level")
        {
            return;
        }

        EnsureMinimapOverlay();
    }

    public static void EnsureMinimapOverlay()
    {
        if (Object.FindFirstObjectByType<MinimapAppUIController>() != null)
        {
            return;
        }

        VisualTreeAsset view = Resources.Load<VisualTreeAsset>("UI/Minimap/MinimapView");
        PanelSettings panelSettings = Resources.Load<PanelSettings>("UI/Minimap/MinimapPanelSettings");
        if (view == null || panelSettings == null)
        {
            Debug.LogWarning("[MinimapOverlayBootstrap] Minimap assets missing in Resources/UI/Minimap/");
            return;
        }

        GameObject go = new GameObject("MinimapOverlay");
        UIDocument document = go.AddComponent<UIDocument>();
        document.panelSettings = panelSettings;
        document.visualTreeAsset = view;
        go.AddComponent<MinimapAppUIController>();
    }
}
