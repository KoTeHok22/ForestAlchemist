using UnityEngine;
using System;
using UnityEngine.SceneManagement;

public sealed class GameCore : MonoBehaviour
{
    private static GameCore instance;
    public static GameCore Instance => ResolveInstance();

    private static GameCore ResolveInstance()
    {
        if (RuntimeSingletonGuard.IsShuttingDown) return null;
        if (instance == null)
        {
            instance = FindAnyObjectByType<GameCore>(FindObjectsInactive.Include);
        }
        if (instance == null)
        {
            GameObject go = new GameObject("GameCore");
            instance = go.AddComponent<GameCore>();
            DontDestroyOnLoad(go);
        }
        return instance;
    }

    private IMenuAccountService accountService;
    public IMenuAccountService AccountService => accountService;

    public GameProgressData CurrentProgress => accountService?.CurrentAccount?.gameProgress;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        // Initialization logic
        IMenuAccountRepository repository = new JsonMenuAccountRepository("menu_save.json");
        IPasswordService passwordService = new Sha256PasswordService(8);
        accountService = new MenuAccountService(repository, passwordService);
        accountService.TryAutoLogin();
    }

    public void SaveProgress()
    {
        GameProgressData progress = CurrentProgress;
        if (progress != null)
        {
            GameProgressUtility.Touch(progress);
        }

        accountService?.Save();
    }

    public void ReloadRuntimeProgress()
    {
        InventoryService existingInventoryService = FindFirstObjectByType<InventoryService>();
        if (existingInventoryService != null)
        {
            existingInventoryService.ReloadFromCurrentAccount();
        }

        ExpeditionManager existingExpeditionManager = FindFirstObjectByType<ExpeditionManager>();
        if (existingExpeditionManager != null)
        {
            existingExpeditionManager.ReloadFromCurrentAccount();
        }

        foreach (PlayerScoreProvider provider in FindObjectsByType<PlayerScoreProvider>(FindObjectsSortMode.None))
        {
            provider.ReloadFromCurrentAccount();
        }
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void OnApplicationQuit()
    {
        RuntimeSingletonGuard.MarkShuttingDown();
        SaveProgress();
    }

    private void OnDisable()
    {
        if (!RuntimeSingletonGuard.IsShuttingDown)
        {
            SaveProgress();
        }
    }
}
