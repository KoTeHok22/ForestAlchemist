using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public enum AuthResultType
{
    Success,
    EmptyUsername,
    EmptyPassword,
    UserNotFound,
    UserAlreadyExists,
    PasswordTooShort,
    PasswordMissingSpecial,
    PasswordMissingUppercase,
    InvalidPassword
}

public sealed class AuthOperationResult
{
    public bool IsSuccess { get; }
    public AuthResultType ResultType { get; }
    public string Message { get; }

    public AuthOperationResult(bool isSuccess, AuthResultType resultType, string message)
    {
        IsSuccess = isSuccess;
        ResultType = resultType;
        Message = message;
    }

    public static AuthOperationResult Success(string message)
    {
        return new AuthOperationResult(true, AuthResultType.Success, message);
    }

    public static AuthOperationResult Failure(AuthResultType resultType, string message)
    {
        return new AuthOperationResult(false, resultType, message);
    }
}

public interface IPasswordService
{
    AuthOperationResult Validate(string password);
    string Hash(string password);
}

public sealed class Sha256PasswordService : IPasswordService
{
    private readonly int minPasswordLength;

    public Sha256PasswordService(int minPasswordLength)
    {
        this.minPasswordLength = minPasswordLength;
    }

    public AuthOperationResult Validate(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return AuthOperationResult.Failure(AuthResultType.EmptyPassword, "Пароль не может быть пустым.");
        }

        if (password.Length < minPasswordLength)
        {
            return AuthOperationResult.Failure(AuthResultType.PasswordTooShort, "Пароль должен быть не менее 8 символов.");
        }

        if (!password.Any(char.IsUpper))
        {
            return AuthOperationResult.Failure(AuthResultType.PasswordMissingUppercase, "Пароль должен содержать хотя бы 1 заглавную букву.");
        }

        if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
        {
            return AuthOperationResult.Failure(AuthResultType.PasswordMissingSpecial, "Пароль должен содержать хотя бы 1 специальный символ.");
        }

        return AuthOperationResult.Success("Пароль корректен.");
    }

    public string Hash(string password)
    {
        using SHA256 sha256 = SHA256.Create();
        byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(password ?? string.Empty));
        StringBuilder builder = new StringBuilder(hash.Length * 2);

        for (int i = 0; i < hash.Length; i++)
        {
            builder.Append(hash[i].ToString("x2"));
        }

        return builder.ToString();
    }
}

public static class MenuSettingsFactory
{
    public static MenuSettingsData CreateDefault()
    {
        return new MenuSettingsData
        {
            musicVolume = 1f,
            sfxVolume = 1f,
            qualityDropdownIndex = 0,
            resolutionDropdownIndex = 0,
            musicEnabled = true,
            sfxEnabled = true,
            windowedModeEnabled = true
        };
    }
}

public interface IMenuSettingsApplier
{
    void Apply(MenuSettingsData settings);
}

public static class GameProgressUtility
{
    public static GameProgressData CreateDefault()
    {
        return new GameProgressData();
    }

    public static void Touch(GameProgressData progress)
    {
        if (progress == null)
        {
            return;
        }

        progress.lastUpdatedUtc = DateTime.UtcNow.ToString("O");
    }

    public static bool HasMeaningfulProgress(GameProgressData progress)
    {
        if (progress == null)
        {
            return false;
        }

        if (progress.homeStorage?.slots != null && progress.homeStorage.slots.Any(slot => slot != null && slot.count > 0))
        {
            return true;
        }

        if (progress.expeditionInventory?.slots != null && progress.expeditionInventory.slots.Any(slot => slot != null && slot.count > 0))
        {
            return true;
        }

        if (progress.loadout?.consumables != null && progress.loadout.consumables.Any(slot => slot != null && slot.count > 0))
        {
            return true;
        }

        if (progress.hotbar?.slotItemIds != null && progress.hotbar.slotItemIds.Any(itemId => !string.IsNullOrWhiteSpace(itemId)))
        {
            return true;
        }

        if (progress.crafting != null)
        {
            if (progress.crafting.level > 1 || progress.crafting.craftingXp > 0)
            {
                return true;
            }

            if ((progress.crafting.unlockedRecipes?.Count ?? 0) > 0 || (progress.crafting.craftedSpells?.Count ?? 0) > 0)
            {
                return true;
            }
        }

        if (progress.garden != null)
        {
            if (progress.garden.currentStage > 0 || progress.garden.expeditionsToNextStage != 1 || progress.garden.maxStage != 3)
            {
                return true;
            }
        }

        if (progress.orcs != null)
        {
            if (progress.orcs.threatLevel > 1 || !Mathf.Approximately(progress.orcs.statMultiplier, 1f))
            {
                return true;
            }
        }

        if (progress.stats != null)
        {
            if (progress.stats.successfulExpeditions > 0 || progress.stats.totalDeaths > 0 || progress.stats.deepestThreatReached > 0)
            {
                return true;
            }
        }

        if (progress.quests != null)
        {
            if ((progress.quests.boardQuestIds?.Count ?? 0) > 0 || (progress.quests.activeQuestIds?.Count ?? 0) > 0)
            {
                return true;
            }
        }

        if (progress.score != null && progress.score.totalScore > 0)
        {
            return true;
        }

        return false;
    }
}

