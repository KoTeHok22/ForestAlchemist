using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Shared UIDocument bootstrap for Home App UI overlays (editor + build).
/// </summary>
public static class HomePanelUiLoader
{
    public static bool AssignAssets(
        UIDocument document,
        string resourceViewPath,
        string resourceSettingsPath,
        string editorViewPath,
        string editorSettingsPath)
    {
        if (document == null)
        {
            return false;
        }

        if (document.visualTreeAsset == null)
        {
            document.visualTreeAsset = Resources.Load<VisualTreeAsset>(resourceViewPath);
        }

        if (document.panelSettings == null)
        {
            document.panelSettings = Resources.Load<PanelSettings>(resourceSettingsPath);
        }

#if UNITY_EDITOR
        if (document.visualTreeAsset == null)
        {
            document.visualTreeAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(editorViewPath);
        }

        if (document.panelSettings == null)
        {
            document.panelSettings = UnityEditor.AssetDatabase.LoadAssetAtPath<PanelSettings>(editorSettingsPath);
        }
#endif

        return document.visualTreeAsset != null && document.panelSettings != null;
    }

    public static bool TryResolveShell(
        UIDocument document,
        out VisualElement root,
        out VisualElement panelRoot,
        out VisualElement dimBg)
    {
        root = document != null ? document.rootVisualElement : null;
        panelRoot = root != null ? root.Q<VisualElement>("panel-root") : null;
        dimBg = root != null ? root.Q<VisualElement>("dim-bg") : null;
        return panelRoot != null && dimBg != null;
    }
}
