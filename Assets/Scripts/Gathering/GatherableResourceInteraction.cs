using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[DisallowMultipleComponent]
public sealed class GatherableResourceInteraction : MonoBehaviour
{
    private static readonly List<GatherableResourceInteraction> ActiveResources = new List<GatherableResourceInteraction>();

    private readonly struct ResourceDefinition
    {
        public ResourceDefinition(string itemName, float gatherTime, float interactionDistance)
        {
            ItemName = itemName;
            GatherTime = gatherTime;
            InteractionDistance = interactionDistance;
        }

        public string ItemName { get; }
        public float GatherTime { get; }
        public float InteractionDistance { get; }
    }

    [SerializeField] private string itemName;
    [SerializeField] private float gatherTime = 3f;
    [SerializeField] private float interactionDistance = 4f;
    [SerializeField] private Color highlightColor = new Color(0.9f, 1f, 0.6f, 1f);

    private SpriteRenderer[] resourceRenderers;
    private Color[] defaultColors;
    private Collider2D interactionCollider;
    private Transform player;
    private ResourceGatherer gatherer;
    private Camera targetCamera;
    private bool isHovered;

    public string ItemName => itemName;
    public float GatherTime => gatherTime;

    public static bool TryAttachToInstance(GameObject instance)
    {
        if (instance == null || instance.GetComponent<GatherableResourceInteraction>() != null)
        {
            return false;
        }

        if (!TryResolveDefinition(instance.name, out ResourceDefinition definition))
        {
            return false;
        }

        GatherableResourceInteraction interaction = instance.AddComponent<GatherableResourceInteraction>();
        interaction.itemName = definition.ItemName;
        interaction.gatherTime = definition.GatherTime;
        interaction.interactionDistance = definition.InteractionDistance;
        return true;
    }