public sealed class UnityMenuSettingsApplier : IMenuSettingsApplier
{
    private static readonly Vector2Int[] ResolutionOptions =
    {
        new Vector2Int(1920, 1080),
        new Vector2Int(1600, 900),
        new Vector2Int(1280, 720)
    };

    private static readonly int[] QualityLevels = { 0, 1, 2 };

    public void Apply(MenuSettingsData settings)
    {
        MenuSettingsData source = settings ?? MenuSettingsFactory.CreateDefault();

        float musicVolume = source.musicEnabled ? Mathf.Clamp01(source.musicVolume) : 0f;
        float sfxVolume = source.sfxEnabled ? Mathf.Clamp01(source.sfxVolume) : 0f;

        AudioListener.volume = Mathf.Max(musicVolume, sfxVolume);
        Time.timeScale = 1f;

        int qualityIndex = Mathf.Clamp(source.qualityDropdownIndex, 0, QualityLevels.Length - 1);
        QualitySettings.SetQualityLevel(QualityLevels[qualityIndex], true);

        int resolutionIndex = Mathf.Clamp(source.resolutionDropdownIndex, 0, ResolutionOptions.Length - 1);
        Vector2Int resolution = ResolutionOptions[resolutionIndex];
        FullScreenMode fullScreenMode = source.windowedModeEnabled ? FullScreenMode.Windowed : FullScreenMode.FullScreenWindow;
        Screen.SetResolution(resolution.x, resolution.y, fullScreenMode);
    }
}

public sealed class RecordEntryData
{
    public int Rank { get; }
    public string Nickname { get; }
    public int Level { get; }
    public int Score { get; }

    public RecordEntryData(int rank, string nickname, int level, int score)
    {
        Rank = rank;
        Nickname = nickname;
        Level = level;
        Score = score;
    }
}

public interface IMenuRecordsService
{
    IReadOnlyList<RecordEntryData> GetSortedRecords(IEnumerable<MenuAccountData> accounts);
}

public sealed class MenuRecordsService : IMenuRecordsService
{
    public IReadOnlyList<RecordEntryData> GetSortedRecords(IEnumerable<MenuAccountData> accounts)
    {
        List<MenuAccountData> sorted = accounts?
            .Where(account => account != null)
            .OrderByDescending(account => account.playerData != null ? account.playerData.completedLevel : 0)
            .ThenByDescending(account => account.playerData != null ? account.playerData.score : 0)
            .ThenBy(account => account.username, StringComparer.OrdinalIgnoreCase)
            .ToList() ?? new List<MenuAccountData>();

        List<RecordEntryData> result = new List<RecordEntryData>(sorted.Count);

        for (int i = 0; i < sorted.Count; i++)
        {
            MenuAccountData account = sorted[i];
            int level = account.playerData != null ? account.playerData.completedLevel : 0;
            int score = account.playerData != null ? account.playerData.score : 0;
            result.Add(new RecordEntryData(i + 1, account.username, level, score));
        }

        return result;
    }
}

public interface IMenuAccountService
{
    MenuSaveRoot SaveData { get; }
    MenuAccountData CurrentAccount { get; }
    bool IsAuthenticated { get; }
    bool HasSavedGame { get; }
    MenuAccountData FindAccount(string username);
    bool TryAutoLogin();
    AuthOperationResult TryLogin(string username, string password);
    AuthOperationResult TryRegister(string username, string password);
    void Logout();
    void ResetProgress();
    void UpdateProgress(int completedLevel, int score);
    void Save();
}

public sealed class MenuAccountService : IMenuAccountService
{
    private readonly IMenuAccountRepository repository;
    private readonly IPasswordService passwordService;

    public MenuSaveRoot SaveData { get; private set; }
    public MenuAccountData CurrentAccount { get; private set; }
    public bool IsAuthenticated => CurrentAccount != null;
    public bool HasSavedGame => IsAuthenticated && GameProgressUtility.HasMeaningfulProgress(CurrentAccount.gameProgress);

