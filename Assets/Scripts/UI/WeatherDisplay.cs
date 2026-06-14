using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// App UI weather-change popup on Level. Replaces the legacy Canvas Weather panel.
/// Pauses gameplay briefly when the weather shifts.
/// </summary>
public sealed class WeatherDisplay : MonoBehaviour
{
    private const string ViewPath = "Assets/UI/LevelPanels/WeatherChangeView.uxml";
    private const string SettingsPath = "Assets/UI/LevelPanels/WeatherChangePanelSettings.asset";
    private const string ViewResourcePath = "UI/LevelPanels/WeatherChangeView";
    private const string SettingsResourcePath = "UI/LevelPanels/WeatherChangePanelSettings";

    private UIDocument document;
    private VisualElement root;
    private VisualElement panelRoot;
    private VisualElement dimBg;
    private Label weatherTitle;
    private Label weatherStatus;
    private AppUIClickRouter clickRouter;

    private WeatherSystem weatherSystem;
    private bool built;
    private bool isOpen;
    private bool pausedByPopup;

    private void Awake()
    {
        weatherSystem = FindFirstObjectByType<WeatherSystem>();
    }

    private void OnEnable()
    {
        EnsureBuilt();
        if (weatherSystem == null)
        {
            weatherSystem = FindFirstObjectByType<WeatherSystem>();
        }

        if (weatherSystem != null)
        {
            weatherSystem.OnWeatherChanged -= OpenForWeather;
            weatherSystem.OnWeatherChanged += OpenForWeather;
        }
    }

    private void OnDisable()
    {
        if (weatherSystem != null)
        {
            weatherSystem.OnWeatherChanged -= OpenForWeather;
        }

        ReleasePause();
    }

    private void OnDestroy()
    {
        ReleasePause();
    }

    public void Close()
    {
        if (!isOpen)
        {
            return;
        }

        isOpen = false;
        if (dimBg != null)
        {
            dimBg.style.display = DisplayStyle.None;
        }

        if (panelRoot != null)
        {
            panelRoot.pickingMode = PickingMode.Ignore;
        }

        ReleasePause();
        AudioHooks.PanelClose();
    }

    private bool EnsureBuilt()
    {
        if (built && panelRoot != null && dimBg != null)
        {
            return true;
        }

        document = GetComponent<UIDocument>();
        if (document == null)
        {
            document = gameObject.AddComponent<UIDocument>();
        }

        if (!HomePanelUiLoader.AssignAssets(document, ViewResourcePath, SettingsResourcePath, ViewPath, SettingsPath))
        {
            Debug.LogError("[WeatherDisplay] visualTreeAsset или panelSettings не найдены.");
            return false;
        }

        if (!HomePanelUiLoader.TryResolveShell(document, out root, out panelRoot, out dimBg))
        {
            Debug.LogError("[WeatherDisplay] Разметка панели неполная (panel-root/dim-bg).");
            return false;
        }

        weatherTitle = root.Q<Label>("weather-title");
        weatherStatus = root.Q<Label>("weather-status");

        clickRouter = new AppUIClickRouter(root);
        VisualElement closeBtn = root.Q<VisualElement>("btn-close");
        if (closeBtn != null)
        {
            clickRouter.Add(closeBtn, Close);
        }

        VisualElement closeX = root.Q<VisualElement>("btn-close-x");
        if (closeX != null)
        {
            clickRouter.Add(closeX, Close);
        }

        dimBg.style.display = DisplayStyle.None;
        panelRoot.pickingMode = PickingMode.Ignore;
        built = true;
        return true;
    }

    private void OpenForWeather(WeatherSystem.WeatherType weather)
    {
        if (!EnsureBuilt())
        {
            Debug.LogError("[WeatherDisplay] Не удалось показать смену погоды: UI не собран.");
            return;
        }

        if (weatherTitle != null)
        {
            weatherTitle.text = "Погода меняется";
        }

        if (weatherStatus != null)
        {
            weatherStatus.text = GetWeatherStatus(weather);
        }

        if (dimBg != null)
        {
            dimBg.style.display = DisplayStyle.Flex;
        }

        if (panelRoot != null)
        {
            panelRoot.pickingMode = PickingMode.Position;
        }

        if (!Mathf.Approximately(Time.timeScale, 0f))
        {
            Time.timeScale = 0f;
            pausedByPopup = true;
        }
        else
        {
            pausedByPopup = false;
        }

        isOpen = true;
        AudioHooks.PanelOpen();
    }

    private void ReleasePause()
    {
        if (!pausedByPopup)
        {
            return;
        }

        pausedByPopup = false;
        Time.timeScale = 1f;
    }

    private string GetWeatherStatus(WeatherSystem.WeatherType weather)
    {
        string weatherName = weatherSystem != null ? weatherSystem.GetWeatherName(weather) : weather.ToString();

        switch (weather)
        {
            case WeatherSystem.WeatherType.Clear:
                return $"Теперь: {weatherName}. Погода прояснилась, особые погодные ограничения сняты.";
            case WeatherSystem.WeatherType.Rain:
                return $"Теперь: {weatherName}. Дальность обзора снижена, небо затянуло осадками.";
            case WeatherSystem.WeatherType.Storm:
                return $"Теперь: {weatherName}. Ливень усилился, враги становятся агрессивнее.";
            case WeatherSystem.WeatherType.Fog:
                return $"Теперь: {weatherName}. Скорость снижена, а врагов труднее заметить.";
            case WeatherSystem.WeatherType.Heatwave:
                return $"Теперь: {weatherName}. Стамина расходуется быстрее.";
            default:
                return $"Теперь: {weatherName}.";
        }
    }
}
