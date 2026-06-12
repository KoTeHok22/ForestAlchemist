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
    private const string ChildName = "DeskOverlay_AppUI";

    private UIDocument document;
    private VisualElement root;
    private VisualElement panelRoot;
    private VisualElement dimBg;
    private VisualElement tasksGrid;
    private AppUIClickRouter clickRouter;

    private bool built;
    private System.Action onCloseRequested;

    public VisualElement TasksGrid => tasksGrid;
    public AppUIClickRouter ClickRouter => clickRouter;

    public void Open(System.Action onClose)
    {
        EnsureBuilt();
        if (root == null) return;
        onCloseRequested = onClose;
        if (dimBg != null) dimBg.style.display = DisplayStyle.Flex;
        if (panelRoot != null) panelRoot.pickingMode = PickingMode.Position;
    }

    public void Close()
    {
        if (dimBg != null) dimBg.style.display = DisplayStyle.None;
        if (panelRoot != null) panelRoot.pickingMode = PickingMode.Ignore;
        var cb = onCloseRequested;
        onCloseRequested = null;
        cb?.Invoke();
    }

    private void EnsureBuilt()
    {
        if (built && document != null) return;

        // Host the UIDocument on a dedicated child GO so it does not share
        // a Transform with the board's SpriteRenderer/Collider2D (those caused
        // pointer events to get eaten by the world-space hit-test).
        Transform existing = transform.Find(ChildName);
        GameObject host;
        if (existing != null)
        {
            host = existing.gameObject;
        }
        else
        {
            host = new GameObject(ChildName);
            host.transform.SetParent(null, false); // detach from board sprite scaling
        }

        document = host.GetComponent<UIDocument>();
        if (document == null) document = host.AddComponent<UIDocument>();

#if UNITY_EDITOR
        if (document.visualTreeAsset == null)
            document.visualTreeAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ViewPath);
        if (document.panelSettings == null)
            document.panelSettings = UnityEditor.AssetDatabase.LoadAssetAtPath<PanelSettings>(SettingsPath);
#endif

        root = document.rootVisualElement;
        if (root == null) return;

        panelRoot = root.Q<VisualElement>("panel-root");
        dimBg = root.Q<VisualElement>("dim-bg");
        tasksGrid = root.Q<VisualElement>("tasks-grid");

        clickRouter = new AppUIClickRouter(root);
        var closeX = root.Q<VisualElement>("btn-close-x");
        if (closeX != null) clickRouter.Add(closeX, Close);

        if (dimBg != null) dimBg.style.display = DisplayStyle.None;
        if (panelRoot != null) panelRoot.pickingMode = PickingMode.Ignore;
        built = true;
    }
}