    public MenuAccountService(IMenuAccountRepository repository, IPasswordService passwordService)
    {
        this.repository = repository;
        this.passwordService = passwordService;
        SaveData = repository.Load();
    }

    public MenuAccountData FindAccount(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return null;
        }

        return SaveData.accounts.FirstOrDefault(account =>
            string.Equals(account.username, username.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public bool TryAutoLogin()
    {
        if (SaveData.suppressAutoLogin || string.IsNullOrWhiteSpace(SaveData.lastLoggedInUser))
        {
            CurrentAccount = null;
            return false;
        }

        CurrentAccount = FindAccount(SaveData.lastLoggedInUser);
        return CurrentAccount != null;
    }

    public AuthOperationResult TryLogin(string username, string password)
    {
        string normalizedUsername = username?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedUsername))
        {
            return AuthOperationResult.Failure(AuthResultType.EmptyUsername, "Имя пользователя не может быть пустым.");
        }

        if (string.IsNullOrEmpty(password))
        {
            return AuthOperationResult.Failure(AuthResultType.EmptyPassword, "Пароль не может быть пустым.");
        }

        MenuAccountData account = FindAccount(normalizedUsername);
        if (account == null)
        {
            return AuthOperationResult.Failure(AuthResultType.UserNotFound, "Пользователь не найден.");
        }

        if (!string.Equals(account.passwordHash, passwordService.Hash(password), StringComparison.Ordinal))
        {
            return AuthOperationResult.Failure(AuthResultType.InvalidPassword, "Неверный пароль.");
        }

        SignIn(account, true);
        return AuthOperationResult.Success("Вход выполнен успешно.");
    }

    public AuthOperationResult TryRegister(string username, string password)
    {
        string normalizedUsername = username?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedUsername))
        {
            return AuthOperationResult.Failure(AuthResultType.EmptyUsername, "Имя пользователя не может быть пустым.");
        }

        if (FindAccount(normalizedUsername) != null)
        {
            return AuthOperationResult.Failure(AuthResultType.UserAlreadyExists, "Пользователь уже зарегистрирован.");
        }

        AuthOperationResult passwordValidation = passwordService.Validate(password);
        if (!passwordValidation.IsSuccess)
        {
            return passwordValidation;
        }

        MenuAccountData newAccount = new MenuAccountData
        {
            username = normalizedUsername,
            passwordHash = passwordService.Hash(password),
            settings = MenuSettingsFactory.CreateDefault(),
            playerData = new MenuPlayerProgressData(),
            gameProgress = GameProgressUtility.CreateDefault()
        };

        SaveData.accounts.Add(newAccount);
        SignIn(newAccount, true);
        return AuthOperationResult.Success("Регистрация выполнена успешно.");
    }

    public void Logout()
    {
        CurrentAccount = null;
        SaveData.lastLoggedInUser = string.Empty;
        SaveData.suppressAutoLogin = true;
        Save();
    }

    public void ResetProgress()
    {
        if (!IsAuthenticated)
        {
            return;
        }

        CurrentAccount.playerData = new MenuPlayerProgressData
        {
            completedLevel = 0,
            score = 0,
            lastUpdatedUtc = DateTime.UtcNow.ToString("O")
        };

        CurrentAccount.gameProgress = GameProgressUtility.CreateDefault();
        GameProgressUtility.Touch(CurrentAccount.gameProgress);

        Save();
    }

    public void UpdateProgress(int completedLevel, int score)
    {
        if (!IsAuthenticated)
        {
            return;
        }
 
        CurrentAccount.playerData ??= new MenuPlayerProgressData();
        CurrentAccount.playerData.completedLevel = Math.Max(CurrentAccount.playerData.completedLevel, completedLevel);
        CurrentAccount.playerData.score = Math.Max(0, score);
        CurrentAccount.playerData.lastUpdatedUtc = DateTime.UtcNow.ToString("O");
        CurrentAccount.gameProgress ??= GameProgressUtility.CreateDefault();
        CurrentAccount.gameProgress.score.totalScore = Math.Max(0, score);
        GameProgressUtility.Touch(CurrentAccount.gameProgress);
        Save();
    }

    public void Save()
    {
        if (CurrentAccount != null)
        {
            CurrentAccount.settings ??= MenuSettingsFactory.CreateDefault();
            CurrentAccount.playerData ??= new MenuPlayerProgressData();
            CurrentAccount.gameProgress ??= GameProgressUtility.CreateDefault();
        }

        repository.Save(SaveData);
    }

    private void SignIn(MenuAccountData account, bool allowAutoLogin)
    {
        CurrentAccount = account;
        SaveData.lastLoggedInUser = account.username;
        SaveData.suppressAutoLogin = !allowAutoLogin;
        Save();
    }
}
