using UnityEngine;

/// <summary>
/// Screen-space and particle visuals for <see cref="WeatherSystem"/> on the Level scene.
/// </summary>
public sealed class WeatherVisualController : MonoBehaviour
{
    private const int ParticleSortingOrder = 31000;
    // Tint must draw on top of every particle/world sprite so it can colour them.
    private const int TintSortingOrder = ParticleSortingOrder + 1000;

    [SerializeField] private WeatherSystem weatherSystem;
    [SerializeField] private float intensitySmoothing = 3f;

    private Camera targetCamera;
    private Color baseBackgroundColor;
    private bool hasBaseBackgroundColor;

    // Full-screen colour overlay. A plain SpriteRenderer instead of a UIDocument so
    // it renders identically in the editor and in a build (the old UI Toolkit path
    // loaded its assets via AssetDatabase under #if UNITY_EDITOR and was invisible
    // in player builds — that broke fog and the heat-haze tint).
    private SpriteRenderer tintRenderer;
    private Sprite tintSprite;

    private ParticleSystem rainParticles;
    private ParticleSystem stormParticles;
    private ParticleSystem heatParticles;

    private Material particleMaterial;

    private float rainIntensity;
    private float stormIntensity;
    private float fogIntensity;
    private float heatIntensity;
    private float lightningFlash;
    private float lightningCooldown;

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

