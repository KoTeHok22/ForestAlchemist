using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central audio service: music layers, SFX pool, settings volumes.
/// See AUDIO.md section 4.
/// </summary>
public sealed class AudioManager : MonoBehaviour
{
    private const int SfxPoolSize = 10;
    private const string RegistryResourcePath = "Audio/AudioClipRegistry";

    private static AudioManager instance;
    public static AudioManager Instance => ResolveInstance();

    private static AudioManager ResolveInstance()
    {
        if (RuntimeSingletonGuard.IsShuttingDown)
        {
            return null;
        }

        if (instance == null)
        {
            instance = FindAnyObjectByType<AudioManager>(FindObjectsInactive.Include);
        }

        if (instance == null)
        {
            GameObject go = new GameObject("AudioManager");
            instance = go.AddComponent<AudioManager>();
            DontDestroyOnLoad(go);
        }

        return instance;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        ResolveInstance();
    }

    private AudioSource musicBase;
    private AudioSource musicOverlay;
    private AudioSource musicAmbience;
    private AudioSource musicStinger;
    private AudioSource sfxLoop;
    private readonly List<AudioSource> sfxPool = new List<AudioSource>();
    private int sfxPoolIndex;

    private float musicVolume = 1f;
    private float sfxVolume = 1f;
    private bool musicEnabled = true;
    private bool sfxEnabled = true;
    private float pauseMusicMultiplier = 1f;

    private MusicDirector musicDirector;
    private Coroutine overlayFadeRoutine;
    private Coroutine ambienceFadeRoutine;
    private string loopSfxId = string.Empty;
    private float musicOverlayLayerScale = 1f;
    private float musicAmbienceLayerScale = 1f;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        BuildSources();
        musicDirector = gameObject.AddComponent<MusicDirector>();
        if (GetComponent<AudioEventBridge>() == null)
        {
            gameObject.AddComponent<AudioEventBridge>();
        }
        SceneManager.sceneLoaded += HandleSceneLoaded;

        AudioListener.volume = 1f;
        ApplyGlobalSettings();
        musicDirector?.OnSceneLoaded(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void BuildSources()
    {
        musicBase = CreateChildSource("MusicBase", loop: true);
        musicOverlay = CreateChildSource("MusicOverlay", loop: true);
        musicAmbience = CreateChildSource("MusicAmbience", loop: true);
        musicStinger = CreateChildSource("MusicStinger", loop: false);
        sfxLoop = CreateChildSource("SfxLoop", loop: true);

        for (int i = 0; i < SfxPoolSize; i++)
        {
            sfxPool.Add(CreateChildSource($"Sfx_{i}", loop: false));
        }
    }

    private AudioSource CreateChildSource(string name, bool loop)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(transform, false);
        AudioSource source = child.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        return source;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        musicDirector?.OnSceneLoaded(scene.name);
    }

    public void ApplySettings(MenuSettingsData settings)
    {
        MenuSettingsData source = settings ?? MenuSettingsFactory.CreateDefault();
        musicEnabled = source.musicEnabled;
        sfxEnabled = source.sfxEnabled;
        musicVolume = Mathf.Clamp01(source.musicVolume);
        sfxVolume = Mathf.Clamp01(source.sfxVolume);
        RefreshVolumes();
    }

    public void ApplyGlobalSettings()
    {
        ApplySettings(GlobalSettingsStore.Current);
    }

    public void SetPauseMusicDucked(bool ducked)
    {
        pauseMusicMultiplier = ducked ? 0.35f : 1f;
        RefreshVolumes();
    }

    public void NotifyLoadingStarted()
    {
        musicDirector?.NotifyLoadingStarted();
    }

    public void NotifyLoadingFinished()
    {
        musicDirector?.NotifyLoadingFinished();
    }

    public void PlaySfx(string clipId, float volumeScale = 1f)
    {
        PlaySfxInternal(clipId, volumeScale, loop: false, usePool: true);
    }

