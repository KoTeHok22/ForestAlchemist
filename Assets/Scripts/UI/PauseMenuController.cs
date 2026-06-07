using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class PauseMenuController : MonoBehaviour
{
    private const string PausePanelPath = "Pause";
    private const string PauseContentPath = "Pause/Content";
    private const string ResumeButtonPath = "Pause/Content/Resume";
    private const string SettingsButtonPath = "Pause/Content/Settings";
    private const string SaveButtonPath = "Pause/Content/Save";
    private const string SaveAndExitButtonPath = "Pause/Content/SaveAndExit";
    private const string SettingsPanelPath = "Pause/Settings";
    private const string SettingsCloseButtonPath = "Pause/Settings/Content/Close";
    private const string ResolutionDropdownPath = "Pause/Settings/Content/Settings/Setting/Panel (1)/Dropdown";
    private const string QualityDropdownPath = "Pause/Settings/Content/Settings/Setting (1)/Panel (1)/Dropdown";
    private const string MusicTogglePath = "Pause/Settings/Content/Settings/Setting (2)/Panel (1)/Toggle";
    private const string SfxTogglePath = "Pause/Settings/Content/Settings/Setting (3)/Panel (1)/Toggle";
    private const string WindowedModeTogglePath = "Pause/Settings/Content/Settings/Setting (4)/Panel (1)/Toggle";
    private const string MusicSliderPath = "Pause/Settings/Content/Settings/Setting (2)/Panel (1)/Slider";
    private const string SfxSliderPath = "Pause/Settings/Content/Settings/Setting (3)/Panel (1)/Slider";

    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject pauseContent;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button saveAndExitButton;
    [SerializeField] private Button settingsCloseButton;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private Toggle musicToggle;
    [SerializeField] private Toggle sfxToggle;
    [SerializeField] private Toggle windowedModeToggle;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    private readonly UnityMenuSettingsApplier settingsApplier = new UnityMenuSettingsApplier();
    private bool isPaused;
    private bool isApplyingSettings;

    private void Awake()
    {
        CacheReferences();
        BindButtons();
        BindSettingsControls();
        CloseAllImmediate();
        PushCurrentSettings();
    }

    private void OnEnable()
    {
        CloseAllImmediate();
        PushCurrentSettings();
    }

    private void OnDisable()
    {
        CloseAllImmediate();
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
        if (pausePanel == null || isPaused)
        {
            return;
        }

        pausePanel.SetActive(true);
        if (pauseContent != null)
        {
            pauseContent.SetActive(true);
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        PushCurrentSettings();
        isPaused = true;
        Time.timeScale = 0f;
    }

    public void Resume()
    {
        CloseAllImmediate();
    }

    public void OpenSettings()
    {
        if (!isPaused)
        {
            OpenPause();
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }

        if (pauseContent != null)
        {
            pauseContent.SetActive(false);
        }

        PushCurrentSettings();
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        if (pauseContent != null)
        {
            pauseContent.SetActive(true);
        }
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

    private void CacheReferences()
    {
        pausePanel = pausePanel != null ? pausePanel : FindChildObject(PausePanelPath);
        pauseContent = pauseContent != null ? pauseContent : FindChildObject(PauseContentPath);
        settingsPanel = settingsPanel != null ? settingsPanel : FindChildObject(SettingsPanelPath);
        resumeButton = resumeButton != null ? resumeButton : FindChildComponent<Button>(ResumeButtonPath);
        settingsButton = settingsButton != null ? settingsButton : FindChildComponent<Button>(SettingsButtonPath);
        saveButton = saveButton != null ? saveButton : FindChildComponent<Button>(SaveButtonPath);
        saveAndExitButton = saveAndExitButton != null ? saveAndExitButton : FindChildComponent<Button>(SaveAndExitButtonPath);
        settingsCloseButton = settingsCloseButton != null ? settingsCloseButton : FindChildComponent<Button>(SettingsCloseButtonPath);
        resolutionDropdown = resolutionDropdown != null ? resolutionDropdown : FindChildComponent<TMP_Dropdown>(ResolutionDropdownPath);
        qualityDropdown = qualityDropdown != null ? qualityDropdown : FindChildComponent<TMP_Dropdown>(QualityDropdownPath);
        musicToggle = musicToggle != null ? musicToggle : FindChildComponent<Toggle>(MusicTogglePath);
        sfxToggle = sfxToggle != null ? sfxToggle : FindChildComponent<Toggle>(SfxTogglePath);
        windowedModeToggle = windowedModeToggle != null ? windowedModeToggle : FindChildComponent<Toggle>(WindowedModeTogglePath);
        musicSlider = musicSlider != null ? musicSlider : FindChildComponent<Slider>(MusicSliderPath);
        sfxSlider = sfxSlider != null ? sfxSlider : FindChildComponent<Slider>(SfxSliderPath);
    }

    private void BindButtons()
    {
        BindButton(resumeButton, Resume);
        BindButton(settingsButton, OpenSettings);
        BindButton(saveButton, Save);
        BindButton(saveAndExitButton, SaveAndExit);
        BindButton(settingsCloseButton, CloseSettings);
    }

    private void BindSettingsControls()
    {
        BindSlider(musicSlider, OnSettingsChanged);
        BindSlider(sfxSlider, OnSettingsChanged);
        BindDropdown(resolutionDropdown, OnSettingsChanged);
        BindDropdown(qualityDropdown, OnSettingsChanged);
        BindToggle(musicToggle, OnSettingsChanged);
        BindToggle(sfxToggle, OnSettingsChanged);
        BindToggle(windowedModeToggle, OnSettingsChanged);
    }

    private void OnSettingsChanged(float _)
    {
        ApplySettingsFromUi();
    }

    private void OnSettingsChanged(int _)
    {
        ApplySettingsFromUi();
    }

    private void OnSettingsChanged(bool _)
    {
        ApplySettingsFromUi();
    }

    private void ApplySettingsFromUi()
    {
        if (isApplyingSettings)
        {
            return;
        }

        MenuSettingsData settings = CaptureSettingsFromUi();
        settingsApplier.Apply(settings);

        IMenuAccountService accountService = GameCore.Instance.AccountService;
        if (!accountService.IsAuthenticated)
        {
            return;
        }

        accountService.CurrentAccount.settings = settings;
        accountService.Save();
    }

    private MenuSettingsData CaptureSettingsFromUi()
    {
        MenuSettingsData defaults = MenuSettingsFactory.CreateDefault();
        return new MenuSettingsData
        {
            musicVolume = musicSlider != null ? musicSlider.value : defaults.musicVolume,
            sfxVolume = sfxSlider != null ? sfxSlider.value : defaults.sfxVolume,
            resolutionDropdownIndex = resolutionDropdown != null ? resolutionDropdown.value : defaults.resolutionDropdownIndex,
            qualityDropdownIndex = qualityDropdown != null ? qualityDropdown.value : defaults.qualityDropdownIndex,
            musicEnabled = musicToggle != null ? musicToggle.isOn : defaults.musicEnabled,
            sfxEnabled = sfxToggle != null ? sfxToggle.isOn : defaults.sfxEnabled,
            windowedModeEnabled = windowedModeToggle != null ? windowedModeToggle.isOn : defaults.windowedModeEnabled
        };
    }

    private void PushCurrentSettings()
    {
        IMenuAccountService accountService = GameCore.Instance.AccountService;
        MenuSettingsData settings = accountService != null && accountService.IsAuthenticated
            ? accountService.CurrentAccount.settings
            : MenuSettingsFactory.CreateDefault();

        PushSettingsToControls(settings);
    }

    private void PushSettingsToControls(MenuSettingsData settings)
    {
        MenuSettingsData source = settings ?? MenuSettingsFactory.CreateDefault();
        isApplyingSettings = true;

        if (musicSlider != null)
        {
            musicSlider.SetValueWithoutNotify(source.musicVolume);
        }

        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(source.sfxVolume);
        }

        if (resolutionDropdown != null)
        {
            resolutionDropdown.SetValueWithoutNotify(source.resolutionDropdownIndex);
        }

        if (qualityDropdown != null)
        {
            qualityDropdown.SetValueWithoutNotify(source.qualityDropdownIndex);
        }

        if (musicToggle != null)
        {
            musicToggle.SetIsOnWithoutNotify(source.musicEnabled);
        }

        if (sfxToggle != null)
        {
            sfxToggle.SetIsOnWithoutNotify(source.sfxEnabled);
        }

        if (windowedModeToggle != null)
        {
            windowedModeToggle.SetIsOnWithoutNotify(source.windowedModeEnabled);
        }

        isApplyingSettings = false;
        settingsApplier.Apply(source);
    }

    private bool IsSettingsOpen()
    {
        return settingsPanel != null && settingsPanel.activeSelf;
    }

    private void CloseAllImmediate()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        if (pauseContent != null)
        {
            pauseContent.SetActive(true);
        }

        if (isPaused)
        {
            Time.timeScale = 1f;
        }

        isPaused = false;
    }

    private GameObject FindChildObject(string path)
    {
        Transform child = transform.Find(path);
        return child != null ? child.gameObject : null;
    }

    private T FindChildComponent<T>(string path) where T : Component
    {
        Transform child = transform.Find(path);
        return child != null ? child.GetComponent<T>() : null;
    }

    private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private static void BindSlider(Slider slider, UnityEngine.Events.UnityAction<float> action)
    {
        if (slider == null)
        {
            return;
        }

        slider.onValueChanged.RemoveAllListeners();
        slider.onValueChanged.AddListener(action);
    }

    private static void BindDropdown(TMP_Dropdown dropdown, UnityEngine.Events.UnityAction<int> action)
    {
        if (dropdown == null)
        {
            return;
        }

        dropdown.onValueChanged.RemoveAllListeners();
        dropdown.onValueChanged.AddListener(action);
    }

    private static void BindToggle(Toggle toggle, UnityEngine.Events.UnityAction<bool> action)
    {
        if (toggle == null)
        {
            return;
        }

        toggle.onValueChanged.RemoveAllListeners();
        toggle.onValueChanged.AddListener(action);
    }
}
