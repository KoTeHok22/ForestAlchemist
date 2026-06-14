using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class HudCompassView
{
    private const float FieldOfViewDegrees = 140f;

    private readonly VisualElement trackRoot;
    private readonly VisualElement markersRoot;
    private readonly VisualElement northMarker;
    private readonly List<VisualElement> markerPool = new List<VisualElement>();

    public HudCompassView(VisualElement root)
    {
        trackRoot = root.Q<VisualElement>("compass-track");
        markersRoot = root.Q<VisualElement>("compass-markers");
        northMarker = root.Q<VisualElement>("compass-north");
    }

    public void Refresh(Vector3 playerPosition, Vector2 lookDirection)
    {
        if (markersRoot == null)
        {
            return;
        }

        float lookAngle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg;
        float markersWidth = markersRoot.resolvedStyle.width > 1f ? markersRoot.resolvedStyle.width : 480f;
        float trackWidth = trackRoot != null && trackRoot.resolvedStyle.width > 1f ? trackRoot.resolvedStyle.width : 520f;

        PlaceDirectionMarker(northMarker, playerPosition, lookAngle, trackWidth, Vector2.up, 11f);

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
            Vector2 worldDirection = objective.Position - playerPosition;
            PlaceDirectionMarker(markerPool[i], playerPosition, lookAngle, markersWidth, worldDirection, 10f);

            float distance = worldDirection.magnitude;
            Label distanceLabel = markerPool[i].Q<Label>(className: "compass-marker__distance");
            if (distanceLabel != null)
            {
                distanceLabel.text = $"{Mathf.RoundToInt(distance)}м";
            }
        }

        ApplyObjectiveStyles(objectives);
    }

    private void EnsureMarkerPool(int requiredCount)
    {
        while (markerPool.Count < requiredCount)
        {
            VisualElement marker = CreateMarkerElement();
            markersRoot.Add(marker);
            markerPool.Add(marker);
        }
    }

    private VisualElement CreateMarkerElement()
    {
        var marker = new VisualElement();
        marker.AddToClassList("compass-marker");
        marker.pickingMode = PickingMode.Ignore;

        var dot = new VisualElement();
        dot.AddToClassList("compass-marker__dot");
        dot.pickingMode = PickingMode.Ignore;
        marker.Add(dot);

        var distance = new Label();
        distance.AddToClassList("compass-marker__distance");
        distance.pickingMode = PickingMode.Ignore;
        marker.Add(distance);

        return marker;
    }

    private static void PlaceDirectionMarker(
        VisualElement marker,
        Vector3 playerPosition,
        float lookAngleDeg,
        float trackWidth,
        Vector2 worldDirection,
        float markerHalfWidth)
    {
        if (marker == null)
        {
            return;
        }

        if (worldDirection.sqrMagnitude < 0.01f)
        {
            marker.style.display = DisplayStyle.None;
            return;
        }

        float targetAngle = Mathf.Atan2(worldDirection.y, worldDirection.x) * Mathf.Rad2Deg;
        float relative = Mathf.DeltaAngle(lookAngleDeg, targetAngle);
        float halfFov = FieldOfViewDegrees * 0.5f;
        float clampedRelative = Mathf.Clamp(relative, -halfFov, halfFov);

        marker.style.display = DisplayStyle.Flex;
        float normalized = clampedRelative / halfFov;
        float x = (normalized * 0.5f + 0.5f) * trackWidth;
        marker.style.left = x - markerHalfWidth;
    }

    public void ApplyObjectiveStyles(IReadOnlyList<WorldObjectiveInfo> objectives)
    {
        for (int i = 0; i < markerPool.Count && i < objectives.Count; i++)
        {
            VisualElement dot = markerPool[i].Q<VisualElement>(className: "compass-marker__dot");
            if (dot == null)
            {
                continue;
            }

            dot.RemoveFromClassList("compass-marker__dot--evacuation");
            dot.RemoveFromClassList("compass-marker__dot--portal");
            dot.RemoveFromClassList("compass-marker__dot--altar-fire");
            dot.RemoveFromClassList("compass-marker__dot--altar-water");
            dot.RemoveFromClassList("compass-marker__dot--other");

            switch (objectives[i].Kind)
            {
                case WorldObjectiveKind.Evacuation:
                    dot.AddToClassList("compass-marker__dot--evacuation");
                    break;
                case WorldObjectiveKind.Portal:
                    dot.AddToClassList("compass-marker__dot--portal");
                    break;
                case WorldObjectiveKind.AltarFire:
                    dot.AddToClassList("compass-marker__dot--altar-fire");
                    break;
                case WorldObjectiveKind.AltarWater:
                    dot.AddToClassList("compass-marker__dot--altar-water");
                    break;
                default:
                    dot.AddToClassList("compass-marker__dot--other");
                    break;
            }
        }
    }
}
