using UnityEngine;
using System;

public sealed class GameCore : MonoBehaviour
{
    private static GameCore instance;
    public static GameCore Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("GameCore");
                instance = go.AddComponent<GameCore>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
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
        accountService?.Save();
    }
private void OnApplicationQuit()
{
    SaveProgress();
}

private void OnDisable()
{
    SaveProgress();
}
}
