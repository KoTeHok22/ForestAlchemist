using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// App UI doska bulletin board for quests. Replaces the legacy Canvas/Desk panel
/// (kept disabled). The UIDocument lives on a dedicated child GameObject so it
/// does not conflict with the SpriteRenderer/Collider2D of the board sprite.
/// </summary>
public sealed class DeskBoardAppUI : MonoBehaviour
{
    private const string ViewPath = "Assets/UI/HomePanels/DeskView.uxml";
    private const string SettingsPath = "Assets/UI/HomePanels/DeskPanelSettings.asset";
    private const string ViewResourcePath = "UI/HomePanels/DeskView";
    private const string SettingsResourcePath = "UI/HomePanels/DeskPanelSettings";
    private const string ChildName = "DeskOverlay_AppUI";

    private UIDocument document;
    private VisualElement root;
    private VisualElement panelRoot;
    private VisualElement dimBg;
    private VisualElement tasksGrid;
    private AppUIClickRouter clickRouter;

    private bool built;
    private bool isOpen;
    private System.Action onCloseRequested;

    public VisualElement TasksGrid => tasksGrid;
    public AppUIClickRouter ClickRouter => clickRouter;

    private void OnDestroy()
    {
        if (isOpen)
        {
            HomeUIBlocker.Release();
            isOpen = false;
        }
    }

    public bool IsOpen => isOpen;

    public void Open(System.Action onClose)
    {
        if (!EnsureBuilt())
        {
            Debug.LogError("[DeskBoardAppUI] Не удалось открыть доску квестов: UI не собран.");
            return;
        }

        onCloseRequested = onClose;
        if (dimBg != null) dimBg.style.display = DisplayStyle.Flex;
        if (panelRoot != null) panelRoot.pickingMode = PickingMode.Position;
        if (!isOpen)
        {
            HomeUIBlocker.Acquire();
            isOpen = true;
            AudioHooks.Sfx(AudioClipId.SfxHomeQuestBoardRustle);
            AudioHooks.PanelOpen();
        }
    }

    public void Close()
    {
        if (dimBg != null) dimBg.style.display = DisplayStyle.None;
        if (panelRoot != null) panelRoot.pickingMode = PickingMode.Ignore;
        if (isOpen)
        {
            HomeUIBlocker.Release();
            isOpen = false;
            AudioHooks.PanelClose();
        }

        var cb = onCloseRequested;
        onCloseRequested = null;
        cb?.Invoke();
    }

    private bool EnsureBuilt()
    {
        if (built && panelRoot != null && dimBg != null)
        {
            return true;
        }

        Transform existing = transform.Find(ChildName);
        GameObject host;
        if (existing != null)
        {
            host = existing.gameObject;
        }
        else
        {
            host = new GameObject(ChildName);
            host.transform.SetParent(null, false);
        }

        document = host.GetComponent<UIDocument>();
        if (document == null)
        {
            document = host.AddComponent<UIDocument>();
        }

        if (!HomePanelUiLoader.AssignAssets(document, ViewResourcePath, SettingsResourcePath, ViewPath, SettingsPath))
        {
            Debug.LogError("[DeskBoardAppUI] visualTreeAsset или panelSettings не найдены.");
            return false;
        }

        if (!HomePanelUiLoader.TryResolveShell(document, out root, out panelRoot, out dimBg))
        {
            Debug.LogError("[DeskBoardAppUI] Разметка панели неполная (panel-root/dim-bg).");
            return false;
        }

        tasksGrid = root.Q<VisualElement>("tasks-grid");

        clickRouter = new AppUIClickRouter(root);
        VisualElement closeX = root.Q<VisualElement>("btn-close-x");
        if (closeX != null) clickRouter.Add(closeX, Close);

        dimBg.style.display = DisplayStyle.None;
        panelRoot.pickingMode = PickingMode.Ignore;
        built = true;
        return true;
    }
}
