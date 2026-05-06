using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyBaseController : MonoBehaviour
{
    [Header("Enemy Configs")]
    [SerializeField] private EnemyConfig greenOrcConfig;
    [SerializeField] private EnemyConfig blueOrcConfig;
    [SerializeField] private EnemyConfig bossOrcConfig;

    [Header("Wave Settings")]
    [SerializeField] private int greenOrcCount = 5;
    [SerializeField] private int blueOrcCount = 3;
    [SerializeField] private int bossCount = 1;

    [Header("Spawn")]
    [SerializeField] private float spawnRadius = 8f;
    [SerializeField] private float spawnJitter = 1.5f;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform playerTarget;

    [Header("Templates (optional)")]
    [SerializeField] private GameObject greenTemplate;
    [SerializeField] private GameObject blueTemplate;
    [SerializeField] private GameObject bossTemplate;

    [Header("Base Zone")]
    [SerializeField] private float baseExclusionRadius = 30f;

    [Header("Statues")]
    [SerializeField] private int statueCount = 10;
    [SerializeField] private float statueRadius = 25f;
    [SerializeField] private string statueObjectName = "\u0421\u0442\u0430\u0442\u0443\u044F\u041F\u043E\u0431\u043E\u043B\u044C\u0448\u0435";

    private readonly List<EnemyController> aliveEnemies = new List<EnemyController>();
    private IPlayerScoreService scoreService;
    private int currentWave;
    private bool baseDefeated;

    private void Awake()
    {
        InfiniteWorldStreamer.RegisterBasePosition(transform.position, baseExclusionRadius);
    }

    private void Start()
    {
        HideTemplateChildren();
        ResolvePlayerTarget();
        ResolveScoreService();
        LoadConfigsIfNeeded();
        SpawnStatues();
        SpawnWave(0);
    }

    private void OnDestroy()
    {
        InfiniteWorldStreamer.UnregisterBasePosition(transform.position);
    }

    private void ResolvePlayerTarget()
    {
        if (playerTarget != null)
        {
            return;
        }

        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            playerTarget = player.transform;
        }
    }

    private void ResolveScoreService()
    {
        PlayerScoreProvider provider = Object.FindFirstObjectByType<PlayerScoreProvider>();
        if (provider != null)
        {
            scoreService = provider.ScoreService;
            return;
        }

        scoreService = new PersistentPlayerScoreService(new JsonPlayerScoreRepository());
    }

    private void LoadConfigsIfNeeded()
    {
        if (greenOrcConfig == null)
        {
            greenOrcConfig = Resources.Load<EnemyConfig>("Game/Orc/GreenOrc");
        }

        if (blueOrcConfig == null)
        {
            blueOrcConfig = Resources.Load<EnemyConfig>("Game/Orc/BlueOrc");
        }

        if (bossOrcConfig == null)
        {
            bossOrcConfig = Resources.Load<EnemyConfig>("Game/Orc/BossOrc");
        }
    }

    private void SpawnWave(int wave)
    {
        if (baseDefeated)
        {
            return;
        }

        currentWave = wave;

        switch (wave)
        {
            case 0:
                SpawnEnemies(greenOrcConfig, greenOrcCount);
                break;
            case 1:
                SpawnEnemies(blueOrcConfig, blueOrcCount);
                break;
            case 2:
                SpawnEnemies(bossOrcConfig, bossCount);
                break;
            default:
                baseDefeated = true;
                break;
        }
    }

    private void SpawnEnemies(EnemyConfig config, int count)
    {
        if (config == null)
        {
            Debug.LogWarning($"[EnemyBase] Missing config for wave {currentWave}, skipping spawn.");
            AdvanceWave();
            return;
        }

        Vector2 center = spawnPoint != null ? (Vector2)spawnPoint.position : (Vector2)transform.position;

        for (int i = 0; i < count; i++)
        {
            Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
            randomOffset += Random.insideUnitCircle * spawnJitter;
            Vector3 spawnPos = new Vector3(center.x + randomOffset.x, center.y + randomOffset.y, 0f);

            GameObject enemyGo = new GameObject(config.enemyName);
            enemyGo.transform.position = spawnPos;
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            enemyGo.layer = enemyLayer;
            Debug.Log($"[EnemyBase] Creating enemy '{config.enemyName}' at {spawnPos}, layer={enemyLayer}", this);

            EnemyController controller = enemyGo.AddComponent<EnemyController>();
            controller.Initialize(config, transform, playerTarget, scoreService);
            controller.OnEnemyDied += HandleEnemyDied;
            aliveEnemies.Add(controller);
        }
    }

    private void HandleEnemyDied(EnemyController enemy)
    {
        enemy.OnEnemyDied -= HandleEnemyDied;
        aliveEnemies.Remove(enemy);

        if (aliveEnemies.Count <= 0)
        {
            AdvanceWave();
        }
    }

    private void AdvanceWave()
    {
        SpawnWave(currentWave + 1);
    }

    private void SpawnStatues()
    {
        GameObject statueTemplate = FindStatueTemplate();
        if (statueTemplate == null)
        {
            Debug.LogWarning("[EnemyBase] Could not find statue template object.");
            return;
        }

        Vector2 basePos = transform.position;

        for (int i = 0; i < statueCount; i++)
        {
            float angle = (360f / statueCount) * i * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(
                basePos.x + Mathf.Cos(angle) * statueRadius,
                basePos.y + Mathf.Sin(angle) * statueRadius,
                0f);

            GameObject statue = Instantiate(statueTemplate, pos, Quaternion.identity, transform);
            statue.name = $"Statue_{i}";
            statue.transform.localScale = new Vector3(.5f, .5f, .5f);
            statue.SetActive(true);

            DepthSortingConfigurator.ConfigureSingle(statue);
        }
    }

    private GameObject FindStatueTemplate()
    {
        Transform objectsRoot = FindInactiveRootByName("Objects");
        if (objectsRoot == null)
        {
            return null;
        }

        for (int i = 0; i < objectsRoot.childCount; i++)
        {
            Transform child = objectsRoot.GetChild(i);
            if (child.name == statueObjectName)
            {
                return child.gameObject;
            }
        }

        return null;
    }

    private Transform FindInactiveRootByName(string rootName)
    {
        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < allTransforms.Length; i++)
        {
            Transform candidate = allTransforms[i];
            if (candidate == null)
            {
                continue;
            }

            if (candidate.parent != null)
            {
                continue;
            }

            if (candidate.name != rootName)
            {
                continue;
            }

            return candidate;
        }

        return null;
    }

    private void HideTemplateChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child == null)
            {
                continue;
            }

            string childName = child.name;
            if (childName == "SpawnPoint")
            {
                continue;
            }

            if (child.GetComponent<SpriteRenderer>() != null && child.GetComponent<EnemyController>() == null)
            {
                child.gameObject.SetActive(false);
            }
        }
    }
}
