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
        screenPause      = root.Q<VisualElement>("screen-pause");
        screenSettings   = root.Q<VisualElement>("screen-settings");
        btnResume        = root.Q<VisualElement>("btn-resume");
        btnSettings      = root.Q<VisualElement>("btn-settings");
        btnSave          = root.Q<VisualElement>("btn-save");
        btnSaveExit      = root.Q<VisualElement>("btn-save-exit");
        btnSettingsClose = root.Q<VisualElement>("btn-settings-close");
    }

    private void BindButtons()
    {
        BindClick(btnResume, Resume);
        BindClick(btnSettings, OpenSettings);
        BindClick(btnSave, Save);
        BindClick(btnSaveExit, SaveAndExit);
        BindClick(btnSettingsClose, CloseSettings);
    }

    private static void BindClick(VisualElement element, System.Action callback)
    {
        if (element == null || callback == null) return;
        element.RegisterCallback<ClickEvent>(_ => callback());
        element.pickingMode = PickingMode.Position;
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
    }

    private static void SetVisible(VisualElement element, bool visible)
    {
        if (element == null) return;
        element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
