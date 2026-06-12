using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Routes clicks to App UI elements. The App UI &lt;Panel&gt; intercepts picking at
/// its root, so per-element ClickEvent callbacks never fire for child buttons.
/// This router listens for PointerDownEvent on the panel root and hit-tests the
/// registered element rects itself, then invokes the matching callback.
///
/// Usage: var router = new AppUIClickRouter(root); router.Add(btn, OnClick);
/// </summary>
public sealed class AppUIClickRouter
{
    private readonly List<(VisualElement el, Action cb)> targets = new List<(VisualElement, Action)>();

    public AppUIClickRouter(VisualElement root)
    {
        if (root == null) return;
        root.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
    }

    public void Add(VisualElement element, Action callback)
    {
        if (element == null || callback == null) return;
        element.pickingMode = PickingMode.Position;
        targets.Add((element, callback));
    }

    public void Clear() => targets.Clear();

    /// <summary>
    /// Drops registrations whose element has been detached from the panel (e.g.
    /// list rows discarded on a ScrollView rebuild). Without this the targets list
    /// grows unbounded across refreshes and stale rows keep intercepting clicks.
    /// Call right after clearing/rebuilding a dynamic list, before re-adding rows.
    /// </summary>
    public void RemoveDead()
    {
        for (int i = targets.Count - 1; i >= 0; i--)
        {
            VisualElement el = targets[i].el;
            if (el == null || el.panel == null) targets.RemoveAt(i);
        }
    }

    private void OnPointerDown(PointerDownEvent evt)
    {
        Vector2 p = evt.position;

        // Pick respects ScrollView clipping and z-order — worldBound does not, so a
        // row scrolled out of the viewport (but still laid out over the tabs) would
        // otherwise swallow the click. Pick returns the real top-most element here.
        IPanel panel = (evt.currentTarget as VisualElement)?.panel
                       ?? (evt.target as VisualElement)?.panel;
        VisualElement picked = panel?.Pick(p);

        for (int i = targets.Count - 1; i >= 0; i--)
        {
            var (el, cb) = targets[i];
            if (el == null || el.panel == null) continue;

            bool hit = picked != null ? IsSelfOrAncestor(el, picked) : el.worldBound.Contains(p);
            if (!hit) continue;

            cb();
            evt.StopPropagation();
            return;
        }
    }

    private static bool IsSelfOrAncestor(VisualElement ancestor, VisualElement node)
    {
        for (var e = node; e != null; e = e.parent)
            if (e == ancestor) return true;
        return false;
    }

    private static bool IsVisible(VisualElement el)
    {
        if (el.panel == null) return false;
        for (var e = el; e != null; e = e.parent)
            if (e.resolvedStyle.display == DisplayStyle.None) return false;
        return true;
    }
}
