using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Scene music, combat/boss/weather layers, low-health ambience. Driven by AUDIO.md.
/// </summary>
public sealed class MusicDirector : MonoBehaviour
{
    private const float CombatRadius = 12f;
    private const float BossRadius = 18f;
    private const float CombatPollInterval = 0.4f;

    private AudioManager audioManager;
    private string activeScene = string.Empty;
    private WeatherSystem.WeatherType activeWeather = WeatherSystem.WeatherType.Clear;

    private enum OverlayMode
    {
        None,
        Weather,
        Combat,
        Boss
    }

    private OverlayMode overlayMode = OverlayMode.None;
    private string overlayClipId = string.Empty;
    private bool lowHealthActive;
    private float combatPollTimer;
    private float thunderCooldown;

    private void Awake()
    {
        audioManager = GetComponent<AudioManager>();
    }

    private void OnEnable()
    {
        WeatherSystem weather = FindFirstObjectByType<WeatherSystem>();
        if (weather != null)
        {
            weather.OnWeatherVisualChanged += HandleWeatherVisualChanged;
            activeWeather = weather.CurrentWeather;
        }

        ExpeditionManager expedition = ExpeditionManager.Instance;
        if (expedition != null)
        {
            expedition.OnExpeditionEnded += HandleExpeditionEnded;
        }
    }

    private void OnDisable()
    {
        WeatherSystem weather = FindFirstObjectByType<WeatherSystem>();
        if (weather != null)
        {
            weather.OnWeatherVisualChanged -= HandleWeatherVisualChanged;
        }

        ExpeditionManager expedition = ExpeditionManager.Instance;
        if (expedition != null)
        {
            expedition.OnExpeditionEnded -= HandleExpeditionEnded;
        }
    }

    private void Update()
    {
        if (activeScene != "Level" || audioManager == null)
        {
            return;
        }

        combatPollTimer -= Time.deltaTime;
        if (combatPollTimer <= 0f)
        {
            combatPollTimer = CombatPollInterval;
            RefreshCombatOverlay();
        }

        UpdateLowHealthLayer();
        UpdateStormThunderSfx();
    }

    public void OnSceneLoaded(string sceneName)
    {
        activeScene = sceneName;
        overlayMode = OverlayMode.None;
        overlayClipId = string.Empty;
        lowHealthActive = false;
        combatPollTimer = 0f;

        switch (sceneName)
        {
            case "MainMenu":
                audioManager.PlayMusicBase(AudioClipId.MusicMainMenu, 2f);
                audioManager.StopMusicOverlay(1f);
                audioManager.StopMusicAmbience(0.5f);
                break;
            case "Home":
                audioManager.PlayMusicBase(AudioClipId.MusicHomeBase, 2f);
                audioManager.StopMusicOverlay(1f);
                audioManager.StopMusicAmbience(0.5f);
                break;
            case "Level":
                audioManager.PlayMusicBase(AudioClipId.MusicExpeditionExplore, 2f);
                ApplyWeatherOverlay(activeWeather, immediate: true);
                break;
            default:
                audioManager.StopAllMusic(0.75f);
                break;
        }
    }

    public void NotifyLoadingStarted()
    {
        audioManager.PlayMusicOverlay(AudioClipId.MusicLoadingUnderscore, 0.75f, 1f);
    }

    public void NotifyLoadingFinished()
    {
        audioManager.StopMusicOverlay(0.75f);
        OnSceneLoaded(SceneManager.GetActiveScene().name);
    }

    private void HandleWeatherVisualChanged(WeatherSystem.WeatherType weather)
    {
        if (activeWeather == weather)
        {
            return;
        }

        activeWeather = weather;
        if (activeScene == "Level")
        {
            audioManager.PlaySfx(AudioClipId.SfxWeatherChangeWhoosh);
            ApplyWeatherOverlay(weather, immediate: false);
        }
    }

    private void HandleExpeditionEnded(ExpeditionResult result)
    {
        if (result == ExpeditionResult.Success)
        {
            audioManager.PlayMusicStinger(AudioClipId.MusicExpeditionSuccessStinger);
            GameProgressData progress = GameCore.Instance?.CurrentProgress;
            if (progress != null && progress.orcs.threatLevel >= 2)
            {
                audioManager.PlayMusicStinger(AudioClipId.MusicThreatLevelUp);
            }
        }
        else if (result == ExpeditionResult.Death)
        {
            audioManager.PlayMusicStinger(AudioClipId.MusicExpeditionDeath);
            audioManager.PlaySfx(AudioClipId.SfxExpeditionInventoryLost);
        }
    }