    public static int AttachToSceneObjects()
    {
        int attachedCount = 0;
        SpriteRenderer[] renderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && TryAttachToInstance(renderers[i].gameObject))
            {
                attachedCount++;
            }
        }

        return attachedCount;
    }

    private void Awake()
    {
        CacheReferences();
    }

    private void OnEnable()
    {
        if (!ActiveResources.Contains(this))
        {
            ActiveResources.Add(this);
        }
    }

    private void Update()
    {
        CacheReferences();

        if (gatherer != null && gatherer.IsBusy && !gatherer.IsGathering(this))
        {
            RestoreDefaultColor();
            return;
        }

        UpdateHoverState();
        HandleInteraction();
    }

    private void OnDisable()
    {
        ActiveResources.Remove(this);
        RestoreDefaultColor();
    }

    public bool CanBeginGathering(bool requirePointer)
    {
        if (string.IsNullOrEmpty(itemName) || !IsSelectableByPlayer())
        {
            return false;
        }

        return !requirePointer || IsPointerOverResource();
    }

    public bool CanContinueGathering(bool useMouseInput)
    {
        if (string.IsNullOrEmpty(itemName) || !IsSelectableByPlayer())
        {
            return false;
        }

        if (useMouseInput)
        {
            return Mouse.current != null && Mouse.current.leftButton.isPressed;
        }

        return Keyboard.current != null && Keyboard.current.eKey.isPressed;
    }

    public void NotifyGatherCompleted()
    {
        RestoreDefaultColor();
    }

    private void CacheReferences()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        if (gatherer == null)
        {
            gatherer = FindFirstObjectByType<ResourceGatherer>();
        }

        if (resourceRenderers == null || resourceRenderers.Length == 0)
        {
            resourceRenderers = GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
            defaultColors = new Color[resourceRenderers.Length];
            for (int i = 0; i < resourceRenderers.Length; i++)
            {
                defaultColors[i] = resourceRenderers[i] != null ? resourceRenderers[i].color : Color.white;
            }
        }

        if (interactionCollider == null)
        {
            interactionCollider = GetComponent<Collider2D>();
            if (interactionCollider == null)
            {
                interactionCollider = GetComponentInChildren<Collider2D>(includeInactive: true);
            }

            if (interactionCollider == null)
            {
                interactionCollider = CreateInteractionCollider();
            }
        }
    }

    private void UpdateHoverState()
    {
        bool shouldHighlight = IsSelectableByPlayer();
        if (shouldHighlight == isHovered)
        {
            return;
        }

        isHovered = shouldHighlight;
        ApplyColor(isHovered ? highlightColor : default);
    }

    private void HandleInteraction()
    {
        if (!isHovered || gatherer == null)
        {
            return;
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && IsPointerOverResource())
        {
            gatherer.TryStartGathering(this, useMouseInput: true);
            return;
        }

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            gatherer.TryStartGathering(this, useMouseInput: false);
        }
    }

    private bool IsPlayerInRange()
    {
        if (player == null)
        {
            return false;
        }

        return Vector2.Distance(transform.position, player.position) <= interactionDistance;
    }

    private bool IsSelectableByPlayer()
    {
        if (!IsPlayerInRange())
        {
            return false;
        }

        float myDistance = Vector2.Distance(transform.position, player.position);
        for (int i = 0; i < ActiveResources.Count; i++)
        {
            GatherableResourceInteraction other = ActiveResources[i];
            if (other == null || other == this || other.player == null || string.IsNullOrEmpty(other.itemName))
            {
                continue;
            }

            if (!other.IsPlayerInRange())
            {
                continue;
            }

            float otherDistance = Vector2.Distance(other.transform.position, other.player.position);
            if (otherDistance < myDistance)
            {
                return false;
            }

            if (Mathf.Approximately(otherDistance, myDistance) && other.GetInstanceID() < GetInstanceID())
            {
                return false;
            }
        }

        return true;
    }

    private bool IsPointerOverResource()
    {
        if (targetCamera == null || Mouse.current == null || interactionCollider == null)
        {
            return false;
        }

        Vector3 worldPoint = targetCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        return interactionCollider.OverlapPoint(new Vector2(worldPoint.x, worldPoint.y));
    }

    private Collider2D CreateInteractionCollider()
    {
        Bounds bounds;
        if (!TryGetRendererBounds(out bounds))
        {
            return null;
        }

        BoxCollider2D boxCollider = gameObject.AddComponent<BoxCollider2D>();
        boxCollider.isTrigger = true;

        Vector3 localCenter = transform.InverseTransformPoint(bounds.center);
        Vector3 localSize = transform.InverseTransformVector(bounds.size);
        boxCollider.offset = localCenter;
        boxCollider.size = new Vector2(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y));
        return boxCollider;
    }

    private bool TryGetRendererBounds(out Bounds bounds)
    {
        bounds = default;
        if (resourceRenderers == null || resourceRenderers.Length == 0)
        {
            return false;
        }

        bool hasBounds = false;
        for (int i = 0; i < resourceRenderers.Length; i++)
        {
            SpriteRenderer renderer = resourceRenderers[i];
            if (renderer == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    private void ApplyColor(Color color)
    {
        if (resourceRenderers == null)
        {
            return;
        }

        for (int i = 0; i < resourceRenderers.Length; i++)
        {
            if (resourceRenderers[i] == null)
            {
                continue;
            }

            resourceRenderers[i].color = color == default ? defaultColors[i] : color;
        }
    }

    private void RestoreDefaultColor()
    {
        isHovered = false;
        ApplyColor(default);
    }

    private static bool TryResolveDefinition(string objectName, out ResourceDefinition definition)
    {
        string loweredName = objectName.ToLowerInvariant();
        if (loweredName.Contains("сакур") || loweredName.Contains("sakura"))
        {
            definition = new ResourceDefinition(ItemCatalog.SakuraSapling, 3f, 4f);
            return true;
        }

        if (loweredName.Contains("яблон") || loweredName.Contains("apple"))
        {
            definition = new ResourceDefinition(ItemCatalog.AppleSapling, 3f, 4f);
            return true;
        }

        if (loweredName.Contains("дуб") || loweredName.Contains("oak"))
        {
            definition = new ResourceDefinition(ItemCatalog.OakSapling, 3f, 4f);
            return true;
        }

        if (loweredName.Contains("цвет") || loweredName.Contains("flower"))
        {
            definition = new ResourceDefinition(ItemCatalog.RareFlower, 2.5f, 4f);
            return true;
        }

        if (loweredName.Contains("жемч"))
        {
            definition = new ResourceDefinition(ItemCatalog.RareFlower, 2.5f, 4f);
            return true;
        }

        definition = default;
        return false;
    }
}
