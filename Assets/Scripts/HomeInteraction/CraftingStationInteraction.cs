using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public sealed class CraftingStationInteraction : MonoBehaviour
{
    [SerializeField] private CraftingUI craftingUI;
    [SerializeField] private float interactionDistance = 2f;
    [SerializeField] private Color highlightColor = new Color(0.45f, 1f, 0.85f, 1f);

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

        if (craftingUI == null)
        {
            craftingUI = FindFirstObjectByType<CraftingUI>();
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    private void Update()
    {
        UpdateHoverState();
        HandleInteraction();
    }

    private void UpdateHoverState()
    {
        bool shouldHighlight = IsPlayerInRange() && IsPointerOver();
        if (shouldHighlight == isHovered)
        {
            return;
        }

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

        craftingUI?.Open();
    }

    private bool IsPlayerInRange()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        return player != null && Vector2.Distance(transform.position, player.position) <= interactionDistance;
    }

    private bool IsPointerOver()
    {
        if (Camera.main == null || Mouse.current == null || triggerCollider == null)
        {
            return false;
        }

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector2 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
        return triggerCollider.OverlapPoint(worldPos);
    }
}
