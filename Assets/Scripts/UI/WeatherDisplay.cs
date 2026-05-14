using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;

public sealed class WeatherDisplay : MonoBehaviour
{
    [SerializeField] private WeatherSystem weatherSystem;
    [SerializeField] private TMP_Text weatherText;
    [SerializeField] private Scrollbar weatherTimer;

    private void Start()
    {
        if (weatherSystem == null) weatherSystem = FindFirstObjectByType<WeatherSystem>();
        if (weatherSystem != null) weatherSystem.OnWeatherChanged += UpdateDisplay;
    }

    private void OnDestroy()
    {
        if (weatherSystem != null) weatherSystem.OnWeatherChanged -= UpdateDisplay;
    }

    private void Update()
    {
        if (weatherSystem == null) return;
        if (weatherTimer != null) weatherTimer.size = weatherSystem.WeatherProgress;
    }

    private void UpdateDisplay(WeatherSystem.WeatherType weather)
    {
        if (weatherText == null) return;
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"Погода: {weatherSystem.GetWeatherName(weather)}");

        switch (weather)
        {
            case WeatherSystem.WeatherType.Rain:
                sb.AppendLine("(Дальность обзора снижена)");
                break;
            case WeatherSystem.WeatherType.Storm:
                sb.AppendLine("(Враги агрессивнее!)");
                break;
            case WeatherSystem.WeatherType.Fog:
                sb.AppendLine("(Скорость снижена, враги незаметны)");
                break;
            case WeatherSystem.WeatherType.Heatwave:
                sb.AppendLine("(Повышенный расход стамины)");
                break;
        }

        weatherText.text = sb.ToString();
    }
}
