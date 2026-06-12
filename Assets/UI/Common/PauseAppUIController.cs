using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

/// <summary>
/// App UI replacement for the legacy uGUI <c>PauseMenuController</c>.
/// Lives on a UIDocument created by the scene bootstrap (Home / Level).
/// ESC toggles pause; pausing sets <see cref="Time.timeScale"/> to 0.
/// Settings reuse the shared <see cref="SettingsPanelController"/>.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public sealed class PauseAppUIController : MonoBehaviour
{
    private UIDocument document;
    private VisualElement root;

    private VisualElement panelRoot;
    private VisualElement screenPause;
    private VisualElement screenSettings;

    private VisualElement btnResume;
    private VisualElement btnSettings;
    private VisualElement btnSave;
    private VisualElement btnSaveExit;
    private VisualElement btnSettingsClose;

    private SettingsPanelController settings;
    private readonly UnityMenuSettingsApplier settingsApplier = new UnityMenuSettingsApplier();

    private bool isPaused;

    private void OnEnable()
    {
        if (document == null) document = GetComponent<UIDocument>();
        root = document.rootVisualElement;
        if (root == null) return;

        CacheElements();
        BindButtons();

        settings = new SettingsPanelController(root, settingsApplier, GameCore.Instance.AccountService);
        settings.PushCurrent();

        CloseAllImmediate();
    }

    private void OnDisable()
    {
        CloseAllImmediate();
    }

    private void CacheElements()
    {
        panelRoot        = root.Q<VisualElement>("panel-root");
        screenPause      = root.Q<VisualElement>("screen-pause");
        screenSettings   = root.Q<VisualElement>("screen-settings");
        btnResume        = root.Q<VisualElement>("btn-resume");
        btnSettings      = root.Q<VisualElement>("btn-settings");
        btnSave          = root.Q<VisualElement>("btn-save");
        btnSaveExit      = root.Q<VisualElement>("btn-save-exit");
        btnSettingsClose = root.Q<VisualElement>("btn-settings-close");
    }

    private AppUIClickRouter clickRouter;

    private void BindButtons()
    {
        clickRouter = new AppUIClickRouter(root);
        clickRouter.Add(btnResume, Resume);
        clickRouter.Add(btnSettings, OpenSettings);
        clickRouter.Add(btnSave, Save);
        clickRouter.Add(btnSaveExit, SaveAndExit);
        clickRouter.Add(btnSettingsClose, CloseSettings);
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            return;
        }

        if (isPaused)
        {
            if (IsSettingsOpen())
            {
                CloseSettings();
                return;
            }

            Resume();
            return;
        }

        if (Mathf.Approximately(Time.timeScale, 0f))
        {
            return;
        }

        OpenPause();
    }

    public void OpenPause()
    {
        if (isPaused) return;

        SetVisible(screenPause, true);
        SetVisible(screenSettings, false);
        if (panelRoot != null) panelRoot.pickingMode = PickingMode.Position;

        settings.PushCurrent();
        isPaused = true;
        Time.timeScale = 0f;
    }

    public void Resume() => CloseAllImmediate();

    public void OpenSettings()
    {
        if (!isPaused) OpenPause();

        SetVisible(screenPause, false);
        SetVisible(screenSettings, true);
        settings.PushCurrent();
    }

    public void CloseSettings()
    {
        SetVisible(screenSettings, false);
        SetVisible(screenPause, true);
    }

    public void Save()
    {
        GameCore.Instance.SaveProgress();
    }

    public void SaveAndExit()
    {
        GameCore.Instance.SaveProgress();
        CloseAllImmediate();
        GameCore.Instance.ReturnToMainMenu();
    }

    private bool IsSettingsOpen()
    {
        return screenSettings != null && screenSettings.style.display == DisplayStyle.Flex;
    }

    private void CloseAllImmediate()
    {
        SetVisible(screenPause, false);
        SetVisible(screenSettings, false);

        if (isPaused)
        {
            Time.timeScale = 1f;
        }

        isPaused = false;
        // Otherwise the Panel keeps eating all pointer events on top of HUD.
        if (panelRoot != null) panelRoot.pickingMode = PickingMode.Ignore;
    }

    private static void SetVisible(VisualElement element, bool visible)
    {
        if (element == null) return;
        element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
