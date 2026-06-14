using System;
using System.Collections.Generic;
using UnityEngine;

public readonly struct WorldObjectiveInfo
{
    public WorldObjectiveInfo(Vector3 position, string label, WorldObjectiveKind kind)
    {
        Position = position;
        Label = label;
        Kind = kind;
    }

    public Vector3 Position { get; }
    public string Label { get; }
    public WorldObjectiveKind Kind { get; }
}

public static class WorldObjectiveRegistry
{
    private static readonly List<WorldObjectiveInfo> entries = new List<WorldObjectiveInfo>();

    public static event Action OnChanged;

    public static IReadOnlyList<WorldObjectiveInfo> Entries => entries;

    public static void Clear()
    {
        if (entries.Count == 0)
        {
            return;
        }

        entries.Clear();
        OnChanged?.Invoke();
    }

    public static void Register(Vector3 position, string label, WorldObjectiveKind kind)
    {
        entries.Add(new WorldObjectiveInfo(position, label, kind));
        OnChanged?.Invoke();
    }
}
