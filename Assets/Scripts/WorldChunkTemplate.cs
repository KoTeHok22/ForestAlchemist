using UnityEngine;

[DisallowMultipleComponent]
public sealed class WorldChunkTemplate : MonoBehaviour
{
    [Header("Chunk Size")]
    [SerializeField] private bool autoDetectFromChildren = true;
    [SerializeField] private Vector2 manualChunkSize = new Vector2(51.2f, 29.6f);

    public Vector2 ChunkSize => ResolveChunkSize();

    private void OnValidate()
    {
        manualChunkSize.x = Mathf.Max(0.01f, manualChunkSize.x);
        manualChunkSize.y = Mathf.Max(0.01f, manualChunkSize.y);
    }

    public Vector2 ResolveChunkSize()
    {
        if (!autoDetectFromChildren)
        {
            return manualChunkSize;
        }

        if (TryResolveBoundsSize(out Vector2 boundsSize))
        {
            return boundsSize;
        }

        if (TryResolveChildOffsetSize(out Vector2 offsetSize))
        {
            return offsetSize;
        }

        return manualChunkSize;
    }

    private bool TryResolveBoundsSize(out Vector2 size)
    {
        bool hasBounds = false;
        Bounds combinedBounds = default;

        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
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

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(includeInactive: true);
        for (int i = 0; i < colliders.Length; i++)
        {
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

        if (!hasBounds)
        {
            size = Vector2.zero;
            return false;
        }

        size = new Vector2(
            Mathf.Max(0.01f, combinedBounds.size.x),
            Mathf.Max(0.01f, combinedBounds.size.y));
        return true;
    }

    private bool TryResolveChildOffsetSize(out Vector2 size)
    {
        if (transform.childCount == 0)
        {
            size = Vector2.zero;
            return false;
        }

        Vector3 center = transform.position;
        float maxOffsetX = 0f;
        float maxOffsetY = 0f;

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            Vector3 offset = child.position - center;
            maxOffsetX = Mathf.Max(maxOffsetX, Mathf.Abs(offset.x));
            maxOffsetY = Mathf.Max(maxOffsetY, Mathf.Abs(offset.y));
        }

        if (maxOffsetX <= 0.0001f || maxOffsetY <= 0.0001f)
        {
            size = Vector2.zero;
            return false;
        }

        size = new Vector2(maxOffsetX * 2f, maxOffsetY * 2f);
        return true;
    }
}
