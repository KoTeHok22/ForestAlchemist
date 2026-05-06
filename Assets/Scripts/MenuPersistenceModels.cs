using System;
using System.Collections.Generic;

[Serializable]
public sealed class MenuSaveRoot
{
    public string lastLoggedInUser = string.Empty;
    public bool suppressAutoLogin;
    public List<MenuAccountData> accounts = new List<MenuAccountData>();
}

[Serializable]
public sealed class MenuAccountData
{
    public string username = string.Empty;
    public string passwordHash = string.Empty;
    public MenuSettingsData settings = new MenuSettingsData();
    public MenuPlayerProgressData playerData = new MenuPlayerProgressData();
}

[Serializable]
public sealed class MenuSettingsData
{
    public float musicVolume = 1f;
    public float sfxVolume = 1f;
    public int qualityDropdownIndex;
    public int resolutionDropdownIndex;
    public bool musicEnabled = true;
    public bool sfxEnabled = true;
    public bool windowedModeEnabled = true;
}

[Serializable]
public sealed class MenuPlayerProgressData
{
    public int completedLevel;
    public int score;
    public string lastUpdatedUtc = string.Empty;
}
