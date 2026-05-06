using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public interface IMenuAccountRepository
{
    MenuSaveRoot Load();
    void Save(MenuSaveRoot data);
}

public sealed class JsonMenuAccountRepository : IMenuAccountRepository
{
    private readonly string saveFilePath;

    public JsonMenuAccountRepository(string saveFileName)
    {
        saveFilePath = Path.Combine(Application.persistentDataPath, saveFileName);
    }

    public MenuSaveRoot Load()
    {
        if (!File.Exists(saveFilePath))
        {
            MenuSaveRoot created = CreateEmptySave();
            Save(created);
            return created;
        }

        try
        {
            string json = File.ReadAllText(saveFilePath);
            MenuSaveRoot loaded = JsonUtility.FromJson<MenuSaveRoot>(json);
            Normalize(loaded);
            return loaded;
        }
        catch
        {
            MenuSaveRoot fallback = CreateEmptySave();
            Save(fallback);
            return fallback;
        }
    }

    public void Save(MenuSaveRoot data)
    {
        Normalize(data);

        string directory = Path.GetDirectoryName(saveFilePath);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(saveFilePath, json, Encoding.UTF8);
    }

    private static void Normalize(MenuSaveRoot data)
    {
        if (data == null)
        {
            return;
        }

        data.lastLoggedInUser ??= string.Empty;
        data.accounts ??= new System.Collections.Generic.List<MenuAccountData>();

        foreach (MenuAccountData account in data.accounts.Where(account => account != null))
        {
            account.username ??= string.Empty;
            account.passwordHash ??= string.Empty;
            account.settings ??= MenuSettingsFactory.CreateDefault();
            account.playerData ??= new MenuPlayerProgressData();
            account.playerData.lastUpdatedUtc ??= string.Empty;
        }
    }

    private static MenuSaveRoot CreateEmptySave()
    {
        return new MenuSaveRoot();
    }
}
