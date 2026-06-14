using UnityEngine;

/// <summary>
/// Runtime-built spell VFX (ParticleSystem + TrailRenderer). No prefab assets required —
/// used by fallback spells and as a default when SpellDefinition.projectilePrefab is null.
/// </summary>
public static class SpellVfxLibrary
{
    private const int VfxSortingOrder = 80;
    private const string VfxSortingLayer = "Default";
    private static Material particleMaterial;

    public static void PlayCastBurst(Vector3 position, ElementType element, Color tint)
    {
        GameObject host = new GameObject("SpellCastVfx");
        host.transform.position = position;

        ParticleSystem ps = CreateParticleSystem(host.transform, $"Cast_{element}");
        ConfigureBurst(ps, element, tint, 14, 0.35f, 1.8f, 0.08f, 0.22f);
        ps.Play();

        SpellVfxLifetime lifetime = host.AddComponent<SpellVfxLifetime>();
        lifetime.Configure(0.6f);
    }

    public static void PlayImpact(Vector3 position, ElementType element, Color tint, float scale)
    {
        GameObject host = new GameObject("SpellImpactVfx");
        host.transform.position = position;

        ParticleSystem ps = CreateParticleSystem(host.transform, $"Impact_{element}");
        int count = element == ElementType.Fire ? 22 : 16;
        float size = 0.1f * Mathf.Max(0.75f, scale);
        ConfigureBurst(ps, element, tint, count, 0.45f, 2.4f * scale, size, size * 1.6f);
        ps.Play();

        SpellVfxLifetime lifetime = host.AddComponent<SpellVfxLifetime>();
        lifetime.Configure(0.75f);
    }

    public static void PlayHealAura(Transform target, float duration, Color tint)
    {
        if (target == null) return;

        GameObject host = new GameObject("SpellHealAura");
        host.transform.SetParent(target, false);
        host.transform.localPosition = Vector3.zero;

        ParticleSystem ps = CreateParticleSystem(host.transform, "HealAura");
        var main = ps.main;
        main.duration = duration;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.1f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.12f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(tint.r, tint.g, tint.b, 0.85f),
            new Color(1f, 1f, 1f, 0.4f));
        main.maxParticles = 48;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.gravityModifier = -0.15f;

        var emission = ps.emission;
        emission.rateOverTime = 28f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.35f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(tint, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.9f, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradient;

        ps.Play();

        SpellVfxLifetime lifetime = host.AddComponent<SpellVfxLifetime>();
        lifetime.Configure(duration + 0.15f);
    }

    public static void PlayShieldAura(Transform target, float duration, Color tint)
    {
        if (target == null) return;

        GameObject host = new GameObject("SpellShieldAura");
        host.transform.position = target.position;

        ParticleSystem ps = CreateParticleSystem(host.transform, "ShieldAura");
        var main = ps.main;
        main.duration = duration;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.65f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.14f);
        main.startColor = tint;
        main.maxParticles = 64;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 32f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.42f;

        var velocityOverLifetime = ps.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        velocityOverLifetime.orbitalZ = 1.2f;

        ps.Play();

