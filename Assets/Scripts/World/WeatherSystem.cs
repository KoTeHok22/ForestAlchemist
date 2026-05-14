using UnityEngine;

public sealed class WeatherSystem : MonoBehaviour
{
    public enum WeatherType
    {
        Clear,
        Rain,
        Storm,
        Fog,
        Heatwave
    }

    [Header("Cycle")]
    [SerializeField] private float weatherChangeInterval = 120f;
    [SerializeField] private float transitionDuration = 5f;

    [Header("Weights")]
    [SerializeField] private float clearWeight = 4f;
    [SerializeField] private float rainWeight = 3f;
    [SerializeField] private float stormWeight = 1f;
    [SerializeField] private float fogWeight = 2f;
    [SerializeField] private float heatwaveWeight = 1.5f;

    private WeatherType currentWeather = WeatherType.Clear;
    private WeatherType targetWeather = WeatherType.Clear;
    private float weatherTimer;
    private float transitionTimer;
    private bool isTransitioning;

    public WeatherType CurrentWeather => isTransitioning ? targetWeather : currentWeather;
    public float WeatherProgress => weatherChangeInterval > 0 ? 1f - (weatherTimer / weatherChangeInterval) : 0f;

    public event System.Action<WeatherType> OnWeatherChanged;

    private void Start()
    {
        weatherTimer = weatherChangeInterval;
        currentWeather = WeatherType.Clear;
    }

    private void Update()
    {
        weatherTimer -= Time.deltaTime;
        if (weatherTimer <= 0f)
        {
            ChangeWeather();
            weatherTimer = weatherChangeInterval;
        }

        if (isTransitioning)
        {
            transitionTimer -= Time.deltaTime;
            if (transitionTimer <= 0f)
            {
                currentWeather = targetWeather;
                isTransitioning = false;
            }
        }
    }

    private void ChangeWeather()
    {
        float totalWeight = clearWeight + rainWeight + stormWeight + fogWeight + heatwaveWeight;
        float roll = Random.value * totalWeight;

        float cumulative = 0f;
        if ((cumulative += clearWeight) > roll) { targetWeather = WeatherType.Clear; }
        else if ((cumulative += rainWeight) > roll) { targetWeather = WeatherType.Rain; }
        else if ((cumulative += stormWeight) > roll) { targetWeather = WeatherType.Storm; }
        else if ((cumulative += fogWeight) > roll) { targetWeather = WeatherType.Fog; }
        else { targetWeather = WeatherType.Heatwave; }

        isTransitioning = true;
        transitionTimer = transitionDuration;
        OnWeatherChanged?.Invoke(targetWeather);
        QuestManager.Instance.ReportWeatherChanged(targetWeather);
    }

    public string GetWeatherName(WeatherType type)
    {
        switch (type)
        {
            case WeatherType.Clear: return "Ясно";
            case WeatherType.Rain: return "Дождь";
            case WeatherType.Storm: return "Гроза";
            case WeatherType.Fog: return "Туман";
            case WeatherType.Heatwave: return "Жара";
            default: return "Ясно";
        }
    }
}
