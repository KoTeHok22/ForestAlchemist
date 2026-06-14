using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Debug hotkeys for forcing weather on the Level scene (no gameplay popup / quest ping).
/// F5 — ясно, F6 — дождь, F7 — туман, F8 — гроза, F9 — жара.
/// </summary>
public sealed class WeatherDebugInput : MonoBehaviour
{
    [SerializeField] private WeatherSystem weatherSystem;
    [SerializeField] private bool enableDebugKeys = true;

    private void Awake()
    {
        if (weatherSystem == null)
        {
            weatherSystem = GetComponent<WeatherSystem>();
        }

        if (weatherSystem == null)
        {
            weatherSystem = FindFirstObjectByType<WeatherSystem>();
        }
    }

    private void Update()
    {
        if (!enableDebugKeys || weatherSystem == null || Keyboard.current == null)
        {
            return;
        }

        if (SceneManager.GetActiveScene().name != "Level")
        {
            return;
        }

        if (Keyboard.current.f5Key.wasPressedThisFrame)
        {
            ForceWeather(WeatherSystem.WeatherType.Clear);
        }
        else if (Keyboard.current.f6Key.wasPressedThisFrame)
        {
            ForceWeather(WeatherSystem.WeatherType.Rain);
        }
        else if (Keyboard.current.f7Key.wasPressedThisFrame)
        {
            ForceWeather(WeatherSystem.WeatherType.Fog);
        }
        else if (Keyboard.current.f8Key.wasPressedThisFrame)
        {
            ForceWeather(WeatherSystem.WeatherType.Storm);
        }
        else if (Keyboard.current.f9Key.wasPressedThisFrame)
        {
            ForceWeather(WeatherSystem.WeatherType.Heatwave);
        }
    }

    private void ForceWeather(WeatherSystem.WeatherType weather)
    {
        weatherSystem.SetWeather(weather, notifyGameplay: false);
        Debug.Log($"[WeatherDebug] Принудительно: {weatherSystem.GetWeatherName(weather)} ({weather})");
    }
}