        SpellVfxShieldAura aura = host.AddComponent<SpellVfxShieldAura>();
        aura.Configure(target, duration);
    }

    public static void PlayDash(Vector3 start, Vector2 direction, float distance, float duration, Color tint)
    {
        GameObject host = new GameObject("SpellDashVfx");
        host.transform.position = start;

        ParticleSystem ps = CreateParticleSystem(host.transform, "DashBurst");
        ConfigureBurst(ps, ElementType.Air, tint, 18, 0.25f, distance * 1.5f, 0.07f, 0.16f);

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 18f;
        shape.rotation = new Vector3(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);

        ps.Play();

        // Streak along dash path.
        GameObject streakHost = new GameObject("DashStreak");
        streakHost.transform.SetParent(host.transform, false);
        ParticleSystem streak = CreateParticleSystem(streakHost.transform, "DashStreakPs");
        var streakMain = streak.main;
        streakMain.duration = duration;
        streakMain.loop = false;
        streakMain.startLifetime = duration * 0.9f;
        streakMain.startSpeed = 0f;
        streakMain.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.22f);
        streakMain.startColor = new Color(tint.r, tint.g, tint.b, 0.55f);
        streakMain.maxParticles = 12;
        streakMain.simulationSpace = ParticleSystemSimulationSpace.World;

        var streakEmission = streak.emission;
        streakEmission.rateOverTime = 0f;
        streakEmission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8) });

        var streakShape = streak.shape;
        streakShape.shapeType = ParticleSystemShapeType.Box;
        streakShape.scale = new Vector3(0.15f, distance * 0.5f, 0f);
        streakHost.transform.position = start + (Vector3)(direction.normalized * distance * 0.5f);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        streakHost.transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
        streak.Play();

        SpellVfxLifetime lifetime = host.AddComponent<SpellVfxLifetime>();
        lifetime.Configure(Mathf.Max(duration, 0.35f));
    }

    public static void BuildProjectileVisual(Transform projectile, ElementType element, Color tint, SpellDefinition spell)
    {
        if (projectile == null) return;

        float intensity = GetVfxIntensity(spell);
        bool isFire = element == ElementType.Fire;
        Color hot = isFire
            ? new Color(1f, 0.95f, 0.55f, 1f)
            : Color.Lerp(Color.white, tint, 0.35f);
        Color mid = isFire ? new Color(1f, 0.55f, 0.15f, 1f) : tint;

        ParticleSystem core = CreateParticleSystem(projectile, "ProjectileCore", stretch: isFire);
        var coreMain = core.main;
        coreMain.duration = 5f;
        coreMain.loop = true;
        coreMain.startLifetime = new ParticleSystem.MinMaxCurve(0.16f, 0.28f);
        coreMain.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.35f);
        coreMain.startSize = new ParticleSystem.MinMaxCurve(0.28f * intensity, 0.48f * intensity);
        coreMain.startColor = new ParticleSystem.MinMaxGradient(hot, mid);
        coreMain.maxParticles = 48;
        coreMain.simulationSpace = ParticleSystemSimulationSpace.Local;

        var coreEmission = core.emission;
        coreEmission.rateOverTime = 110f * intensity;

        var coreShape = core.shape;
        coreShape.shapeType = ParticleSystemShapeType.Circle;
        coreShape.radius = 0.08f;

        var coreColor = core.colorOverLifetime;
        coreColor.enabled = true;
        coreColor.color = BuildFadeGradient(hot, new Color(mid.r, mid.g, mid.b, 0f));

        ParticleSystem trail = CreateParticleSystem(projectile, "ProjectileTrail");
        var trailMain = trail.main;
        trailMain.duration = 5f;
        trailMain.loop = true;
        trailMain.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, 0.42f);
        trailMain.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.8f);
        trailMain.startSize = new ParticleSystem.MinMaxCurve(0.1f * intensity, 0.2f * intensity);
        trailMain.startColor = new ParticleSystem.MinMaxGradient(
            new Color(mid.r, mid.g, mid.b, 0.95f),
            new Color(tint.r, tint.g, tint.b, 0.35f));
        trailMain.maxParticles = 96;
        trailMain.simulationSpace = ParticleSystemSimulationSpace.World;

        var trailEmission = trail.emission;
        trailEmission.rateOverTime = (isFire ? 90f : 55f) * intensity;

        var trailShape = trail.shape;
        trailShape.shapeType = ParticleSystemShapeType.Circle;
        trailShape.radius = 0.06f;

        var trailColor = trail.colorOverLifetime;
        trailColor.enabled = true;
        trailColor.color = BuildFadeGradient(mid, new Color(tint.r, tint.g, tint.b, 0f));

        if (isFire)
        {
            ParticleSystem embers = CreateParticleSystem(projectile, "ProjectileEmbers");
            var emberMain = embers.main;
            emberMain.duration = 5f;
            emberMain.loop = true;
            emberMain.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
            emberMain.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.4f);
            emberMain.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.12f);
            emberMain.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.8f, 0.2f, 1f),
                new Color(1f, 0.35f, 0.05f, 0.8f));
            emberMain.maxParticles = 64;
            emberMain.simulationSpace = ParticleSystemSimulationSpace.World;
            emberMain.gravityModifier = 0.15f;

            var emberEmission = embers.emission;
            emberEmission.rateOverTime = 45f * intensity;

            var emberShape = embers.shape;
            emberShape.shapeType = ParticleSystemShapeType.Circle;
            emberShape.radius = 0.1f;

            embers.Play();
        }

        core.Play();
        trail.Play();
    }

    public static Sprite ResolveSpellIcon(SpellDefinition spell)
    {
        if (spell == null) return null;
        if (spell.icon != null) return spell.icon;

        if (!string.IsNullOrEmpty(spell.spellId))
        {
            Sprite loaded = Resources.Load<Sprite>($"Game/Icons/{spell.spellId}");
            if (loaded != null)
            {
                spell.icon = loaded;
                return loaded;
            }
        }

        return null;
    }

    public static float GetProjectileHitRadius(SpellDefinition spell)
    {
        if (spell == null) return 0.4f;
        if (spell.spellId == "spell_warchief_wrath") return 0.52f;
        if (spell.spellId == "spell_infernobolt") return 0.46f;
        return 0.4f;
    }

    public static float GetVfxIntensity(SpellDefinition spell)
    {
        if (spell == null) return 1f;
        if (spell.spellId == "spell_warchief_wrath") return 1.45f;
        if (spell.spellId == "spell_infernobolt") return 1.2f;
        return 1f;
    }

    private static Gradient BuildFadeGradient(Color start, Color end)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(start, 0f), new GradientColorKey(end, 1f) },
            new[] { new GradientAlphaKey(start.a, 0f), new GradientAlphaKey(end.a, 1f) });
        return gradient;
    }

    private static void ConfigureBurst(
        ParticleSystem ps,
        ElementType element,
        Color tint,
        int burstCount,
        float lifetime,
        float speed,
        float minSize,
        float maxSize)
    {
        var main = ps.main;
        main.duration = lifetime + 0.1f;
        main.loop = false;
        main.startLifetime = lifetime;
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.6f, speed);
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startColor = tint;
        main.maxParticles = burstCount + 8;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = element == ElementType.Earth ? 0.4f : element == ElementType.Water ? -0.2f : 0f;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)burstCount) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = element == ElementType.Air ? 0.18f : 0.12f;
    }

    private static ParticleSystem CreateParticleSystem(Transform parent, string name, bool stretch = false)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = GetParticleMaterial();
        renderer.sortingLayerName = VfxSortingLayer;
        renderer.sortingOrder = VfxSortingOrder;
        renderer.renderMode = stretch ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
        if (stretch)
        {
            renderer.lengthScale = 2.5f;
            renderer.velocityScale = 0.35f;
        }

        return ps;
    }

    private static Material GetParticleMaterial()
    {
        if (particleMaterial != null) return particleMaterial;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");

        particleMaterial = new Material(shader);
        particleMaterial.color = Color.white;
        return particleMaterial;
    }
}