    private void RefreshCombatOverlay()
    {
        bool bossNear = false;
        bool combatNear = false;
        int threatLevel = GameCore.Instance?.CurrentProgress?.orcs?.threatLevel ?? 1;

        EnemyStateMachine[] enemies = FindObjectsByType<EnemyStateMachine>(FindObjectsSortMode.None);
        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyStateMachine machine = enemies[i];
            if (machine == null || machine.Health == null || !machine.Health.IsAlive)
            {
                continue;
            }

            float distance = machine.DistanceToPlayer();
            if (distance > BossRadius)
            {
                continue;
            }

            EnemyConfig config = machine.Config;
            if (config != null && config.isBoss)
            {
                bossNear = true;
                break;
            }

            if (distance <= CombatRadius && machine.IsAggressive)
            {
                combatNear = true;
            }
        }

        if (!combatNear && threatLevel >= 3)
        {
            combatNear = true;
        }

        if (bossNear)
        {
            SetOverlay(OverlayMode.Boss, AudioClipId.MusicBossWarchief, 1.5f);
        }
        else if (combatNear)
        {
            SetOverlay(OverlayMode.Combat, AudioClipId.MusicExpeditionCombatLayer, 1.5f);
        }
        else if (overlayMode == OverlayMode.Boss || overlayMode == OverlayMode.Combat)
        {
            RestoreWeatherOverlay();
        }
    }

    private void ApplyWeatherOverlay(WeatherSystem.WeatherType weather, bool immediate)
    {
        if (overlayMode == OverlayMode.Boss || overlayMode == OverlayMode.Combat)
        {
            return;
        }

        string clipId = ResolveWeatherClip(weather);
        float fade = immediate ? 0.05f : 5f;
        if (string.IsNullOrEmpty(clipId))
        {
            if (overlayMode == OverlayMode.Weather)
            {
                audioManager.StopMusicOverlay(fade);
                overlayMode = OverlayMode.None;
                overlayClipId = string.Empty;
            }

            return;
        }

        SetOverlay(OverlayMode.Weather, clipId, fade);
    }

    private void RestoreWeatherOverlay()
    {
        ApplyWeatherOverlay(activeWeather, immediate: false);
    }

    private void SetOverlay(OverlayMode mode, string clipId, float fadeSeconds)
    {
        if (overlayMode == mode && overlayClipId == clipId)
        {
            return;
        }

        overlayMode = mode;
        overlayClipId = clipId;
        audioManager.PlayMusicOverlay(clipId, fadeSeconds, 1f);
    }

    private static string ResolveWeatherClip(WeatherSystem.WeatherType weather)
    {
        switch (weather)
        {
            case WeatherSystem.WeatherType.Rain:
                return AudioClipId.MusicWeatherRainLayer;
            case WeatherSystem.WeatherType.Storm:
                return AudioClipId.MusicWeatherStormLayer;
            case WeatherSystem.WeatherType.Fog:
                return AudioClipId.MusicWeatherFogLayer;
            case WeatherSystem.WeatherType.Heatwave:
                return AudioClipId.MusicWeatherHeatwaveLayer;
            default:
                return null;
        }
    }

    private void UpdateLowHealthLayer()
    {
        PlayerHealth health = FindFirstObjectByType<PlayerHealth>();
        bool shouldPlay = health != null && health.IsAlive && health.MaxHealth > 0
            && (float)health.CurrentHealth / health.MaxHealth < 0.25f;

        if (shouldPlay == lowHealthActive)
        {
            return;
        }

        lowHealthActive = shouldPlay;
        if (shouldPlay)
        {
            audioManager.PlayMusicAmbience(AudioClipId.MusicLowHealthStinger, 1.5f, 1f);
        }
        else
        {
            audioManager.StopMusicAmbience(1f);
        }
    }

    private void UpdateStormThunderSfx()
    {
        if (activeWeather != WeatherSystem.WeatherType.Storm)
        {
            return;
        }

        thunderCooldown -= Time.deltaTime;
        if (thunderCooldown > 0f)
        {
            return;
        }

        if (Random.value < Time.deltaTime * 0.08f)
        {
            audioManager.PlaySfx(AudioClipId.SfxWeatherStormThunder);
            thunderCooldown = Random.Range(6f, 14f);
        }
    }
}