    public void PlaySfxUnscaled(string clipId, float volumeScale = 1f)
    {
        if (!sfxEnabled)
        {
            return;
        }

        AudioClip clip = AudioCatalog.Get(clipId);
        if (clip == null)
        {
            return;
        }

        AudioSource source = RentSfxSource();
        source.clip = clip;
        source.volume = sfxVolume * AudioMixProfile.ResolveSfxVolume(clipId, volumeScale);
        source.loop = false;
        source.ignoreListenerPause = true;
        source.Play();
    }

    public void PlaySfxAtPoint(string clipId, Vector3 worldPosition, float volumeScale = 1f)
    {
        if (!sfxEnabled)
        {
            return;
        }

        AudioClip clip = AudioCatalog.Get(clipId);
        if (clip == null)
        {
            return;
        }

        float volume = sfxVolume * AudioMixProfile.ResolveSfxVolume(clipId, volumeScale);
        AudioSource.PlayClipAtPoint(clip, worldPosition, volume);
    }

    public void PlayUiClick()
    {
        PlaySfxUnscaled(AudioClipId.SfxUiButtonClick);
    }

    public void PlayUiPanelOpen()
    {
        PlaySfxUnscaled(AudioClipId.SfxUiPanelOpen);
    }

    public void PlayUiPanelClose()
    {
        PlaySfxUnscaled(AudioClipId.SfxUiPanelClose);
    }

    public void StartLoopSfx(string clipId, float volumeScale = 1f)
    {
        loopSfxId = clipId;
        PlaySfxInternal(clipId, volumeScale, loop: true, usePool: false);
    }

    public void StopLoopSfx()
    {
        loopSfxId = string.Empty;
        if (sfxLoop.isPlaying)
        {
            sfxLoop.Stop();
        }
    }

    public void PlayMusicBase(string clipId, float fadeSeconds = 2f)
    {
        CrossfadeSource(musicBase, clipId, fadeSeconds, loop: true, baseVolume: 1f);
    }

    public void PlayMusicOverlay(string clipId, float fadeSeconds, float targetVolume = 1f)
    {
        musicOverlayLayerScale = targetVolume;
        if (overlayFadeRoutine != null)
        {
            StopCoroutine(overlayFadeRoutine);
        }

        overlayFadeRoutine = StartCoroutine(CrossfadeCoroutine(musicOverlay, clipId, fadeSeconds, loop: true, targetVolume));
    }

    public void StopMusicOverlay(float fadeSeconds = 1.5f)
    {
        if (overlayFadeRoutine != null)
        {
            StopCoroutine(overlayFadeRoutine);
        }

        overlayFadeRoutine = StartCoroutine(FadeOutCoroutine(musicOverlay, fadeSeconds));
    }

    public void PlayMusicAmbience(string clipId, float fadeSeconds, float targetVolume = 1f)
    {
        musicAmbienceLayerScale = targetVolume;
        if (ambienceFadeRoutine != null)
        {
            StopCoroutine(ambienceFadeRoutine);
        }

        ambienceFadeRoutine = StartCoroutine(CrossfadeCoroutine(musicAmbience, clipId, fadeSeconds, loop: true, targetVolume));
    }

    public void StopMusicAmbience(float fadeSeconds = 1f)
    {
        if (ambienceFadeRoutine != null)
        {
            StopCoroutine(ambienceFadeRoutine);
        }

        ambienceFadeRoutine = StartCoroutine(FadeOutCoroutine(musicAmbience, fadeSeconds));
    }

    public void PlayMusicStinger(string clipId, float volumeScale = 1f)
    {
        AudioClip clip = AudioCatalog.Get(clipId);
        if (clip == null || !musicEnabled)
        {
            return;
        }

        musicStinger.Stop();
        musicStinger.clip = clip;
        musicStinger.loop = false;
        musicStinger.volume = musicVolume * pauseMusicMultiplier
            * AudioMixProfile.ResolveMusicVolume(clipId, volumeScale);
        musicStinger.Play();
    }

    public void StopAllMusic(float fadeSeconds = 0.5f)
    {
        StartCoroutine(FadeOutCoroutine(musicBase, fadeSeconds));
        StopMusicOverlay(fadeSeconds);
        StopMusicAmbience(fadeSeconds);
    }

