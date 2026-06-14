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
    private VisualElement screenControls;

    private VisualElement btnResume;
    private VisualElement btnSettings;
    private VisualElement btnSave;
    private VisualElement btnSaveExit;
    private VisualElement btnSettingsClose;
    private VisualElement btnOpenControls;

    private SettingsPanelController settings;
    private ControlsSettingsPanelController controls;
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
        controls = new ControlsSettingsPanelController(root, GameCore.Instance.AccountService);
        controls.OnBackRequested = CloseControlsToSettings;
        settings.PushCurrent();

        CloseAllImmediate();
    }

    private void OnDisable()
    {
        controls?.CancelRebind();
        CloseAllImmediate();
    }

    private void CacheElements()
    {
        panelRoot        = root.Q<VisualElement>("panel-root");
        screenPause      = root.Q<VisualElement>("screen-pause");
        screenSettings   = root.Q<VisualElement>("screen-settings");
        screenControls   = root.Q<VisualElement>("screen-controls");
        btnResume        = root.Q<VisualElement>("btn-resume");
        btnSettings      = root.Q<VisualElement>("btn-settings");
        btnSave          = root.Q<VisualElement>("btn-save");
        btnSaveExit      = root.Q<VisualElement>("btn-save-exit");
        btnSettingsClose = root.Q<VisualElement>("btn-settings-close");
        btnOpenControls  = root.Q<VisualElement>("btn-open-controls");
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
        clickRouter.Add(btnOpenControls, OpenControlsFromSettings);
    }

    private void Update()
    {
        if (Keyboard.current == null || !GameControls.WasPressedThisFrame(ControlBindingId.Pause))
        {
            return;
        }

        if (GameControls.IsListeningForRebind)
        {
            return;
        }

        if (isPaused)
        {
            if (IsControlsOpen())
            {
                CloseControlsToSettings();
                return;
            }

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
        SetVisible(screenControls, false);
        if (panelRoot != null) panelRoot.pickingMode = PickingMode.Position;

        settings.PushCurrent();
        isPaused = true;
        Time.timeScale = 0f;
        AudioHooks.PanelOpen();
        AudioHooks.Manager?.SetPauseMusicDucked(true);
    }

    public void Resume() => CloseAllImmediate();

    public void OpenSettings()
    {
        if (!isPaused) OpenPause();

        SetVisible(screenPause, false);
        SetVisible(screenSettings, true);
        SetVisible(screenControls, false);
        settings.PushCurrent();
    }

    public void CloseSettings()
    {
        controls?.CancelRebind();
        SetVisible(screenSettings, false);
        SetVisible(screenControls, false);
        SetVisible(screenPause, true);
    }

    private void OpenControlsFromSettings()
    {
        controls?.Refresh();
        SetVisible(screenSettings, false);
        SetVisible(screenControls, true);
    }

    private void CloseControlsToSettings()
    {
        controls?.CancelRebind();
        SetVisible(screenControls, false);
        SetVisible(screenSettings, true);
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

    private bool IsControlsOpen()
    {
        return screenControls != null && screenControls.style.display == DisplayStyle.Flex;
    }

    private void CloseAllImmediate()
    {
        controls?.CancelRebind();
        SetVisible(screenPause, false);
        SetVisible(screenSettings, false);
        SetVisible(screenControls, false);

        if (isPaused && !HomeUIBlocker.IsBlocked)
        {
            Time.timeScale = 1f;
        }

        isPaused = false;
        if (panelRoot != null) panelRoot.pickingMode = PickingMode.Ignore;
        AudioHooks.PanelClose();
        AudioHooks.Manager?.SetPauseMusicDucked(false);
    }

    private static void SetVisible(VisualElement element, bool visible)
    {
        if (element == null) return;
        element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        element.EnableInClassList("hidden", !visible);
    }
}
