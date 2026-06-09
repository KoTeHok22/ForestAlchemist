using System.Collections;
using System.Collections.Generic;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using TextField = Unity.AppUI.UI.TextField;
using Toggle = Unity.AppUI.UI.Toggle;

/// <summary>
/// App UI–based replacement for the legacy uGUI MainMenuController.
/// Visual look mirrors the original Canvas: wooden frame buttons,
/// nickname plate top-right, settings gear bottom-right.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public sealed class MainMenuAppUIController : MonoBehaviour
{
    private const string HomeSceneName = "Home";
    private const string LoggedOutNickname = "Гость";

    private UIDocument document;
    private VisualElement root;

    // Screens
    private VisualElement screenMain;
    private VisualElement screenLogin;
    private VisualElement screenSettings;
    private VisualElement screenRecords;
    private VisualElement screenLoad;
    private VisualElement screenNewGame;

    // Main buttons (wood-btn VisualElements)
    private VisualElement btnNewGame;
    private VisualElement btnContinue;
    private VisualElement btnRecords;
    private VisualElement btnExit;
    private VisualElement btnSettingsGear;
    private VisualElement btnAccountHit;

    // Account
    private Label accountLabel;

    // Login
    private TextField loginUsername;
    private TextField loginPassword;
    private VisualElement btnDoLogin;
    private VisualElement btnDoRegister;
    private VisualElement btnLoginClose;
    private Label loginStatus;

    // Settings
    private Dropdown settingsResolution;
    private Dropdown settingsQuality;
    private Toggle settingsWindowed;
    private Toggle settingsMusicEnabled;
    private Toggle settingsSfxEnabled;
    private SliderFloat settingsMusicVolume;
    private SliderFloat settingsSfxVolume;
    private VisualElement btnLogout;
    private VisualElement btnSettingsClose;

    // Records
    private ScrollView recordsList;
    private VisualElement btnRecordsClose;

    // Load
    private LinearProgress loadBar;

    // NewGame confirm
    private VisualElement btnNewGameYes;
    private VisualElement btnNewGameNo;

    // Services
    private IMenuAccountService accountService;
    private IMenuSettingsApplier settingsApplier;
    private IMenuRecordsService recordsService;

    private Coroutine loadRoutine;
    private bool isApplyingSettings;
    private bool pendingNewGameReset;

    private void Awake()
    {
        document = GetComponent<UIDocument>();

        var core = GameCore.Instance;
        accountService = core.AccountService;
        settingsApplier = new UnityMenuSettingsApplier();
        recordsService = new MenuRecordsService();
    }

    private void OnEnable()
    {
        if (document == null) document = GetComponent<UIDocument>();
        root = document.rootVisualElement;
        if (root == null)
        {
            Debug.LogWarning("[MainMenuAppUI] UIDocument has no rootVisualElement.");
            return;
        }

        CacheElements();
        PopulateDropdowns();
        BindButtons();
        BindSettings();
        ShowOverlay(null);
        ApplySessionState();
        RefreshRecords();
    }

    // =================== Wiring ===================

    private void CacheElements()
    {
        screenMain     = root.Q<VisualElement>("screen-main");
        screenLogin    = root.Q<VisualElement>("screen-login");
        screenSettings = root.Q<VisualElement>("screen-settings");
        screenRecords  = root.Q<VisualElement>("screen-records");
        screenLoad     = root.Q<VisualElement>("screen-load");
        screenNewGame  = root.Q<VisualElement>("screen-newgame");

        btnNewGame      = root.Q<VisualElement>("btn-new-game");
        btnContinue     = root.Q<VisualElement>("btn-continue");
        btnRecords      = root.Q<VisualElement>("btn-records");
        btnExit         = root.Q<VisualElement>("btn-exit");
        btnSettingsGear = root.Q<VisualElement>("settings-gear-hit");
        btnAccountHit   = root.Q<VisualElement>("account-button-hit");

        accountLabel    = root.Q<Label>("account-label");

        loginUsername  = root.Q<TextField>("login-username");
        loginPassword  = root.Q<TextField>("login-password");
        btnDoLogin     = root.Q<VisualElement>("btn-do-login");
        btnDoRegister  = root.Q<VisualElement>("btn-do-register");
        btnLoginClose  = root.Q<VisualElement>("btn-login-close");
        loginStatus    = root.Q<Label>("login-status");

        settingsResolution    = root.Q<Dropdown>("settings-resolution");
        settingsQuality       = root.Q<Dropdown>("settings-quality");
        settingsWindowed      = root.Q<Toggle>("settings-windowed");
        settingsMusicEnabled  = root.Q<Toggle>("settings-music-enabled");
        settingsSfxEnabled    = root.Q<Toggle>("settings-sfx-enabled");
        settingsMusicVolume   = root.Q<SliderFloat>("settings-music-volume");
        settingsSfxVolume     = root.Q<SliderFloat>("settings-sfx-volume");
        btnLogout             = root.Q<VisualElement>("btn-logout");
        btnSettingsClose      = root.Q<VisualElement>("btn-settings-close");

        recordsList     = root.Q<ScrollView>("records-list");
        btnRecordsClose = root.Q<VisualElement>("btn-records-close");

        loadBar = root.Q<LinearProgress>("load-bar");

        btnNewGameYes = root.Q<VisualElement>("btn-newgame-yes");
        btnNewGameNo  = root.Q<VisualElement>("btn-newgame-no");
    }

    private void PopulateDropdowns()
    {
        if (settingsResolution != null)
        {
            settingsResolution.sourceItems = new List<string>
            {
                "1280x720",
                "1600x900",
                "1920x1080",
                "2560x1440",
                "3840x2160"
            };
            settingsResolution.bindItem = (item, i) => item.label = (string)settingsResolution.sourceItems[i];
        }

        if (settingsQuality != null)
        {
            settingsQuality.sourceItems = new List<string> { "Низкое", "Среднее", "Высокое", "Очень высокое" };
            settingsQuality.bindItem = (item, i) => item.label = (string)settingsQuality.sourceItems[i];
        }
    }

    private void BindButtons()
    {
        BindClick(btnNewGame,      OpenNewGame);
        BindClick(btnContinue,     ContinueGame);
        BindClick(btnRecords,      OpenRecords);
        BindClick(btnExit,         ExitGame);
        BindClick(btnSettingsGear, OpenSettings);
        BindClick(btnAccountHit,   OpenLogin);

        BindClick(btnDoLogin,    TryLogin);
        BindClick(btnDoRegister, TryRegister);
        BindClick(btnLoginClose, CloseOverlay);
        BindClick(btnSettingsClose, CloseOverlay);
        BindClick(btnRecordsClose,  CloseOverlay);
        BindClick(btnLogout, Logout);

        BindClick(btnNewGameYes, ConfirmNewGame);
        BindClick(btnNewGameNo,  CancelNewGame);
    }

    private static void BindClick(VisualElement element, System.Action callback)
    {
        if (element == null || callback == null) return;
        element.RegisterCallback<ClickEvent>(_ =>
        {
            if (element.enabledSelf)
            {
                callback();
            }
        });
        // Hand cursor for clarity
        element.pickingMode = PickingMode.Position;
    }

    private void BindSettings()
    {
        if (settingsResolution != null)
            settingsResolution.RegisterValueChangedCallback(_ => OnSettingsChanged());

        if (settingsQuality != null)
            settingsQuality.RegisterValueChangedCallback(_ => OnSettingsChanged());

        if (settingsWindowed != null)
            settingsWindowed.RegisterValueChangedCallback(_ => OnSettingsChanged());

        if (settingsMusicEnabled != null)
            settingsMusicEnabled.RegisterValueChangedCallback(_ => OnSettingsChanged());

        if (settingsSfxEnabled != null)
            settingsSfxEnabled.RegisterValueChangedCallback(_ => OnSettingsChanged());

        if (settingsMusicVolume != null)
            settingsMusicVolume.RegisterValueChangedCallback(_ => OnSettingsChanged());

        if (settingsSfxVolume != null)
            settingsSfxVolume.RegisterValueChangedCallback(_ => OnSettingsChanged());
    }

    // =================== Screen switching ===================

    private void ShowOverlay(VisualElement screen)
    {
        SetScreenVisible(screenLogin,    screen == screenLogin);
        SetScreenVisible(screenSettings, screen == screenSettings);
        SetScreenVisible(screenRecords,  screen == screenRecords);
        SetScreenVisible(screenLoad,     screen == screenLoad);
        SetScreenVisible(screenNewGame,  screen == screenNewGame);
        // Main remains always visible behind overlays
    }

    private static void SetScreenVisible(VisualElement screen, bool visible)
    {
        if (screen == null) return;
        screen.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void CloseOverlay() => ShowOverlay(null);

    // =================== Account / new game / continue ===================

    public void OpenNewGame()
    {
        if (!accountService.IsAuthenticated || loadRoutine != null) return;

        if (accountService.HasSavedGame)
        {
            ShowOverlay(screenNewGame);
            return;
        }

        StartNewGame();
    }

    public void ConfirmNewGame()
    {
        if (!accountService.IsAuthenticated || loadRoutine != null) return;
        StartNewGame();
    }

    public void CancelNewGame() => ShowOverlay(null);

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
        ShowOverlay(screenLoad);
        loadRoutine = StartCoroutine(LoadHomeAsync());
    }

    public void OpenSettings()
    {
        if (!accountService.IsAuthenticated) return;
        PushSettingsToControls(accountService.CurrentAccount.settings);
        ShowOverlay(screenSettings);
    }

    public void OpenRecords()
    {
        if (!accountService.IsAuthenticated) return;
        RefreshRecords();
        ShowOverlay(screenRecords);
    }

    public void OpenLogin()
    {
        ClearLoginInputs();
        SetLoginStatus(string.Empty);
        ShowOverlay(screenLogin);
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
        AuthOperationResult result = accountService.TryLogin(GetUsername(), GetPassword());
        SetLoginStatus(result.Message);

        if (!result.IsSuccess) return;

        ClearLoginInputs();
        GameCore.Instance.ReloadRuntimeProgress();
        ShowOverlay(null);
        ApplySessionState();
        RefreshRecords();
    }

    public void TryRegister()
    {
        AuthOperationResult result = accountService.TryRegister(GetUsername(), GetPassword());
        SetLoginStatus(result.Message);

        if (!result.IsSuccess) return;

        ClearLoginInputs();
        GameCore.Instance.ReloadRuntimeProgress();
        ShowOverlay(null);
        ApplySessionState();
        RefreshRecords();
    }

    public void Logout()
    {
        accountService.Logout();
        GameCore.Instance.ReloadRuntimeProgress();
        PushSettingsToControls(MenuSettingsFactory.CreateDefault());
        ShowOverlay(null);
        ApplySessionState();
        RefreshRecords();
    }

    // =================== Session state ===================

    private void ApplySessionState()
    {
        bool authed = accountService.IsAuthenticated;

        if (accountLabel != null)
            accountLabel.text = authed ? accountService.CurrentAccount.username : LoggedOutNickname;

        SetElementEnabled(btnNewGame,  authed);
        SetElementEnabled(btnContinue, authed && accountService.HasSavedGame);
        SetElementEnabled(btnRecords,  authed);

        if (authed)
        {
            PushSettingsToControls(accountService.CurrentAccount.settings);
        }
        else
        {
            SetLoadProgress(0f);
            PushSettingsToControls(MenuSettingsFactory.CreateDefault());
        }
    }

    private static void SetElementEnabled(VisualElement element, bool enabled)
    {
        if (element == null) return;
        element.SetEnabled(enabled);
    }

    // =================== Settings ===================

    private void OnSettingsChanged()
    {
        if (isApplyingSettings) return;

        MenuSettingsData settings = CaptureSettingsFromUi();
        settingsApplier.Apply(settings);

        if (!accountService.IsAuthenticated) return;

        accountService.CurrentAccount.settings = settings;
        accountService.Save();
    }

    private MenuSettingsData CaptureSettingsFromUi()
    {
        MenuSettingsData defaults = MenuSettingsFactory.CreateDefault();
        return new MenuSettingsData
        {
            musicVolume             = settingsMusicVolume != null ? settingsMusicVolume.value : defaults.musicVolume,
            sfxVolume               = settingsSfxVolume   != null ? settingsSfxVolume.value   : defaults.sfxVolume,
            resolutionDropdownIndex = settingsResolution  != null ? settingsResolution.selectedIndex : defaults.resolutionDropdownIndex,
            qualityDropdownIndex    = settingsQuality     != null ? settingsQuality.selectedIndex    : defaults.qualityDropdownIndex,
            musicEnabled            = settingsMusicEnabled != null ? settingsMusicEnabled.value : defaults.musicEnabled,
            sfxEnabled              = settingsSfxEnabled   != null ? settingsSfxEnabled.value   : defaults.sfxEnabled,
            windowedModeEnabled     = settingsWindowed     != null ? settingsWindowed.value     : defaults.windowedModeEnabled
        };
    }

    private void PushSettingsToControls(MenuSettingsData settings)
    {
        MenuSettingsData src = settings ?? MenuSettingsFactory.CreateDefault();
        isApplyingSettings = true;

        if (settingsMusicVolume != null)   settingsMusicVolume.SetValueWithoutNotify(src.musicVolume);
        if (settingsSfxVolume != null)     settingsSfxVolume.SetValueWithoutNotify(src.sfxVolume);
        if (settingsResolution != null)    settingsResolution.SetValueWithoutNotify(new[] { src.resolutionDropdownIndex });
        if (settingsQuality != null)       settingsQuality.SetValueWithoutNotify(new[] { src.qualityDropdownIndex });
        if (settingsMusicEnabled != null)  settingsMusicEnabled.SetValueWithoutNotify(src.musicEnabled);
        if (settingsSfxEnabled != null)    settingsSfxEnabled.SetValueWithoutNotify(src.sfxEnabled);
        if (settingsWindowed != null)      settingsWindowed.SetValueWithoutNotify(src.windowedModeEnabled);

        isApplyingSettings = false;
        settingsApplier.Apply(src);
    }

    // =================== Records ===================

    private void RefreshRecords()
    {
        if (recordsList == null) return;

        recordsList.contentContainer.Clear();

        IReadOnlyList<RecordEntryData> records = recordsService.GetSortedRecords(accountService.SaveData.accounts);
        if (records == null || records.Count == 0)
        {
            var empty = new Label("Пока никто не возвращался из леса с трофеями");
            empty.style.unityTextAlign = TextAnchor.MiddleCenter;
            empty.style.color = new StyleColor(new Color(0.84f, 0.78f, 0.61f, 1f));
            empty.style.paddingTop = 40;
            empty.style.fontSize = 16;
            recordsList.contentContainer.Add(empty);
            return;
        }

        for (int i = 0; i < records.Count; i++)
        {
            var row = BuildRecordRow(i + 1, records[i]);
            recordsList.contentContainer.Add(row);
        }
    }

    private static VisualElement BuildRecordRow(int rank, RecordEntryData data)
    {
        var row = new VisualElement();
        row.AddToClassList("records-row");
        if (rank % 2 == 0) row.AddToClassList("records-row--alt");
        if (rank <= 3) row.AddToClassList("records-row--top");

        row.Add(MakeCell(rank.ToString(), "records-col--rank"));
        row.Add(MakeCell(data.Nickname, "records-col--name"));
        row.Add(MakeCell(data.Level.ToString(), "records-col--level"));
        row.Add(MakeCell(data.Score.ToString(), "records-col--score"));
        return row;
    }

    private static VisualElement MakeCell(string text, string colClass)
    {
        var cell = new Label(text);
        cell.AddToClassList("records-cell");
        cell.AddToClassList(colClass);
        return cell;
    }

    // =================== Loading ===================

    private IEnumerator LoadHomeAsync()
    {
        SetLoadProgress(0f);

        if (pendingNewGameReset)
        {
            accountService.ResetProgress();
            GameCore.Instance.ReloadRuntimeProgress();
            RefreshRecords();
            pendingNewGameReset = false;
        }

        AsyncOperation op = SceneManager.LoadSceneAsync(HomeSceneName);
        if (op == null)
        {
            loadRoutine = null;
            yield break;
        }

        while (!op.isDone)
        {
            float progress = Mathf.Clamp01(op.progress / 0.9f);
            SetLoadProgress(progress);
            yield return null;
        }

        SetLoadProgress(1f);
        loadRoutine = null;
    }

    private void StartNewGame()
    {
        if (!accountService.IsAuthenticated || loadRoutine != null) return;

        pendingNewGameReset = accountService.HasSavedGame;
        ShowOverlay(screenLoad);
        loadRoutine = StartCoroutine(LoadHomeAsync());
    }

    private void SetLoadProgress(float value)
    {
        if (loadBar != null)
            loadBar.value = Mathf.Clamp01(value);
    }

    // =================== Helpers ===================

    private void SetLoginStatus(string message)
    {
        if (loginStatus != null)
            loginStatus.text = message ?? string.Empty;
    }

    private string GetUsername() => loginUsername != null ? (loginUsername.value ?? string.Empty).Trim() : string.Empty;
    private string GetPassword() => loginPassword != null ? loginPassword.value ?? string.Empty : string.Empty;

    private void ClearLoginInputs()
    {
        if (loginUsername != null) loginUsername.SetValueWithoutNotify(string.Empty);
        if (loginPassword != null) loginPassword.SetValueWithoutNotify(string.Empty);
    }
}