    private void PlaySfxInternal(string clipId, float volumeScale, bool loop, bool usePool)
    {
        if (!sfxEnabled)
        {
            return;
        }

        AudioClip clip = AudioCatalog.Get(clipId);
        if (clip == null)
        {
            return;
        }

        AudioSource source = usePool ? RentSfxSource() : sfxLoop;
        source.clip = clip;
        source.loop = loop;
        source.volume = sfxVolume * AudioMixProfile.ResolveSfxVolume(clipId, volumeScale);
        source.ignoreListenerPause = loop;
        source.Play();
    }

    private AudioSource RentSfxSource()
    {
        AudioSource source = sfxPool[sfxPoolIndex];
        sfxPoolIndex = (sfxPoolIndex + 1) % sfxPool.Count;
        source.ignoreListenerPause = false;
        return source;
    }

    private void CrossfadeSource(AudioSource source, string clipId, float fadeSeconds, bool loop, float baseVolume)
    {
        StartCoroutine(CrossfadeCoroutine(source, clipId, fadeSeconds, loop, baseVolume));
    }

    private IEnumerator CrossfadeCoroutine(
        AudioSource source,
        string clipId,
        float fadeSeconds,
        bool loop,
        float targetVolume)
    {
        AudioClip clip = AudioCatalog.Get(clipId);
        if (clip == null)
        {
            yield break;
        }

        float startVolume = source.isPlaying ? source.volume : 0f;
        float elapsed = 0f;
        while (elapsed < fadeSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = fadeSeconds > 0f ? elapsed / fadeSeconds : 1f;
            source.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }

        source.Stop();
        source.clip = clip;
        source.loop = loop;
        source.volume = 0f;
        if (musicEnabled)
        {
            source.Play();
        }

        elapsed = 0f;
        float endVolume = musicVolume * pauseMusicMultiplier
            * AudioMixProfile.ResolveMusicVolume(clipId, targetVolume);
        while (elapsed < fadeSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = fadeSeconds > 0f ? elapsed / fadeSeconds : 1f;
            source.volume = Mathf.Lerp(0f, endVolume, t);
            yield return null;
        }

        source.volume = endVolume;
    }

    private IEnumerator FadeOutCoroutine(AudioSource source, float fadeSeconds)
    {
        if (!source.isPlaying)
        {
            yield break;
        }

        float startVolume = source.volume;
        float elapsed = 0f;
        while (elapsed < fadeSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = fadeSeconds > 0f ? elapsed / fadeSeconds : 1f;
            source.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }

        source.Stop();
        source.volume = 0f;
    }

    private void RefreshVolumes()
    {
        float music = musicEnabled ? musicVolume * pauseMusicMultiplier : 0f;
        if (musicBase.isPlaying && musicBase.clip != null)
        {
            musicBase.volume = music * AudioMixProfile.ResolveMusicVolume(musicBase.clip.name, 1f);
        }

        if (musicOverlay.isPlaying && musicOverlay.clip != null)
        {
            musicOverlay.volume = music * AudioMixProfile.ResolveMusicVolume(
                musicOverlay.clip.name, musicOverlayLayerScale);
        }

        if (musicAmbience.isPlaying && musicAmbience.clip != null)
        {
            musicAmbience.volume = music * AudioMixProfile.ResolveMusicVolume(
                musicAmbience.clip.name, musicAmbienceLayerScale);
        }

        if (musicStinger.isPlaying && musicStinger.clip != null)
        {
            musicStinger.volume = music * AudioMixProfile.ResolveMusicVolume(musicStinger.clip.name, 1f);
        }

        if (sfxLoop.isPlaying)
        {
            string id = string.IsNullOrEmpty(loopSfxId) && sfxLoop.clip != null ? sfxLoop.clip.name : loopSfxId;
            sfxLoop.volume = sfxEnabled
                ? sfxVolume * AudioMixProfile.ResolveSfxVolume(id, 1f)
                : 0f;
        }
    }
}