        BuildVisuals();
    }

    private void OnEnable()
    {
        if (weatherSystem != null)
        {
            weatherSystem.OnWeatherVisualChanged += HandleWeatherVisualChanged;
        }
    }

    private void OnDisable()
    {
        if (weatherSystem != null)
        {
            weatherSystem.OnWeatherVisualChanged -= HandleWeatherVisualChanged;
        }
    }

    private void HandleWeatherVisualChanged(WeatherSystem.WeatherType weather)
    {
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera != null && !hasBaseBackgroundColor)
            {
                baseBackgroundColor = targetCamera.backgroundColor;
                hasBaseBackgroundColor = true;
            }
        }

        if (targetCamera == null)
        {
            return;
        }

        transform.position = targetCamera.transform.position;
        transform.rotation = targetCamera.transform.rotation;

        float blend = weatherSystem != null ? weatherSystem.TransitionBlend : 1f;
        WeatherSystem.WeatherType from = weatherSystem != null ? weatherSystem.ActiveWeather : WeatherSystem.WeatherType.Clear;
        WeatherSystem.WeatherType to = weatherSystem != null ? weatherSystem.IncomingWeather : WeatherSystem.WeatherType.Clear;
        GetIntensityTargets(from, out float rainFrom, out float stormFrom, out float fogFrom, out float heatFrom);
        GetIntensityTargets(to, out float rainTo, out float stormTo, out float fogTo, out float heatTo);

        float rainTarget = Mathf.Lerp(rainFrom, rainTo, blend);
        float stormTarget = Mathf.Lerp(stormFrom, stormTo, blend);
        float fogTarget = Mathf.Lerp(fogFrom, fogTo, blend);
        float heatTarget = Mathf.Lerp(heatFrom, heatTo, blend);

        float delta = Time.deltaTime * Mathf.Max(0.01f, intensitySmoothing);
        rainIntensity = Mathf.MoveTowards(rainIntensity, rainTarget, delta);
        stormIntensity = Mathf.MoveTowards(stormIntensity, stormTarget, delta);
        fogIntensity = Mathf.MoveTowards(fogIntensity, fogTarget, delta);
        heatIntensity = Mathf.MoveTowards(heatIntensity, heatTarget, delta);

        UpdateScreenTint();
        UpdateParticles();
        UpdateCameraTint();
        UpdateLightning();
    }

    private static void GetIntensityTargets(
        WeatherSystem.WeatherType weather,
        out float rain,
        out float storm,
        out float fog,
        out float heat)
    {
        rain = 0f;
        storm = 0f;
        fog = 0f;
        heat = 0f;

        switch (weather)
        {
            case WeatherSystem.WeatherType.Rain:
                rain = 1f;
                break;
            case WeatherSystem.WeatherType.Storm:
                rain = 1f;
                storm = 1f;
                break;
            case WeatherSystem.WeatherType.Fog:
                fog = 1f;
                break;
            case WeatherSystem.WeatherType.Heatwave:
                heat = 1f;
                break;
        }
    }

    private void BuildVisuals()
    {
        BuildScreenTint();
        particleMaterial = CreateParticleMaterial();
        rainParticles = CreateRainSystem("RainParticles", new Color(0.78f, 0.88f, 1f, 0.92f));
        stormParticles = CreateRainSystem("StormParticles", new Color(0.62f, 0.72f, 0.92f, 0.96f));
        heatParticles = CreateHeatSystem();
    }

    private void BuildScreenTint()
    {
        GameObject host = new GameObject("WeatherScreenTint");
        host.transform.SetParent(transform, false);
        host.transform.localPosition = new Vector3(0f, 0f, 1f);

        tintSprite = CreateWhiteSprite();
        tintRenderer = host.AddComponent<SpriteRenderer>();
        tintRenderer.sprite = tintSprite;
        tintRenderer.color = new Color(0f, 0f, 0f, 0f);
        tintRenderer.sortingOrder = TintSortingOrder;
    }

    private static Sprite CreateWhiteSprite()
    {
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        Color32 white = new Color32(255, 255, 255, 255);
        texture.SetPixels32(new[] { white, white, white, white });
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f), 1f);
    }

    private Material CreateParticleMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Particles/Standard Unlit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        return new Material(shader) { color = Color.white };
    }

    private static void SetVelocityTwoConstants(
        ParticleSystem.VelocityOverLifetimeModule velocity,
        float xMin,
        float xMax,
        float yMin,
        float yMax,
        float zMin = 0f,
        float zMax = 0f)
    {
        velocity.enabled = true;
        velocity.x = new ParticleSystem.MinMaxCurve(xMin, xMax);
        velocity.y = new ParticleSystem.MinMaxCurve(yMin, yMax);
        velocity.z = new ParticleSystem.MinMaxCurve(zMin, zMax);
    }

    private ParticleSystem CreateRainSystem(string objectName, Color color)
    {
        GameObject host = new GameObject(objectName);
        host.transform.SetParent(transform, false);

        ParticleSystem ps = host.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = 2.5f;
        main.startSpeed = 0f;
        main.startSize = 0.16f;
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 4000;
        main.gravityModifier = 0f;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(30f, 20f, 1f);

        SetVelocityTwoConstants(ps.velocityOverLifetime, -1.5f, 1.5f, -14f, -10f);

        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 0.55f;
        renderer.velocityScale = 0.12f;
        renderer.material = particleMaterial;
        renderer.sortingOrder = ParticleSortingOrder;

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return ps;
    }

    private ParticleSystem CreateHeatSystem()
    {
        GameObject host = new GameObject("HeatParticles");
        host.transform.SetParent(transform, false);
        host.transform.localPosition = Vector3.zero;

        ParticleSystem ps = host.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        // Longer life so a particle spawned anywhere on screen drifts a while.
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
        main.startColor = new Color(1f, 0.62f, 0.2f, 0.22f);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 600;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 24f;

        // Box covering the full view (resized each frame in UpdateParticles) so the
        // shimmer rises from everywhere, not just a strip across the middle.
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(34f, 20f, 1f);

        SetVelocityTwoConstants(ps.velocityOverLifetime, -0.2f, 0.2f, 0.8f, 2.2f);

        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = particleMaterial;
        renderer.sortingOrder = ParticleSortingOrder - 10;

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return ps;
    }

    private void UpdateScreenTint()
    {
        if (tintRenderer == null)
        {
            return;
        }

        Color fogColor = new Color(0.78f, 0.82f, 0.86f, 0.58f * fogIntensity);
        Color heatColor = new Color(1f, 0.55f, 0.18f, 0.32f * heatIntensity);
        Color stormColor = new Color(0.08f, 0.1f, 0.16f, 0.28f * stormIntensity);
        Color lightningColor = new Color(0.95f, 0.97f, 1f, 0.42f * lightningFlash);

        Color combined = fogColor;
        combined = AlphaBlend(combined, heatColor);
        combined = AlphaBlend(combined, stormColor);
        combined = AlphaBlend(combined, lightningColor);
        tintRenderer.color = combined;

        // Stretch the 2x2 sprite to cover the whole orthographic view (+margin).
        if (targetCamera != null && targetCamera.orthographic)
        {
            float height = targetCamera.orthographicSize * 2f + 4f;
            float width = height * targetCamera.aspect + 4f;
            tintRenderer.transform.localScale = new Vector3(width * 0.5f, height * 0.5f, 1f);
        }
    }

    private void UpdateCameraTint()
    {
        if (targetCamera == null || !hasBaseBackgroundColor)
        {
            return;
        }

        Color rainTint = Color.Lerp(baseBackgroundColor, new Color(0.14f, 0.2f, 0.28f, 1f), rainIntensity * 0.28f);
        Color fogTint = Color.Lerp(rainTint, new Color(0.28f, 0.3f, 0.33f, 1f), fogIntensity * 0.35f);
        Color heatTint = Color.Lerp(fogTint, new Color(0.34f, 0.22f, 0.12f, 1f), heatIntensity * 0.35f);
        Color stormTint = Color.Lerp(heatTint, new Color(0.1f, 0.12f, 0.18f, 1f), stormIntensity * 0.3f);
        targetCamera.backgroundColor = stormTint;
    }

    private void UpdateParticles()
    {
        float rainRate = 420f + stormIntensity * 380f;
        ConfigureRainEmitter(
            rainParticles,
            Mathf.Max(rainIntensity, stormIntensity * 0.35f),
            rainRate,
            0.12f,
            0.22f,
            1.5f + stormIntensity * 2.5f,
            12f,
            16f + stormIntensity * 4f);

        ConfigureRainEmitter(
            stormParticles,
            stormIntensity,
            620f,
            0.18f,
            0.3f,
            5f,
            18f,
            26f);

        if (targetCamera != null && heatParticles != null)
        {
            float height = targetCamera.orthographicSize * 2f;
            float width = height * targetCamera.aspect;

            // Fill the whole view, and offset so the box is centred on the camera.
            heatParticles.transform.localPosition = Vector3.zero;
            ParticleSystem.ShapeModule heatShape = heatParticles.shape;
            heatShape.scale = new Vector3(Mathf.Max(24f, width), Mathf.Max(16f, height), 1f);
        }

        // Rate scales with screen area so density stays even across resolutions.
        float heatRate = 90f;
        if (targetCamera != null)
        {
            float height = targetCamera.orthographicSize * 2f;
            float width = height * targetCamera.aspect;
            heatRate = Mathf.Clamp(width * height * 0.35f, 60f, 400f);
        }

        SetParticleActive(heatParticles, heatIntensity, heatRate);
    }

    private void ConfigureRainEmitter(
        ParticleSystem ps,
        float intensity,
        float baseRate,
        float sizeMin,
        float sizeMax,
        float wind,
        float fallMin,
        float fallMax)
    {
        if (ps == null || targetCamera == null)
        {
            return;
        }

        ps.transform.localPosition = Vector3.zero;

        float height = targetCamera.orthographicSize * 2f;
        float width = height * targetCamera.aspect;
        float margin = 4f;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.scale = new Vector3(width + margin, height + margin, 1f);

        float avgFall = (fallMin + fallMax) * 0.5f;
        float lifetime = (height + margin) / Mathf.Max(1f, avgFall) * 1.2f;

        ParticleSystem.MainModule main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.95f, lifetime * 1.08f);
        main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);

        SetVelocityTwoConstants(ps.velocityOverLifetime, -wind, wind, -fallMax, -fallMin);
        SetParticleActive(ps, intensity, baseRate);
    }

    private static void SetParticleActive(ParticleSystem ps, float intensity, float fullRate)
    {
        if (ps == null)
        {
            return;
        }

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = fullRate * intensity;

        if (intensity > 0.02f)
        {
            if (!ps.isPlaying)
            {
                ps.Play();
            }
        }
        else if (ps.isPlaying)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    private void UpdateLightning()
    {
        lightningFlash = Mathf.MoveTowards(lightningFlash, 0f, Time.deltaTime * 20f);

        if (stormIntensity <= 0.35f)
        {
            return;
        }

        lightningCooldown -= Time.deltaTime;
        if (lightningCooldown > 0f)
        {
            return;
        }

        if (Random.value < Time.deltaTime * 0.045f)
        {
            lightningFlash = 1f;
            lightningCooldown = Random.Range(12f, 30f);
        }
    }

    private static Color AlphaBlend(Color bottom, Color top)
    {
        float alpha = top.a + bottom.a * (1f - top.a);
        if (alpha <= 0.0001f)
        {
            return new Color(0f, 0f, 0f, 0f);
        }

        float r = (top.r * top.a + bottom.r * bottom.a * (1f - top.a)) / alpha;
        float g = (top.g * top.a + bottom.g * bottom.a * (1f - top.a)) / alpha;
        float b = (top.b * top.a + bottom.b * bottom.a * (1f - top.a)) / alpha;
        return new Color(r, g, b, alpha);
    }

    private void OnDestroy()
    {
        if (particleMaterial != null)
        {
            Destroy(particleMaterial);
        }

        if (tintSprite != null)
        {
            if (tintSprite.texture != null)
            {
                Destroy(tintSprite.texture);
            }

            Destroy(tintSprite);
        }
    }
}
