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
    [SerializeField] private float minWeatherInterval = 40f;
    [SerializeField] private float maxWeatherInterval = 150f;
    [SerializeField] private float transitionDuration = 5f;
    [SerializeField] private float suddenShiftChancePerSecond = 0.025f;

    [Header("Weights")]
    [SerializeField] private float clearWeight = 2.5f;
    [SerializeField] private float rainWeight = 3f;
    [SerializeField] private float stormWeight = 1.4f;
    [SerializeField] private float fogWeight = 2.5f;
    [SerializeField] private float heatwaveWeight = 2f;

    private WeatherType currentWeather = WeatherType.Clear;
    private WeatherType targetWeather = WeatherType.Clear;
    private float weatherTimer;
    private float transitionTimer;
    private bool isTransitioning;

    public WeatherType CurrentWeather => isTransitioning ? targetWeather : currentWeather;
    public WeatherType ActiveWeather => currentWeather;
    public WeatherType IncomingWeather => targetWeather;
    public bool IsTransitioning => isTransitioning;
    public float WeatherProgress => GetScheduledInterval() > 0f ? 1f - (weatherTimer / GetScheduledInterval()) : 0f;
    public float TransitionBlend => isTransitioning && transitionDuration > 0f
        ? 1f - Mathf.Clamp01(transitionTimer / transitionDuration)
        : 1f;

    public event System.Action<WeatherType> OnWeatherChanged;
    public event System.Action<WeatherType> OnWeatherVisualChanged;

    private void Start()
    {
        ScheduleNextWeather();
        currentWeather = WeatherType.Clear;
        targetWeather = WeatherType.Clear;
        OnWeatherVisualChanged?.Invoke(currentWeather);
    }

    private void Update()
    {
        weatherTimer -= Time.deltaTime;

        float scheduled = GetScheduledInterval();
        if (scheduled > 20f && weatherTimer > 10f && weatherTimer < scheduled * 0.75f)
        {
            if (Random.value < suddenShiftChancePerSecond * Time.deltaTime)
            {
                PickAndApplyNextWeather(resetCycleTimer: true);
                return;
            }
        }

        if (weatherTimer <= 0f)
        {
            PickAndApplyNextWeather(resetCycleTimer: true);
        }

        if (isTransitioning)
        {
            transitionTimer -= Time.deltaTime;
            if (transitionTimer <= 0f)
            {
                currentWeather = targetWeather;
                isTransitioning = false;
                OnWeatherVisualChanged?.Invoke(currentWeather);
            }
        }
    }

    public void SetWeather(WeatherType weather, bool notifyGameplay = true, bool resetCycleTimer = true)
    {
        if (!isTransitioning && currentWeather == weather)
        {
            return;
        }

        targetWeather = weather;
        isTransitioning = true;
        transitionTimer = transitionDuration;

        if (resetCycleTimer)
        {
            ScheduleNextWeather();
        }

        OnWeatherVisualChanged?.Invoke(targetWeather);

        if (!notifyGameplay)
        {
            return;
        }

        OnWeatherChanged?.Invoke(targetWeather);
        QuestManager questManager = QuestManager.Instance;
        if (questManager != null)
        {
            questManager.ReportWeatherChanged(targetWeather);
        }
    }

    private void PickAndApplyNextWeather(bool resetCycleTimer)
    {
        WeatherType next = RollNextWeather(avoidRepeat: true);
        SetWeather(next, notifyGameplay: true, resetCycleTimer: resetCycleTimer);
    }

    private WeatherType RollNextWeather(bool avoidRepeat)
    {
        WeatherType reference = isTransitioning ? targetWeather : currentWeather;
        float totalWeight = clearWeight + rainWeight + stormWeight + fogWeight + heatwaveWeight;
        const int maxAttempts = 8;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            float roll = Random.value * totalWeight;
            float cumulative = 0f;
            WeatherType candidate = WeatherType.Heatwave;

            if ((cumulative += clearWeight) > roll)
            {
                candidate = WeatherType.Clear;
            }
            else if ((cumulative += rainWeight) > roll)
            {
                candidate = WeatherType.Rain;
            }
            else if ((cumulative += stormWeight) > roll)
            {
                candidate = WeatherType.Storm;
            }
            else if ((cumulative += fogWeight) > roll)
            {
                candidate = WeatherType.Fog;
            }

            if (!avoidRepeat || candidate != reference || attempt == maxAttempts - 1)
            {
                return candidate;
            }
        }

        return WeatherType.Rain;
    }

    private void ScheduleNextWeather()
    {
        weatherTimer = Random.Range(minWeatherInterval, maxWeatherInterval);
    }

    private float GetScheduledInterval()
    {
        return Mathf.Max(minWeatherInterval, weatherTimer);
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
