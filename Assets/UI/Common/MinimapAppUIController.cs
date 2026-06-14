using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public sealed class MinimapAppUIController : MonoBehaviour
{
    [SerializeField] private float mapRadiusWorld = 260f;

    private UIDocument document;
    private VisualElement overlay;
    private VisualElement markersRoot;
    private VisualElement legendRoot;
    private VisualElement mapArea;
    private VisualElement mapWrap;

    private Transform player;
    private bool isOpen;
    private bool visualsInitialized;
    private readonly List<VisualElement> markerPool = new List<VisualElement>();

    private void OnEnable()
    {
        if (document == null)
        {
            document = GetComponent<UIDocument>();
        }

        TryInitVisuals();
        WorldObjectiveRegistry.OnChanged += HandleObjectivesChanged;
    }

    private void OnDisable()
    {
        WorldObjectiveRegistry.OnChanged -= HandleObjectivesChanged;
        SetOpen(false);
    }

    private void Update()
    {
        if (!visualsInitialized)
        {
            TryInitVisuals();
        }

        if (SceneManager.GetActiveScene().name != "Level")
        {
            if (isOpen)
            {
                SetOpen(false);
            }

            return;
        }

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.mKey.wasPressedThisFrame)
        {
            SetOpen(!isOpen);
        }

        if (isOpen && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            SetOpen(false);
        }

        if (isOpen)
        {
            RefreshMap();
        }
    }

    private void HandleObjectivesChanged()
    {
        if (isOpen)
        {
            RefreshMap();
        }
    }

    private void TryInitVisuals()
    {
        if (visualsInitialized)
        {
            return;
        }

        if (document == null)
        {
            document = GetComponent<UIDocument>();
        }

        VisualElement root = document != null ? document.rootVisualElement : null;
        if (root == null)
        {
            return;
        }

        visualsInitialized = true;
        overlay = root.Q<VisualElement>("minimap-overlay");
        markersRoot = root.Q<VisualElement>("minimap-markers");
        legendRoot = root.Q<VisualElement>("minimap-legend");
        mapWrap = root.Q<VisualElement>("minimap-map-wrap");
        mapArea = mapWrap;

        BuildLegend();
        SetOpen(false);
    }

    private void SetOpen(bool open)
    {
        if (!visualsInitialized)
        {
            TryInitVisuals();
        }

        isOpen = open;
        if (overlay == null)
        {
            return;
        }

        overlay.EnableInClassList("hidden", !open);
    }

    private void RefreshMap()
    {
        if (player == null || markersRoot == null || mapArea == null)
        {
            return;
        }

        float mapSize = mapWrap != null && mapWrap.resolvedStyle.width > 1f
            ? mapWrap.resolvedStyle.width
            : mapArea.resolvedStyle.width > 1f ? mapArea.resolvedStyle.width : 720f;
        float halfPlot = mapSize * 0.5f;
        Vector3 playerPosition = player.position;

        IReadOnlyList<WorldObjectiveInfo> objectives = WorldObjectiveRegistry.Entries;
        EnsureMarkerPool(objectives.Count);

        for (int i = 0; i < markerPool.Count; i++)
        {
            bool active = i < objectives.Count;
            markerPool[i].style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
            if (!active)
            {
                continue;
            }

            WorldObjectiveInfo objective = objectives[i];
            Vector2 offset = objective.Position - playerPosition;
            Vector2 normalized = offset / mapRadiusWorld;
            if (normalized.magnitude > 1f)
            {
                normalized = normalized.normalized;
            }

            float x = halfPlot + normalized.x * halfPlot;
            float y = halfPlot - normalized.y * halfPlot;
            markerPool[i].style.left = x;
            markerPool[i].style.top = y;
        }
    }

    private void EnsureMarkerPool(int requiredCount)
    {
        while (markerPool.Count < requiredCount)
        {
            var marker = new VisualElement();
            marker.AddToClassList("minimap-marker");
            marker.pickingMode = PickingMode.Ignore;
            markersRoot.Add(marker);
            markerPool.Add(marker);
        }

        IReadOnlyList<WorldObjectiveInfo> objectives = WorldObjectiveRegistry.Entries;
        for (int i = 0; i < markerPool.Count && i < objectives.Count; i++)
        {
            ApplyMarkerKind(markerPool[i], objectives[i].Kind);
        }
    }

    private static void ApplyMarkerKind(VisualElement marker, WorldObjectiveKind kind)
    {
        marker.RemoveFromClassList("minimap-marker--evacuation");
        marker.RemoveFromClassList("minimap-marker--portal");
        marker.RemoveFromClassList("minimap-marker--altar-fire");
        marker.RemoveFromClassList("minimap-marker--altar-water");
        marker.RemoveFromClassList("minimap-marker--other");

        switch (kind)
        {
            case WorldObjectiveKind.Evacuation:
                marker.AddToClassList("minimap-marker--evacuation");
                break;
            case WorldObjectiveKind.Portal:
                marker.AddToClassList("minimap-marker--portal");
                break;
            case WorldObjectiveKind.AltarFire:
                marker.AddToClassList("minimap-marker--altar-fire");
                break;
            case WorldObjectiveKind.AltarWater:
                marker.AddToClassList("minimap-marker--altar-water");
                break;
            default:
                marker.AddToClassList("minimap-marker--other");
                break;
        }
    }

    private void BuildLegend()
    {
        if (legendRoot == null)
        {
            return;
        }

        legendRoot.Clear();
        AddLegendItem("Эвакуация", "minimap-legend__dot--evacuation");
        AddLegendItem("Портал", "minimap-legend__dot--portal");
        AddLegendItem("Алтарь огня", "minimap-legend__dot--altar-fire");
        AddLegendItem("Алтарь воды", "minimap-legend__dot--altar-water");
        AddLegendItem("Вы", "minimap-legend__dot--player");
    }

    private void AddLegendItem(string label, string dotClass)
    {
        var item = new VisualElement();
        item.AddToClassList("minimap-legend__item");
        item.pickingMode = PickingMode.Ignore;

        var dot = new VisualElement();
        dot.AddToClassList("minimap-legend__dot");
        if (!string.IsNullOrEmpty(dotClass))
        {
            dot.AddToClassList(dotClass);
        }
        dot.pickingMode = PickingMode.Ignore;
        item.Add(dot);

        var text = new Label(label);
        text.AddToClassList("minimap-legend__label");
        text.pickingMode = PickingMode.Ignore;
        item.Add(text);

        legendRoot.Add(item);
    }
}
