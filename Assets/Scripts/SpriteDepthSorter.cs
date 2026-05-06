using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class SpriteDepthSorter : MonoBehaviour
{
    [Header("Sorting")]
    [SerializeField] private bool updateEveryFrame = true;
    [SerializeField] private int sortingOrderOffset = 16383;
    [SerializeField] private int sortingPrecision = 10;
    [SerializeField] private bool useColliderBottomAsPivot = true;
    [SerializeField] private float customPivotOffset = 0f;

    private SpriteRenderer spriteRenderer;
    private Collider2D cachedCollider;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        cachedCollider = GetComponent<Collider2D>();
        RefreshSortingOrder();
    }

    private void LateUpdate()
    {
        if (!updateEveryFrame)
        {
            return;
        }

        RefreshSortingOrder();
    }

    private void OnEnable()
    {
        RefreshSortingOrder();
    }

    public void RefreshSortingOrder()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                return;
            }
        }

        int computedOrder = sortingOrderOffset - Mathf.RoundToInt(GetSortingPivotY() * sortingPrecision);
        spriteRenderer.sortingOrder = Mathf.Clamp(computedOrder, 0, 32767);
    }

    private float GetSortingPivotY()
    {
        if (useColliderBottomAsPivot)
        {
            if (cachedCollider == null)
            {
                cachedCollider = GetComponent<Collider2D>();
            }

            if (cachedCollider != null)
            {
                return cachedCollider.bounds.min.y + customPivotOffset;
            }
        }

        return transform.position.y + customPivotOffset;
    }
}
