using System.Collections.Generic;
using Unity.AppUI.UI;
using UnityEngine.UIElements;
using Toggle = Unity.AppUI.UI.Toggle;

/// <summary>
/// Shared settings logic for App UI. Binds the controls inside a settings
/// fragment (graphics + sound) to <see cref="MenuSettingsData"/> and applies
/// them live. Used by both the MainMenu and the in-game Pause overlay so the
/// settings UI exists in exactly one place.
/// </summary>
public sealed class SettingsPanelController
{
    private readonly IMenuSettingsApplier settingsApplier;
    private readonly IMenuAccountService accountService;

    private Dropdown resolution;
    private Dropdown quality;
    private Toggle windowed;
    private Toggle musicEnabled;
    private Toggle sfxEnabled;
    private SliderFloat musicVolume;
    private SliderFloat sfxVolume;

    private bool isApplyingSettings;

    public SettingsPanelController(VisualElement root, IMenuSettingsApplier applier, IMenuAccountService accountService)
    {
        this.settingsApplier = applier;
        this.accountService = accountService;

        CacheElements(root);
        PopulateDropdowns();
        BindControls();
    }

    private void CacheElements(VisualElement root)
    {
        resolution   = root.Q<Dropdown>("settings-resolution");
        quality      = root.Q<Dropdown>("settings-quality");
        windowed     = root.Q<Toggle>("settings-windowed");
        musicEnabled = root.Q<Toggle>("settings-music-enabled");
        sfxEnabled   = root.Q<Toggle>("settings-sfx-enabled");
        musicVolume  = root.Q<SliderFloat>("settings-music-volume");
        sfxVolume    = root.Q<SliderFloat>("settings-sfx-volume");
    }

    private void PopulateDropdowns()
    {
        if (resolution != null)
        {
            resolution.sourceItems = new List<string>
            {
                "1920x1080",
                "1600x900",
                "1280x720"
            };
            resolution.bindItem = (item, i) => item.label = (string)resolution.sourceItems[i];
        }

        if (quality != null)
        {
            quality.sourceItems = new List<string> { "Низкое", "Среднее", "Высокое" };
            quality.bindItem = (item, i) => item.label = (string)quality.sourceItems[i];
        }
    }

    private void BindControls()
    {
        resolution?.RegisterValueChangedCallback(_ => OnChanged());
        quality?.RegisterValueChangedCallback(_ => OnChanged());
        windowed?.RegisterValueChangedCallback(_ => OnChanged());
        musicEnabled?.RegisterValueChangedCallback(_ => OnChanged());
        sfxEnabled?.RegisterValueChangedCallback(_ => OnChanged());
        musicVolume?.RegisterValueChangedCallback(_ => OnChanged());
        sfxVolume?.RegisterValueChangedCallback(_ => OnChanged());
    }

    /// <summary>Push the currently saved settings into the controls (no callbacks).</summary>
    public void PushCurrent()
    {
        MenuSettingsData src = accountService != null && accountService.IsAuthenticated
            ? accountService.CurrentAccount.settings
            : MenuSettingsFactory.CreateDefault();
        PushToControls(src);
    }

    public void PushToControls(MenuSettingsData settings)
    {
        MenuSettingsData src = settings ?? MenuSettingsFactory.CreateDefault();
        isApplyingSettings = true;

        musicVolume?.SetValueWithoutNotify(src.musicVolume);
        sfxVolume?.SetValueWithoutNotify(src.sfxVolume);
        resolution?.SetValueWithoutNotify(new[] { src.resolutionDropdownIndex });
        quality?.SetValueWithoutNotify(new[] { src.qualityDropdownIndex });
        musicEnabled?.SetValueWithoutNotify(src.musicEnabled);
        sfxEnabled?.SetValueWithoutNotify(src.sfxEnabled);
        windowed?.SetValueWithoutNotify(src.windowedModeEnabled);

        isApplyingSettings = false;
        settingsApplier?.Apply(src);
    }

    private void OnChanged()
    {
        if (isApplyingSettings) return;

        MenuSettingsData settings = CaptureFromUi();
        settingsApplier?.Apply(settings);

        if (accountService == null || !accountService.IsAuthenticated) return;

        accountService.CurrentAccount.settings = settings;
        accountService.Save();
    }

    private MenuSettingsData CaptureFromUi()
    {
        MenuSettingsData defaults = MenuSettingsFactory.CreateDefault();
        return new MenuSettingsData
        {
            musicVolume             = musicVolume  != null ? musicVolume.value : defaults.musicVolume,
            sfxVolume               = sfxVolume    != null ? sfxVolume.value   : defaults.sfxVolume,
            resolutionDropdownIndex = resolution   != null ? resolution.selectedIndex : defaults.resolutionDropdownIndex,
            qualityDropdownIndex    = quality      != null ? quality.selectedIndex    : defaults.qualityDropdownIndex,
            musicEnabled            = musicEnabled != null ? musicEnabled.value : defaults.musicEnabled,
            sfxEnabled              = sfxEnabled   != null ? sfxEnabled.value   : defaults.sfxEnabled,
            windowedModeEnabled     = windowed     != null ? windowed.value     : defaults.windowedModeEnabled
        };
    }
}
