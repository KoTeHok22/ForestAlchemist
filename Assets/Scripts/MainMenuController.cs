using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class MainMenuController : MonoBehaviour
{
    private const string MainMenuPanelPath = "MainMenu";
    private const string LoadPanelPath = "Load";
    private const string SettingsPanelPath = "Settings";
    private const string RecordsPanelPath = "Records";
    private const string StorePanelPath = "Store";
    private const string LoginPanelPath = "Login";

    private const string NewGameButtonPath = "MainMenu/Buttons/Button";
    private const string ContinueButtonPath = "MainMenu/Buttons/Button (1)";
    private const string SettingsButtonPath = "MainMenu/SettingsBtn";
    private const string RecordsButtonPath = "MainMenu/Buttons/Button (3)";
    private const string ExitButtonPath = "MainMenu/Buttons/Button (4)";
    private const string AccountButtonPath = "MainMenu/Account";
    private const string NicknameLabelPath = "MainMenu/Account/Account_Name/NickName";

    private const string NewGamePanelPath = "NewGame";
    private const string NewGameYesButtonPath = "NewGame/Yes";
    private const string NewGameNoButtonPath = "NewGame/No";
    private const string LoadScrollbarPath = "Load/LoadLevel";
    private const string SettingsCloseButtonPath = "Settings/Content/Close";
    private const string RecordsCloseButtonPath = "Records/Content/Close";
    private const string LogoutButtonPath = "Settings/Content/Button";

    private const string LoginUsernameInputPath = "Login/Content/Content/LoginInput";
    private const string LoginPasswordInputPath = "Login/Content/Content/PassInput";
    private const string LoginButtonPath = "Login/Content/Content/GoLogin";
    private const string RegisterButtonPath = "Login/Content/Content/GoReg";
    private const string LoginStatusTextPath = "Login/Content/Content/StatusLogin";

    private const string ResolutionDropdownPath = "Settings/Content/Settings/Setting/Panel (1)/Dropdown";
    private const string QualityDropdownPath = "Settings/Content/Settings/Setting (1)/Panel (1)/Dropdown";
    private const string MusicTogglePath = "Settings/Content/Settings/Setting (2)/Panel (1)/Toggle";
    private const string SfxTogglePath = "Settings/Content/Settings/Setting (3)/Panel (1)/Toggle";
    private const string WindowedModeTogglePath = "Settings/Content/Settings/Setting (4)/Panel (1)/Toggle";
    private const string SettingsSliderOnePath = "Settings/Content/Settings/Setting (2)/Panel (1)/Slider";
    private const string SettingsSliderTwoPath = "Settings/Content/Settings/Setting (3)/Panel (1)/Slider";

    private const string RatingTemplatePath = "Records/Content/Top/Rating";
    private const string TopContainerPath = "Records/Content/Top";
    private const string SaveFileName = "menu_save.json";
    private const string HomeSceneName = "Home";
    private const string LoggedOutNickname = "Войти";
    private const int MinPasswordLength = 8;

    private readonly UiReferences ui = new UiReferences();

    private IMenuAccountService accountService;
    private IMenuSettingsApplier settingsApplier;
    private IMenuRecordsService recordsService;
    private IRecordsViewPopulator recordsViewPopulator;
    private Coroutine loadRoutine;
    private bool isApplyingSettings;
    private bool pendingNewGameReset;

    private void Awake()
    {
        var core = GameCore.Instance;
        accountService = core.AccountService;
        settingsApplier = new UnityMenuSettingsApplier();
        recordsService = new MenuRecordsService();

        CacheReferences();
        BindButtons();
        BindSettingsControls();
        ApplySessionState();
        CloseOverlayPanels();
        SetLoadProgress(0f);
        RefreshRecordsView();
    }

    private void Start()
    {
        BindButtons();
        ApplySessionState();
        CloseOverlayPanels();
    }

    public void OpenNewGame()
    {
        if (!accountService.IsAuthenticated || loadRoutine != null)
        {
            return;
        }

        if (accountService.HasSavedGame)
        {
            ShowOnly(ui.newGamePanel);
            return;
        }

        StartNewGame();
    }

    public void ConfirmNewGame()
    {
        if (!accountService.IsAuthenticated || loadRoutine != null)
        {
            return;
        }

        StartNewGame();
    }

    public void CancelNewGame()
    {
        if (!accountService.IsAuthenticated || loadRoutine != null)
        {
            return;
        }

        ShowOnly(null);
    }

    public void ContinueGame()
    {
        if (!accountService.IsAuthenticated || loadRoutine != null)
        {
            ApplySessionState();
            return;
        }

        if (!accountService.HasSavedGame)
        {
            ApplySessionState();
            return;
        }
 
        pendingNewGameReset = false;
        ShowOnly(ui.loadPanel);
        loadRoutine = StartCoroutine(LoadHomeAsync());
    }

    public void OpenSettings()
    {
        if (!accountService.IsAuthenticated)
        {
            return;
        }

        ShowOnly(ui.settingsPanel);
    }

    public void OpenRecords()
    {
        if (!accountService.IsAuthenticated)
        {
            return;
        }

        RefreshRecordsView();
        ShowOnly(ui.recordsPanel);
    }

    public void OpenLogin()
    {
        ClearLoginInputs();
        SetLoginStatus(string.Empty);
        ShowOnly(ui.loginPanel);
    }

    public void CloseOverlayPanels()
    {
        ShowOnly(null);
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void TryLogin()
    {
        AuthOperationResult result = accountService.TryLogin(GetTrimmedUsername(), GetPassword());
        SetLoginStatus(result.Message);

        if (!result.IsSuccess)
        {
            return;
        }

        ClearLoginInputs();
        GameCore.Instance.ReloadRuntimeProgress();
        CloseOverlayPanels();
        ApplySessionState();
        RefreshRecordsView();
    }

    public void TryRegister()
    {
        AuthOperationResult result = accountService.TryRegister(GetTrimmedUsername(), GetPassword());
        SetLoginStatus(result.Message);

        if (!result.IsSuccess)
        {
            return;
        }

        ClearLoginInputs();
        GameCore.Instance.ReloadRuntimeProgress();
        CloseOverlayPanels();
        ApplySessionState();
        RefreshRecordsView();
    }

    public void Logout()
    {
        accountService.Logout();
        GameCore.Instance.ReloadRuntimeProgress();
        ApplyGuestSettings();
        CloseOverlayPanels();
        ApplySessionState();
        RefreshRecordsView();
    }

    public void UpdateCurrentPlayerProgress(int completedLevel, int score)
    {
        accountService.UpdateProgress(completedLevel, score);
        RefreshRecordsView();
    }

    private void ConstructServices()
    {
    }

    private void CacheReferences()
    {
        ui.mainMenuPanel = FindChildObject(MainMenuPanelPath);
        ui.loadPanel = FindChildObject(LoadPanelPath);
        ui.settingsPanel = FindChildObject(SettingsPanelPath);
        ui.recordsPanel = FindChildObject(RecordsPanelPath);
        ui.storePanel = FindChildObject(StorePanelPath);
        ui.loginPanel = FindChildObject(LoginPanelPath);
        ui.newGamePanel = FindChildObject(NewGamePanelPath);

        ui.loadScrollbar = FindChildComponent<Scrollbar>(LoadScrollbarPath);
        ui.newGameButton = FindChildComponent<Button>(NewGameButtonPath);
        ui.continueButton = FindChildComponent<Button>(ContinueButtonPath);
        ui.settingsButton = FindChildComponent<Button>(SettingsButtonPath);
        ui.recordsButton = FindChildComponent<Button>(RecordsButtonPath);
        ui.exitButton = FindChildComponent<Button>(ExitButtonPath);
        ui.accountButton = FindChildComponent<Button>(AccountButtonPath);
        ui.settingsCloseButton = FindChildComponent<Button>(SettingsCloseButtonPath);
        ui.recordsCloseButton = FindChildComponent<Button>(RecordsCloseButtonPath);
        ui.logoutButton = FindChildComponent<Button>(LogoutButtonPath);
        ui.loginActionButton = FindChildComponent<Button>(LoginButtonPath);
        ui.registerActionButton = FindChildComponent<Button>(RegisterButtonPath);
        ui.newGameYesButton = FindChildComponent<Button>(NewGameYesButtonPath);
        ui.newGameNoButton = FindChildComponent<Button>(NewGameNoButtonPath);

        ui.nicknameLabel = FindChildComponent<TMP_Text>(NicknameLabelPath);
        ui.usernameInputField = FindChildComponent<TMP_InputField>(LoginUsernameInputPath);
        ui.passwordInputField = FindChildComponent<TMP_InputField>(LoginPasswordInputPath);
        ui.loginStatusText = FindChildComponent<TMP_Text>(LoginStatusTextPath);
        ui.resolutionDropdown = FindChildComponent<TMP_Dropdown>(ResolutionDropdownPath);
        ui.qualityDropdown = FindChildComponent<TMP_Dropdown>(QualityDropdownPath);
        ui.musicToggle = FindChildComponent<Toggle>(MusicTogglePath);
        ui.sfxToggle = FindChildComponent<Toggle>(SfxTogglePath);
        ui.windowedModeToggle = FindChildComponent<Toggle>(WindowedModeTogglePath);
        ui.musicVolumeSlider = FindChildComponent<Slider>(SettingsSliderOnePath);
        ui.sfxVolumeSlider = FindChildComponent<Slider>(SettingsSliderTwoPath);
        GameObject ratingTemplate = FindChildObject(RatingTemplatePath);
        Transform topContainer = FindChildTransform(TopContainerPath);
        recordsViewPopulator = new RecordsViewPopulator(ratingTemplate, topContainer);
    }

    private void BindButtons()
    {
        BindButton(ui.newGameButton, OpenNewGame);
        BindButton(ui.continueButton, ContinueGame);
        BindButton(ui.settingsButton, OpenSettings);
        BindButton(ui.recordsButton, OpenRecords);
        BindButton(ui.exitButton, ExitGame);
        BindButton(ui.accountButton, OpenLogin);
        BindButton(ui.settingsCloseButton, CloseOverlayPanels);
        BindButton(ui.recordsCloseButton, CloseOverlayPanels);
        BindButton(ui.logoutButton, Logout);
        BindButton(ui.loginActionButton, TryLogin);
        BindButton(ui.registerActionButton, TryRegister);
        BindButton(ui.newGameYesButton, ConfirmNewGame);
        BindButton(ui.newGameNoButton, CancelNewGame);
    }

    private void BindSettingsControls()
    {
        BindSlider(ui.musicVolumeSlider, OnSettingsControlChanged);
        BindSlider(ui.sfxVolumeSlider, OnSettingsControlChanged);
        BindDropdown(ui.resolutionDropdown, OnSettingsControlChanged);
        BindDropdown(ui.qualityDropdown, OnSettingsControlChanged);
        BindToggle(ui.musicToggle, OnSettingsControlChanged);
        BindToggle(ui.sfxToggle, OnSettingsControlChanged);
        BindToggle(ui.windowedModeToggle, OnSettingsControlChanged);
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

    private IEnumerator LoadHomeAsync()
    {
        SetLoadProgress(0f);

        if (pendingNewGameReset)
        {
            accountService.ResetProgress();
            GameCore.Instance.ReloadRuntimeProgress();
            RefreshRecordsView();
            pendingNewGameReset = false;
        }
 
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(HomeSceneName);
        if (loadOperation == null)
        {
            loadRoutine = null;
            yield break;
        }
 
        while (!loadOperation.isDone)
        {
            float normalizedProgress = Mathf.Clamp01(loadOperation.progress / 0.9f);
            SetLoadProgress(normalizedProgress);
            yield return null;
        }
 
        SetLoadProgress(1f);
        loadRoutine = null;
    }

    private void ShowOnly(GameObject activePanel)
    {
        SetPanelState(ui.mainMenuPanel, true);
        SetPanelState(ui.loadPanel, activePanel == ui.loadPanel);
        SetPanelState(ui.settingsPanel, activePanel == ui.settingsPanel);
        SetPanelState(ui.recordsPanel, activePanel == ui.recordsPanel);
        SetPanelState(ui.storePanel, activePanel == ui.storePanel);
        SetPanelState(ui.loginPanel, activePanel == ui.loginPanel);
        SetPanelState(ui.newGamePanel, activePanel == ui.newGamePanel);
    }

    private void ApplySessionState()
    {
        bool authenticated = accountService.IsAuthenticated;

        if (ui.nicknameLabel != null)
        {
            ui.nicknameLabel.text = authenticated ? accountService.CurrentAccount.username : LoggedOutNickname;
        }

        SetButtonInteractable(ui.newGameButton, authenticated);
        SetButtonInteractable(ui.continueButton, authenticated && accountService.HasSavedGame);
        SetButtonInteractable(ui.settingsButton, authenticated);
        SetButtonInteractable(ui.recordsButton, authenticated);
        SetButtonInteractable(ui.exitButton, authenticated);
        SetButtonInteractable(ui.accountButton, !authenticated);
        SetButtonInteractable(ui.logoutButton, authenticated);

        if (authenticated)
        {
            PushSettingsToControls(accountService.CurrentAccount.settings);
            return;
        }

        SetLoadProgress(0f);
        PushSettingsToControls(MenuSettingsFactory.CreateDefault());
    }

    private static void SetButtonInteractable(Button button, bool interactable)
    {
        if (button != null)
        {
            button.interactable = interactable;
        }
    }

    private static void SetPanelState(GameObject panel, bool state)
    {
        if (panel != null)
        {
            panel.SetActive(state);
        }
    }

    private void SetLoadProgress(float progress)
    {
        if (ui.loadScrollbar != null)
        {
            ui.loadScrollbar.size = progress;
        }
    }

    private void SetLoginStatus(string message)
    {
        if (ui.loginStatusText != null)
        {
            ui.loginStatusText.text = message ?? string.Empty;
        }
    }

    private void OnSettingsControlChanged(float _)
    {
        ApplyAndPersistCurrentSettings();
    }

    private void OnSettingsControlChanged(int _)
    {
        ApplyAndPersistCurrentSettings();
    }

    private void OnSettingsControlChanged(bool _)
    {
        ApplyAndPersistCurrentSettings();
    }

    private void ApplyAndPersistCurrentSettings()
    {
        if (isApplyingSettings)
        {
            return;
        }

        MenuSettingsData settings = CaptureSettingsFromUi();
        settingsApplier.Apply(settings);

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
            musicVolume = ui.musicVolumeSlider != null ? ui.musicVolumeSlider.value : defaults.musicVolume,
            sfxVolume = ui.sfxVolumeSlider != null ? ui.sfxVolumeSlider.value : defaults.sfxVolume,
            resolutionDropdownIndex = ui.resolutionDropdown != null ? ui.resolutionDropdown.value : defaults.resolutionDropdownIndex,
            qualityDropdownIndex = ui.qualityDropdown != null ? ui.qualityDropdown.value : defaults.qualityDropdownIndex,
            musicEnabled = ui.musicToggle != null ? ui.musicToggle.isOn : defaults.musicEnabled,
            sfxEnabled = ui.sfxToggle != null ? ui.sfxToggle.isOn : defaults.sfxEnabled,
            windowedModeEnabled = ui.windowedModeToggle != null ? ui.windowedModeToggle.isOn : defaults.windowedModeEnabled
        };
    }

    private void PushSettingsToControls(MenuSettingsData settings)
    {
        MenuSettingsData source = settings ?? MenuSettingsFactory.CreateDefault();
        isApplyingSettings = true;

        if (ui.musicVolumeSlider != null)
        {
            ui.musicVolumeSlider.SetValueWithoutNotify(source.musicVolume);
        }

        if (ui.sfxVolumeSlider != null)
        {
            ui.sfxVolumeSlider.SetValueWithoutNotify(source.sfxVolume);
        }

        if (ui.resolutionDropdown != null)
        {
            ui.resolutionDropdown.SetValueWithoutNotify(source.resolutionDropdownIndex);
        }

        if (ui.qualityDropdown != null)
        {
            ui.qualityDropdown.SetValueWithoutNotify(source.qualityDropdownIndex);
        }

        if (ui.musicToggle != null)
        {
            ui.musicToggle.SetIsOnWithoutNotify(source.musicEnabled);
        }

        if (ui.sfxToggle != null)
        {
            ui.sfxToggle.SetIsOnWithoutNotify(source.sfxEnabled);
        }

        if (ui.windowedModeToggle != null)
        {
            ui.windowedModeToggle.SetIsOnWithoutNotify(source.windowedModeEnabled);
        }

        isApplyingSettings = false;
        settingsApplier.Apply(source);
    }

    private void ApplyGuestSettings()
    {
        PushSettingsToControls(MenuSettingsFactory.CreateDefault());
    }

    private void StartNewGame()
    {
        if (!accountService.IsAuthenticated || loadRoutine != null)
        {
            return;
        }

        pendingNewGameReset = accountService.HasSavedGame;
        ShowOnly(ui.loadPanel);
        loadRoutine = StartCoroutine(LoadHomeAsync());
    }

    private void RefreshRecordsView()
    {
        IReadOnlyList<RecordEntryData> records = recordsService.GetSortedRecords(accountService.SaveData.accounts);
        recordsViewPopulator.Populate(records);
    }

    private string GetTrimmedUsername()
    {
        return ui.usernameInputField != null ? ui.usernameInputField.text.Trim() : string.Empty;
    }

    private string GetPassword()
    {
        return ui.passwordInputField != null ? ui.passwordInputField.text : string.Empty;
    }

    private void ClearLoginInputs()
    {
        if (ui.usernameInputField != null)
        {
            ui.usernameInputField.SetTextWithoutNotify(string.Empty);
        }

        if (ui.passwordInputField != null)
        {
            ui.passwordInputField.SetTextWithoutNotify(string.Empty);
        }
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

    private Transform FindChildTransform(string path)
    {
        return transform.Find(path);
    }

    private sealed class UiReferences
    {
        public GameObject mainMenuPanel;
        public GameObject loadPanel;
        public GameObject settingsPanel;
        public GameObject recordsPanel;
        public GameObject storePanel;
        public GameObject loginPanel;
        public GameObject newGamePanel;
 
        public Scrollbar loadScrollbar;
        public Button newGameButton;
        public Button continueButton;
        public Button settingsButton;
        public Button recordsButton;
        public Button exitButton;
        public Button accountButton;
        public Button settingsCloseButton;
        public Button recordsCloseButton;
        public Button logoutButton;
        public Button loginActionButton;
        public Button registerActionButton;
        public Button newGameYesButton;
        public Button newGameNoButton;
 
        public TMP_Text nicknameLabel;
        public TMP_InputField usernameInputField;
        public TMP_InputField passwordInputField;
        public TMP_Text loginStatusText;
        public TMP_Dropdown resolutionDropdown;
        public TMP_Dropdown qualityDropdown;
        public Toggle musicToggle;
        public Toggle sfxToggle;
        public Toggle windowedModeToggle;
        public Slider musicVolumeSlider;
        public Slider sfxVolumeSlider;
    }
}
