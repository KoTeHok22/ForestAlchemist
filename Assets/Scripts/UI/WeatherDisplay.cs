using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class WeatherDisplay : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private WeatherSystem weatherSystem;
    [SerializeField] private TMP_Text weatherTitle;
    [SerializeField] private TMP_Text weatherStatus;
    [SerializeField] private Button closeButton;

    private bool isOpen;
    private bool pausedByPopup;

    private void Awake()
    {
        if (panelRoot == null)
        {
            panelRoot = gameObject;
        }

        if (weatherSystem == null)
        {
            weatherSystem = FindFirstObjectByType<WeatherSystem>();
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
            closeButton.onClick.AddListener(Close);
        }

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    private void OnEnable()
    {
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
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
        }

        ReleasePause();
    }

    private void OpenForWeather(WeatherSystem.WeatherType weather)
    {
        if (weatherTitle != null)
        {
            weatherTitle.text = "Погода меняется";
        }

        if (weatherStatus != null)
        {
            weatherStatus.text = GetWeatherStatus(weather);
        }

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
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
    }

    public void Close()
    {
        if (!isOpen)
        {
            return;
        }

        isOpen = false;

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        ReleasePause();
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
                return $"Теперь: {weatherName}. Дальность обзора снижена.";
            case WeatherSystem.WeatherType.Storm:
                return $"Теперь: {weatherName}. Враги становятся агрессивнее.";
            case WeatherSystem.WeatherType.Fog:
                return $"Теперь: {weatherName}. Скорость снижена, а врагов труднее заметить.";
            case WeatherSystem.WeatherType.Heatwave:
                return $"Теперь: {weatherName}. Стамина расходуется быстрее.";
            default:
                return $"Теперь: {weatherName}.";
        }
    }
}
