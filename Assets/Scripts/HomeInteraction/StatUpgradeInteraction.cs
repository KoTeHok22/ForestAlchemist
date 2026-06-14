using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public sealed class StatUpgradeInteraction : MonoBehaviour
{
    [SerializeField] private StatUpgradeUI statUpgradeUI;
    [SerializeField] private float interactionDistance = 2.4f;
    [SerializeField] private Color highlightColor = new Color(0.75f, 1f, 0.65f, 1f);

    private SpriteRenderer spriteRenderer;
    private Collider2D triggerCollider;
    private Color defaultColor = Color.white;
    private Transform player;
    private bool isHovered;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        triggerCollider = GetComponent<Collider2D>();
        if (spriteRenderer != null)
        {
            defaultColor = spriteRenderer.color;
        }

        if (statUpgradeUI == null)
        {
            statUpgradeUI = FindFirstObjectByType<StatUpgradeUI>();
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    private void Update()
    {
        if (HomeUIBlocker.IsBlocked)
        {
            ClearHighlight();
            return;
        }

        UpdateHoverState();
        HandleInteraction();
    }

    private void UpdateHoverState()
    {
        bool shouldHighlight = IsPlayerInRange() && IsPointerOver();
        if (shouldHighlight == isHovered) return;

        isHovered = shouldHighlight;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = isHovered ? highlightColor : defaultColor;
        }
    }

    private void HandleInteraction()
    {
        if (!isHovered || Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
        {
            return;
        }

        statUpgradeUI?.Open();
    }

    private bool IsPlayerInRange()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null) player = playerObject.transform;
        }

        return player != null &&
               Vector2.Distance(transform.position, player.position) <= interactionDistance;
    }

    private bool IsPointerOver()
    {
        if (Camera.main == null || Mouse.current == null || triggerCollider == null) return false;

        Vector2 worldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        return triggerCollider.OverlapPoint(worldPos);
    }

    private void ClearHighlight()
    {
        if (!isHovered) return;
        isHovered = false;
        if (spriteRenderer != null) spriteRenderer.color = defaultColor;
    }
}
