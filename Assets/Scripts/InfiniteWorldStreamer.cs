using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class InfiniteWorldStreamer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform trackedTarget;
    [SerializeField] private WorldChunkTemplate worldTemplate;

    [Header("Streaming")]
    [SerializeField] private int horizontalRadiusInChunks = 2;
    [SerializeField] private int verticalRadiusInChunks = 2;
    [SerializeField] private bool hideTemplateChunk = false;
    [SerializeField] private int maxChunkActivationsPerFrame = 2;
    [SerializeField] private bool prewarmPoolOnStart = true;
    [SerializeField] private int initialPoolSize = 4;

    [Header("World Objects")]
    [SerializeField] private string objectTemplateRootName = "Objects";
    [SerializeField] private int objectSeed = 13579;
    [SerializeField] private int objectSpawnAttemptsPerChunk = 8;
    [SerializeField] private float objectSpawnChance = 0.65f;
    [SerializeField] private float objectPadding = 0.5f;
    [SerializeField] private bool applyDepthSortingToPlayer = true;

    [Header("Enemy Bases")]
    [SerializeField] private string enemyBaseTemplateRootName = "Enemy";
    [SerializeField] private int baseSeed = 24680;
    [SerializeField] private float baseSpawnChance = 0.15f;
    [SerializeField] private float minBaseDistance = 200f;

    private readonly Dictionary<Vector2Int, GameObject> activeChunks = new Dictionary<Vector2Int, GameObject>();
    private readonly Dictionary<GameObject, Vector2Int> chunkCoordinatesByObject = new Dictionary<GameObject, Vector2Int>();
    private readonly HashSet<Vector2Int> requiredCoordinates = new HashSet<Vector2Int>();
    private readonly HashSet<Vector2Int> queuedCoordinates = new HashSet<Vector2Int>();
    private readonly Queue<Vector2Int> spawnQueue = new Queue<Vector2Int>();
    private readonly Stack<GameObject> pooledChunks = new Stack<GameObject>();
    private readonly List<Vector2Int> coordinateBuffer = new List<Vector2Int>();
    private readonly List<Vector2Int> removalBuffer = new List<Vector2Int>();
    private readonly List<GameObject> prewarmedChunks = new List<GameObject>();
    private readonly List<GameObject> objectTemplates = new List<GameObject>();
    private readonly List<PlacedObjectInfo> placedObjects = new List<PlacedObjectInfo>();
    private readonly Dictionary<Vector2Int, GameObject> spawnedBases = new Dictionary<Vector2Int, GameObject>();
    private readonly List<Vector3> allBasePositions = new List<Vector3>();
    private Transform enemyBaseTemplateRoot;

    private static readonly List<BaseExclusionZone> baseExclusionZones = new List<BaseExclusionZone>();

    private struct BaseExclusionZone
    {
        public Vector3 Position;
        public float Radius;
    }

    public static void RegisterBasePosition(Vector3 position, float radius)
    {
        baseExclusionZones.Add(new BaseExclusionZone { Position = position, Radius = radius });
    }

    public static void UnregisterBasePosition(Vector3 position)
    {
        for (int i = baseExclusionZones.Count - 1; i >= 0; i--)
        {
            if (Vector3.Distance(baseExclusionZones[i].Position, position) < 0.1f)
            {
                baseExclusionZones.RemoveAt(i);
                return;
            }
        }
    }

    private static bool IsInsideBaseExclusionZone(Vector3 worldPosition)
    {
        for (int i = 0; i < baseExclusionZones.Count; i++)
        {
            float dist = Vector2.Distance(worldPosition, baseExclusionZones[i].Position);
            if (dist < baseExclusionZones[i].Radius)
            {
                return true;
            }
        }

        return false;
    }

    private Transform chunkContainer;
    private Transform objectTemplateRoot;
    private Vector2 chunkSize = Vector2.one;
    private Vector3 templateOrigin;
    private Vector2Int currentCenterCoordinate = new Vector2Int(int.MinValue, int.MinValue);

    private sealed class PlacedObjectInfo
    {
        public Bounds Bounds;
    }

    private void Awake()
    {
        if (IsGeneratedChunkClone())
        {
            enabled = false;
            return;
        }

        if (worldTemplate == null)
        {
            worldTemplate = GetComponent<WorldChunkTemplate>();
        }

        // Use seed from ExpeditionManager if available
        if (ExpeditionManager.Instance != null)
        {
            objectSeed = ExpeditionManager.Instance.CurrentSeed;
            baseSeed = ExpeditionManager.Instance.CurrentSeed + 100;
        }

        if (trackedTarget == null)
        {
            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                trackedTarget = player.transform;
            }
        }

        ValidateConfiguration();
        if (!enabled)
        {
            return;
        }

        chunkSize = worldTemplate.ChunkSize;
        templateOrigin = worldTemplate.transform.position;
        chunkContainer = EnsureChunkContainer();

        CacheObjectTemplates();
        CacheEnemyBaseTemplate();
        ConfigureDepthSortingForTrackedTarget();
        RegisterTemplateChunk();
        PrewarmPoolIfNeeded();
    }

    private void Start()
    {
        if (!enabled)
        {
            return;
        }

        QueueRequiredChunks(forceRefresh: true);
        ProcessSpawnQueue();
    }

    private void Update()
    {
        if (!enabled)
        {
            return;
        }

        if (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
        {
            RegenerateChunks();
        }

        QueueRequiredChunks(forceRefresh: false);
        ProcessSpawnQueue();
    }

    private void OnValidate()
    {
        horizontalRadiusInChunks = Mathf.Max(0, horizontalRadiusInChunks);
        verticalRadiusInChunks = Mathf.Max(0, verticalRadiusInChunks);
        maxChunkActivationsPerFrame = Mathf.Max(1, maxChunkActivationsPerFrame);
        initialPoolSize = Mathf.Max(0, initialPoolSize);
        objectSpawnAttemptsPerChunk = Mathf.Max(0, objectSpawnAttemptsPerChunk);
        objectSpawnChance = Mathf.Clamp01(objectSpawnChance);
        objectPadding = Mathf.Max(0f, objectPadding);

    }

    private void QueueRequiredChunks(bool forceRefresh)
    {
        if (trackedTarget == null || worldTemplate == null)
        {
            return;
        }

        Vector2Int targetCoordinate = GetChunkCoordinate(trackedTarget.position);
        if (!forceRefresh && targetCoordinate == currentCenterCoordinate)
        {
            return;
        }

        currentCenterCoordinate = targetCoordinate;

        BuildRequiredCoordinateSet(targetCoordinate);
        ReleaseFarChunksToPool();
        RebuildSpawnQueue();
        UpdateTemplateVisibility();
    }

    private void BuildRequiredCoordinateSet(Vector2Int centerCoordinate)
    {
        requiredCoordinates.Clear();
        coordinateBuffer.Clear();

        for (int y = -verticalRadiusInChunks; y <= verticalRadiusInChunks; y++)
        {
            for (int x = -horizontalRadiusInChunks; x <= horizontalRadiusInChunks; x++)
            {
                Vector2Int coordinate = new Vector2Int(centerCoordinate.x + x, centerCoordinate.y + y);
                requiredCoordinates.Add(coordinate);
                coordinateBuffer.Add(coordinate);
            }
        }

        coordinateBuffer.Sort((left, right) =>
        {
            int leftDistance = (left - centerCoordinate).sqrMagnitude;
            int rightDistance = (right - centerCoordinate).sqrMagnitude;
            return leftDistance.CompareTo(rightDistance);
        });
    }

    private void ReleaseFarChunksToPool()
    {
        removalBuffer.Clear();

        foreach (KeyValuePair<Vector2Int, GameObject> pair in activeChunks)
        {
            if (pair.Key == Vector2Int.zero)
            {
                continue;
            }

            if (requiredCoordinates.Contains(pair.Key))
            {
                continue;
            }

            removalBuffer.Add(pair.Key);
        }

        for (int i = 0; i < removalBuffer.Count; i++)
        {
            Vector2Int coordinate = removalBuffer[i];
            if (!activeChunks.TryGetValue(coordinate, out GameObject chunk) || chunk == null)
            {
                activeChunks.Remove(coordinate);
                continue;
            }

            activeChunks.Remove(coordinate);
            chunkCoordinatesByObject.Remove(chunk);
            PrepareChunkForPooling(chunk);
            pooledChunks.Push(chunk);
        }
    }

    private void RebuildSpawnQueue()
    {
        spawnQueue.Clear();
        queuedCoordinates.Clear();

        for (int i = 0; i < coordinateBuffer.Count; i++)
        {
            Vector2Int coordinate = coordinateBuffer[i];
            if (activeChunks.ContainsKey(coordinate))
            {
                continue;
            }

            if (queuedCoordinates.Add(coordinate))
            {
                spawnQueue.Enqueue(coordinate);
            }
        }
    }

    private void ProcessSpawnQueue()
    {
        int processedCount = 0;

        while (processedCount < maxChunkActivationsPerFrame && spawnQueue.Count > 0)
        {
            Vector2Int coordinate = spawnQueue.Dequeue();
            queuedCoordinates.Remove(coordinate);

            if (activeChunks.ContainsKey(coordinate) || !requiredCoordinates.Contains(coordinate))
            {
                continue;
            }

            if (coordinate == Vector2Int.zero)
            {
                activeChunks[coordinate] = worldTemplate.gameObject;
                chunkCoordinatesByObject[worldTemplate.gameObject] = coordinate;
                UpdateTemplateVisibility();
                processedCount++;
                continue;
            }

            GameObject chunk = AcquireChunkFromPool();
            ActivateChunk(chunk, coordinate);
            processedCount++;
        }
    }

    private void RegisterTemplateChunk()
    {
        activeChunks.Clear();
        chunkCoordinatesByObject.Clear();
        activeChunks[Vector2Int.zero] = worldTemplate.gameObject;
        chunkCoordinatesByObject[worldTemplate.gameObject] = Vector2Int.zero;
        worldTemplate.gameObject.name = "World";
    }

    private Transform EnsureChunkContainer()
    {
        string containerName = $"{name}_GeneratedChunks";
        Transform parentTransform = transform.parent;

        if (parentTransform != null)
        {
            Transform existingSiblingContainer = parentTransform.Find(containerName);
            if (existingSiblingContainer != null)
            {
                return existingSiblingContainer;
            }
        }
        else
        {
            GameObject existingRootContainer = GameObject.Find(containerName);
            if (existingRootContainer != null)
            {
                return existingRootContainer.transform;
            }
        }

        GameObject container = new GameObject(containerName);
        if (parentTransform != null)
        {
            container.transform.SetParent(parentTransform, false);
        }

        container.transform.position = templateOrigin;
        container.transform.rotation = worldTemplate.transform.rotation;
        container.transform.localScale = worldTemplate.transform.localScale;
        return container.transform;
    }

    private void CacheObjectTemplates()
    {
        objectTemplates.Clear();
        objectTemplateRoot = FindInactiveRootByName(objectTemplateRootName);
        if (objectTemplateRoot == null)
        {
            return;
        }

        for (int i = 0; i < objectTemplateRoot.childCount; i++)
        {
            Transform child = objectTemplateRoot.GetChild(i);
            if (child == null)
            {
                continue;
            }

            objectTemplates.Add(child.gameObject);
        }
    }



    private float Sample01WithSeed(Vector2Int coordinate, int attempt, int salt, int seed)
    {
        uint hash = ComputeHash((uint)coordinate.x, (uint)coordinate.y, (uint)attempt, (uint)salt, (uint)seed);
        return (hash & 0x00FFFFFF) / 16777215f;
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

    private void ConfigureDepthSortingForTrackedTarget()
    {
        if (!applyDepthSortingToPlayer || trackedTarget == null)
        {
            return;
        }

        DepthSortingConfigurator.ConfigureSingle(trackedTarget.gameObject);
    }

    private void PrewarmPoolIfNeeded()
    {
        if (!prewarmPoolOnStart || initialPoolSize <= 0)
        {
            return;
        }

        prewarmedChunks.Clear();

        int prewarmBudget = Mathf.Min(initialPoolSize, Mathf.Max(0, GetVisibleChunkCapacity() - 1));
        for (int i = 0; i < prewarmBudget; i++)
        {
            GameObject chunk = CreatePooledChunkInstance();
            prewarmedChunks.Add(chunk);
        }

        for (int i = 0; i < prewarmedChunks.Count; i++)
        {
            pooledChunks.Push(prewarmedChunks[i]);
        }

        prewarmedChunks.Clear();
    }

    private GameObject AcquireChunkFromPool()
    {
        while (pooledChunks.Count > 0)
        {
            GameObject pooledChunk = pooledChunks.Pop();
            if (pooledChunk != null)
            {
                return pooledChunk;
            }
        }

        return CreatePooledChunkInstance();
    }

    private GameObject CreatePooledChunkInstance()
    {
        GameObject chunk = Instantiate(worldTemplate.gameObject, templateOrigin, worldTemplate.transform.rotation, chunkContainer);
        chunk.name = "World_Pooled";
        EnsureChunkObjectsRoot(chunk.transform);
        ConfigureDepthSortingOnHierarchy(chunk);
        chunk.SetActive(false);
        return chunk;
    }

    private void ActivateChunk(GameObject chunk, Vector2Int coordinate)
    {
        chunk.transform.SetParent(chunkContainer, false);
        Vector3 chunkPosition = GetChunkPosition(coordinate);
        chunk.transform.SetPositionAndRotation(chunkPosition, worldTemplate.transform.rotation);
        chunk.transform.localScale = worldTemplate.transform.localScale;
        chunk.name = $"World_{coordinate.x}_{coordinate.y}";
        chunk.SetActive(true);

        PopulateChunkObjects(chunk.transform, coordinate);
        TrySpawnEnemyBase(coordinate, chunkPosition);

        activeChunks[coordinate] = chunk;
        chunkCoordinatesByObject[chunk] = coordinate;
    }

    private void PrepareChunkForPooling(GameObject chunk)
    {
        ClearChunkObjects(chunk.transform);

        if (chunkCoordinatesByObject.TryGetValue(chunk, out Vector2Int coord))
        {
            CleanupEnemyBase(coord);
        }

        chunk.SetActive(false);
        chunk.name = "World_Pooled";
        chunk.transform.SetParent(chunkContainer, false);
        chunk.transform.SetPositionAndRotation(templateOrigin, worldTemplate.transform.rotation);
        chunk.transform.localScale = worldTemplate.transform.localScale;
    }

    private void PopulateChunkObjects(Transform chunkTransform, Vector2Int coordinate)
    {
        ClearChunkObjects(chunkTransform);
        if (objectTemplates.Count == 0 || objectSpawnAttemptsPerChunk <= 0)
        {
            return;
        }

        Transform objectsRoot = EnsureChunkObjectsRoot(chunkTransform);
        placedObjects.Clear();

        for (int attempt = 0; attempt < objectSpawnAttemptsPerChunk; attempt++)
        {
            if (Sample01(coordinate, attempt, 0) > objectSpawnChance)
            {
                continue;
            }

            GameObject template = objectTemplates[GetDeterministicIndex(coordinate, attempt, 1, objectTemplates.Count)];
            if (template == null)
            {
                continue;
            }

            Vector3 localPosition = new Vector3(
                Mathf.Lerp(-chunkSize.x * 0.5f, chunkSize.x * 0.5f, Sample01(coordinate, attempt, 2)),
                Mathf.Lerp(-chunkSize.y * 0.5f, chunkSize.y * 0.5f, Sample01(coordinate, attempt, 3)),
                0f);

            Vector3 worldPosition = chunkTransform.position + localPosition;

            if (IsInsideBaseExclusionZone(worldPosition))
            {
                continue;
            }

            GameObject instance = Instantiate(template, worldPosition, template.transform.rotation, objectsRoot);
            instance.name = template.name;
            instance.transform.localScale = template.transform.localScale;
            instance.SetActive(true);

            Bounds instanceBounds = CalculateInstanceBounds(instance);
            if (instanceBounds.size != Vector3.zero && IsOverlappingPlacedObject(instanceBounds))
            {
                Destroy(instance);
                continue;
            }

            ConfigureDepthSortingOnHierarchy(instance);

            if (instanceBounds.size != Vector3.zero)
            {
                placedObjects.Add(new PlacedObjectInfo
                {
                    Bounds = ExpandBounds(instanceBounds, objectPadding)
                });
            }
        }
    }

    private void ClearChunkObjects(Transform chunkTransform)
    {
        Transform objectsRoot = chunkTransform.Find("ChunkObjects");
        if (objectsRoot == null)
        {
            return;
        }

        for (int i = objectsRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(objectsRoot.GetChild(i).gameObject);
        }
    }

    private Transform EnsureChunkObjectsRoot(Transform chunkTransform)
    {
        Transform existing = chunkTransform.Find("ChunkObjects");
        if (existing != null)
        {
            return existing;
        }

        GameObject objectsRoot = new GameObject("ChunkObjects");
        objectsRoot.transform.SetParent(chunkTransform, false);
        objectsRoot.transform.localPosition = Vector3.zero;
        objectsRoot.transform.localRotation = Quaternion.identity;
        objectsRoot.transform.localScale = Vector3.one;
        return objectsRoot.transform;
    }

    private void ConfigureDepthSortingOnHierarchy(GameObject rootObject)
    {
        DepthSortingConfigurator.ConfigureHierarchy(rootObject);
    }

    private bool IsOverlappingPlacedObject(Bounds candidateBounds)
    {
        Bounds expandedCandidate = ExpandBounds(candidateBounds, objectPadding);
        for (int i = 0; i < placedObjects.Count; i++)
        {
            if (placedObjects[i].Bounds.Intersects(expandedCandidate))
            {
                return true;
            }
        }

        return false;
    }

    private Bounds ExpandBounds(Bounds source, float padding)
    {
        if (padding <= 0f)
        {
            return source;
        }

        source.Expand(new Vector3(padding, padding, 0f));
        return source;
    }

    private Bounds CalculateInstanceBounds(GameObject instance)
    {
        Collider2D[] colliders = instance.GetComponentsInChildren<Collider2D>(includeInactive: true);
        bool hasBounds = false;
        Bounds combinedBounds = default;

        for (int i = 0; i < colliders.Length; i++)
        {
            if (!colliders[i].enabled)
            {
                continue;
            }

            if (!hasBounds)
            {
                combinedBounds = colliders[i].bounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(colliders[i].bounds);
            }
        }

        if (hasBounds)
        {
            return combinedBounds;
        }

        SpriteRenderer[] renderers = instance.GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (!hasBounds)
            {
                combinedBounds = renderers[i].bounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(renderers[i].bounds);
            }
        }

        return hasBounds ? combinedBounds : new Bounds(instance.transform.position, Vector3.zero);
    }

    private float Sample01(Vector2Int coordinate, int attempt, int salt)
    {
        uint hash = ComputeHash((uint)coordinate.x, (uint)coordinate.y, (uint)attempt, (uint)salt, (uint)objectSeed);
        return (hash & 0x00FFFFFF) / 16777215f;
    }

    private int GetDeterministicIndex(Vector2Int coordinate, int attempt, int salt, int maxExclusive)
    {
        if (maxExclusive <= 1)
        {
            return 0;
        }

        uint hash = ComputeHash((uint)coordinate.x, (uint)coordinate.y, (uint)attempt, (uint)salt, (uint)(objectSeed * 31 + 7));
        return (int)(hash % (uint)maxExclusive);
    }

    private uint ComputeHash(uint a, uint b, uint c, uint d, uint e)
    {
        uint hash = 2166136261u;
        hash = (hash ^ a) * 16777619u;
        hash = (hash ^ b) * 16777619u;
        hash = (hash ^ c) * 16777619u;
        hash = (hash ^ d) * 16777619u;
        hash = (hash ^ e) * 16777619u;
        return hash;
    }

    private void UpdateTemplateVisibility()
    {
        if (worldTemplate == null)
        {
            return;
        }

        bool templateIsRequired = requiredCoordinates.Contains(Vector2Int.zero);
        bool shouldShowTemplate = !hideTemplateChunk || templateIsRequired;
        if (worldTemplate.gameObject.activeSelf != shouldShowTemplate)
        {
            worldTemplate.gameObject.SetActive(shouldShowTemplate);
        }
    }

    private void RegenerateChunks()
    {
        removalBuffer.Clear();

        foreach (KeyValuePair<Vector2Int, GameObject> pair in activeChunks)
        {
            if (pair.Key == Vector2Int.zero)
            {
                continue;
            }

            removalBuffer.Add(pair.Key);
        }

        for (int i = 0; i < removalBuffer.Count; i++)
        {
            Vector2Int coordinate = removalBuffer[i];
            if (!activeChunks.TryGetValue(coordinate, out GameObject chunk) || chunk == null)
            {
                activeChunks.Remove(coordinate);
                continue;
            }

            activeChunks.Remove(coordinate);
            chunkCoordinatesByObject.Remove(chunk);
            PrepareChunkForPooling(chunk);
            pooledChunks.Push(chunk);
        }

        spawnQueue.Clear();
        queuedCoordinates.Clear();
        currentCenterCoordinate = new Vector2Int(int.MinValue, int.MinValue);

        QueueRequiredChunks(forceRefresh: true);
        ProcessSpawnQueue();
    }

    private int GetVisibleChunkCapacity()
    {
        return (horizontalRadiusInChunks * 2 + 1) * (verticalRadiusInChunks * 2 + 1);
    }

    private Vector2Int GetChunkCoordinate(Vector3 worldPosition)
    {
        if (chunkSize.x <= 0f || chunkSize.y <= 0f)
        {
            return Vector2Int.zero;
        }

        Vector3 offset = worldPosition - templateOrigin;
        int x = Mathf.RoundToInt(offset.x / chunkSize.x);
        int y = Mathf.RoundToInt(offset.y / chunkSize.y);
        return new Vector2Int(x, y);
    }

    private Vector3 GetChunkPosition(Vector2Int coordinate)
    {
        return templateOrigin + new Vector3(coordinate.x * chunkSize.x, coordinate.y * chunkSize.y, 0f);
    }

    private bool IsGeneratedChunkClone()
    {
        Transform current = transform.parent;
        while (current != null)
        {
            if (current.name.EndsWith("_GeneratedChunks"))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private void ValidateConfiguration()
    {
        if (worldTemplate == null)
        {
            enabled = false;
            return;
        }

        horizontalRadiusInChunks = Mathf.Max(0, horizontalRadiusInChunks);
        verticalRadiusInChunks = Mathf.Max(0, verticalRadiusInChunks);
        maxChunkActivationsPerFrame = Mathf.Max(1, maxChunkActivationsPerFrame);
    }

    private void CacheEnemyBaseTemplate()
    {
        enemyBaseTemplateRoot = FindInactiveRootByName(enemyBaseTemplateRootName);
        if (enemyBaseTemplateRoot != null)
        {
            enemyBaseTemplateRoot.gameObject.SetActive(false);
        }
    }

    private void TrySpawnEnemyBase(Vector2Int coordinate, Vector3 chunkCenter)
    {
        if (enemyBaseTemplateRoot == null)
        {
            return;
        }

        if (coordinate == Vector2Int.zero)
        {
            return;
        }

        if (spawnedBases.ContainsKey(coordinate))
        {
            return;
        }

        float roll = Sample01WithSeed(coordinate, 0, 99, baseSeed);
        if (roll > baseSpawnChance)
        {
            return;
        }

        float offsetX = Mathf.Lerp(-chunkSize.x * 0.3f, chunkSize.x * 0.3f, Sample01WithSeed(coordinate, 1, 99, baseSeed));
        float offsetY = Mathf.Lerp(-chunkSize.y * 0.3f, chunkSize.y * 0.3f, Sample01WithSeed(coordinate, 2, 99, baseSeed));
        Vector3 basePosition = chunkCenter + new Vector3(offsetX, offsetY, 0f);

        if (IsTooCloseToExistingBase(basePosition))
        {
            return;
        }

        GameObject baseInstance = Instantiate(enemyBaseTemplateRoot.gameObject, basePosition, Quaternion.identity);
        baseInstance.name = $"EnemyBase_{coordinate.x}_{coordinate.y}";
        baseInstance.SetActive(true);

        DepthSortingConfigurator.ConfigureHierarchy(baseInstance);

        spawnedBases[coordinate] = baseInstance;
        allBasePositions.Add(basePosition);
    }

    private void CleanupEnemyBase(Vector2Int coordinate)
    {
        if (!spawnedBases.TryGetValue(coordinate, out GameObject baseObj))
        {
            return;
        }

        if (baseObj != null)
        {
            Vector3 pos = baseObj.transform.position;
            allBasePositions.Remove(pos);
            Destroy(baseObj);
        }

        spawnedBases.Remove(coordinate);
    }

    private bool IsTooCloseToExistingBase(Vector3 position)
    {
        for (int i = 0; i < allBasePositions.Count; i++)
        {
            if (Vector2.Distance(position, allBasePositions[i]) < minBaseDistance)
            {
                return true;
            }
        }

        return false;
    }
}
