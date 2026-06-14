using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Per-PC settings store shared by every account/player on this machine.
/// Graphics, audio and control bindings live here — a single JSON file in
/// <see cref="Application.persistentDataPath"/> — instead of inside each
/// account's save. Changing settings under one player therefore applies for
/// everyone who plays on the computer.
/// </summary>
public static class GlobalSettingsStore
{
    private const string FileName = "global_settings.json";

    private static MenuSettingsData cached;

    private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    /// <summary>Canonical machine-wide settings. Lazily loaded from disk.</summary>
    public static MenuSettingsData Current => cached ??= Load();

    /// <summary>Persist the given settings as the new machine-wide settings.</summary>
    public static void Save(MenuSettingsData settings)
    {
        cached = settings ?? MenuSettingsFactory.CreateDefault();
        cached.controlBindings ??= new List<ControlBindingEntry>();

        try
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(cached, true));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[GlobalSettingsStore] Не удалось сохранить настройки: {e.Message}");
        }
    }

    private static MenuSettingsData Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                MenuSettingsData data = JsonUtility.FromJson<MenuSettingsData>(File.ReadAllText(FilePath));
                if (data != null)
                {
                    data.controlBindings ??= new List<ControlBindingEntry>();
                    return data;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[GlobalSettingsStore] Не удалось загрузить настройки: {e.Message}");
        }

        // No file yet (fresh PC) → defaults, persisted so they exist next launch.
        MenuSettingsData defaults = MenuSettingsFactory.CreateDefault();
        Save(defaults);
        return defaults;
    }
}
