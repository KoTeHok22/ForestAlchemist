using System.Collections;
using System.Collections.Generic;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using TextField = Unity.AppUI.UI.TextField;

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
    private VisualElement screenControls;
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
    private SettingsPanelController settingsPanel;
    private ControlsSettingsPanelController controlsPanel;
    private VisualElement btnOpenControls;
    private VisualElement btnLogout;
    private VisualElement accountActions;
    private Label accountSectionTitle;
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
    private bool pendingNewGameReset;
    private AppUIClickRouter clickRouter;

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
        settingsPanel = new SettingsPanelController(root, settingsApplier, accountService);
        controlsPanel = new ControlsSettingsPanelController(root, accountService);
        controlsPanel.OnBackRequested = CloseControlsToSettings;
        BindButtons();
        ShowOverlay(null);
        ApplySessionState();
        RefreshRecords();
    }

    private void OnDisable()
    {
        controlsPanel?.CancelRebind();
    }

    // =================== Wiring ===================

    private void CacheElements()
    {
        screenMain     = root.Q<VisualElement>("screen-main");
        screenLogin    = root.Q<VisualElement>("screen-login");
        screenSettings = root.Q<VisualElement>("screen-settings");
        screenControls = root.Q<VisualElement>("screen-controls");
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

        btnLogout             = root.Q<VisualElement>("btn-logout");
        accountActions        = root.Q<VisualElement>("account-actions");
        accountSectionTitle   = root.Q<Label>("account-section-title");
        btnOpenControls       = root.Q<VisualElement>("btn-open-controls");
        btnSettingsClose      = root.Q<VisualElement>("btn-settings-close");

        recordsList     = root.Q<ScrollView>("records-list");
        btnRecordsClose = root.Q<VisualElement>("btn-records-close");

        loadBar = root.Q<LinearProgress>("load-bar");

        btnNewGameYes = root.Q<VisualElement>("btn-newgame-yes");
        btnNewGameNo  = root.Q<VisualElement>("btn-newgame-no");
    }

    private void BindButtons()
    {
        VisualElement clickRoot = root.Q<VisualElement>("root-panel") ?? root;
        clickRouter = new AppUIClickRouter(clickRoot);

        clickRouter.Add(btnNewGame, OpenNewGame);
        clickRouter.Add(btnContinue, ContinueGame);
        clickRouter.Add(btnRecords, OpenRecords);
        clickRouter.Add(btnExit, ExitGame);
        clickRouter.Add(btnSettingsGear, OpenSettings);
        clickRouter.Add(btnAccountHit, OpenLogin);

        clickRouter.Add(btnDoLogin, TryLogin);
        clickRouter.Add(btnDoRegister, TryRegister);
        clickRouter.Add(btnLoginClose, CloseOverlay);
        clickRouter.Add(btnSettingsClose, CloseOverlay);
        clickRouter.Add(btnOpenControls, OpenControlsFromSettings);
        clickRouter.Add(btnRecordsClose, CloseOverlay);
        clickRouter.Add(btnLogout, Logout);

        clickRouter.Add(btnNewGameYes, ConfirmNewGame);
        clickRouter.Add(btnNewGameNo, CancelNewGame);
    }

    // =================== Screen switching ===================

    private void ShowOverlay(VisualElement screen)
    {
        SetScreenVisible(screenLogin,    screen == screenLogin);
        SetScreenVisible(screenSettings, screen == screenSettings);
        SetScreenVisible(screenControls, screen == screenControls);
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

    private void CloseOverlay()
    {
        controlsPanel?.CancelRebind();
        ShowOverlay(null);
    }

    private void OpenControlsFromSettings()
    {
        if (!accountService.IsAuthenticated)
        {
            return;
        }

        controlsPanel?.Refresh();
        SetScreenVisible(screenSettings, false);
        SetScreenVisible(screenControls, true);
    }

    private void CloseControlsToSettings()
    {
        SetScreenVisible(screenControls, false);
        SetScreenVisible(screenSettings, true);
    }

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
        AudioHooks.SfxUnscaled(AudioClipId.SfxMenuNewGameConfirm);
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
        // Settings (graphics + sound) are machine-wide and do not require a login.
        PushSettingsToControls(GlobalSettingsStore.Current);
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
        AudioHooks.SfxUnscaled(AudioClipId.SfxMenuExitGame);
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
        AudioHooks.SfxUnscaled(result.IsSuccess ? AudioClipId.SfxMenuLoginSuccess : AudioClipId.SfxMenuLoginFail);

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
        if (!result.IsSuccess)
        {
            AudioHooks.SfxUnscaled(AudioClipId.SfxMenuLoginFail);
            return;
        }

        AudioHooks.SfxUnscaled(AudioClipId.SfxMenuRegisterSuccess);

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
        // Settings are machine-wide — logging out must NOT reset them.
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

        // Logout (and its "Account" heading) only make sense when signed in.
        SetScreenVisible(accountSectionTitle, authed);
        SetScreenVisible(accountActions, authed);

        // Settings are machine-wide, independent of which account (if any) is active.
        PushSettingsToControls(GlobalSettingsStore.Current);
        if (!authed)
        {
            SetLoadProgress(0f);
        }
    }

    private static void SetElementEnabled(VisualElement element, bool enabled)
    {
        if (element == null) return;
        element.SetEnabled(enabled);
    }

    // =================== Settings ===================

    private void PushSettingsToControls(MenuSettingsData settings)
    {
        settingsPanel?.PushToControls(settings);
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
        AudioHooks.Manager?.NotifyLoadingStarted();

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
            AudioHooks.Manager?.NotifyLoadingFinished();
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
        AudioHooks.Manager?.NotifyLoadingFinished();
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
