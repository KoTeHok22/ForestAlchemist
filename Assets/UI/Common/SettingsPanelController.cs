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

    private Dropdown resolution;
    private Dropdown quality;
    private Toggle windowed;
    private Toggle musicEnabled;
    private Toggle sfxEnabled;
    private SliderFloat musicVolume;
    private SliderFloat sfxVolume;

    private bool isApplyingSettings;

    /// <param name="accountService">
    /// Unused — settings are now machine-wide (see <see cref="GlobalSettingsStore"/>).
    /// Kept in the signature so existing call sites need not change.
    /// </param>
    public SettingsPanelController(VisualElement root, IMenuSettingsApplier applier, IMenuAccountService accountService)
    {
        this.settingsApplier = applier;

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
                "1280x720",
                "1600x900",
                "1920x1080",
                "2560x1440",
                "3840x2160"
            };
            resolution.bindItem = (item, i) => item.label = (string)resolution.sourceItems[i];
        }

        if (quality != null)
        {
            quality.sourceItems = new List<string> { "Низкое", "Среднее", "Высокое", "Очень высокое" };
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
        BindLiveVolumeSlider(musicVolume);
        BindLiveVolumeSlider(sfxVolume);
    }

    private void BindLiveVolumeSlider(SliderFloat slider)
    {
        if (slider == null)
        {
            return;
        }

        slider.RegisterCallback<ChangingEvent<float>>(_ => ApplyLivePreview());
        slider.RegisterValueChangedCallback(_ => OnChanged());

        VisualElement innerSlider = slider.Q(className: "unity-base-slider");
        if (innerSlider != null)
        {
            innerSlider.RegisterCallback<ChangeEvent<float>>(_ => ApplyLivePreview());
        }
    }

    /// <summary>Push the currently saved (machine-wide) settings into the controls (no callbacks).</summary>
    public void PushCurrent()
    {
        PushToControls(GlobalSettingsStore.Current);
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

    private void ApplyLivePreview()
    {
        if (isApplyingSettings)
        {
            return;
        }

        settingsApplier?.Apply(CaptureFromUi());
    }

    private void OnChanged()
    {
        if (isApplyingSettings) return;

        MenuSettingsData settings = CaptureFromUi();
        settingsApplier?.Apply(settings);

        // Machine-wide: saved once for every account/player on this PC.
        GlobalSettingsStore.Save(settings);
    }

    private MenuSettingsData CaptureFromUi()
    {
        // Base on the stored settings (never CreateDefault here — it resets live
        // control bindings as a side effect). Override only what this panel edits.
        MenuSettingsData baseline = GlobalSettingsStore.Current;
        MenuSettingsData captured = new MenuSettingsData
        {
            musicVolume             = musicVolume  != null ? musicVolume.value : baseline.musicVolume,
            sfxVolume               = sfxVolume    != null ? sfxVolume.value   : baseline.sfxVolume,
            resolutionDropdownIndex = resolution   != null ? resolution.selectedIndex : baseline.resolutionDropdownIndex,
            qualityDropdownIndex    = quality      != null ? quality.selectedIndex    : baseline.qualityDropdownIndex,
            musicEnabled            = musicEnabled != null ? musicEnabled.value : baseline.musicEnabled,
            sfxEnabled              = sfxEnabled   != null ? sfxEnabled.value   : baseline.sfxEnabled,
            windowedModeEnabled     = windowed     != null ? windowed.value     : baseline.windowedModeEnabled
        };

        // This panel has no rebind UI, so preserve the current control bindings.
        GameControls.WriteTo(captured);
        return captured;
    }
}
