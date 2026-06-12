using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Click interaction that opens the App UI crafting board. Mirrors the Desk
/// board pattern: highlight on hover when the player is in range, open on left
/// click, disable player movement while open. Lives on the House sprite so the
/// whole house becomes the crafting interaction target.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public sealed class CraftingStationInteraction : MonoBehaviour
{
    [SerializeField] private CraftingUI craftingUI;
    [SerializeField] private Behaviour playerController;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float interactionDistance = 2f;
    [SerializeField] private Color highlightColor = new Color(0.45f, 1f, 0.85f, 1f);

    private SpriteRenderer spriteRenderer;
    private Collider2D triggerCollider;
    private Color defaultColor = Color.white;
    private Transform player;
    private bool isHovered;
    private bool isOpen;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        triggerCollider = GetComponent<Collider2D>();
        if (spriteRenderer != null) defaultColor = spriteRenderer.color;

        if (targetCamera == null) targetCamera = Camera.main;
        if (playerController == null) playerController = FindFirstObjectByType<PlayerTopDownController>();

        if (craftingUI == null) craftingUI = GetComponent<CraftingUI>();
        if (craftingUI == null) craftingUI = FindFirstObjectByType<CraftingUI>();
        if (craftingUI == null) craftingUI = gameObject.AddComponent<CraftingUI>();

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null) player = playerObject.transform;
    }

    private void Update()
    {
        if (isOpen)
        {
            RestoreDefaultColor();
            return;
        }

        UpdateHoverState();
        HandleInteraction();
    }

    private void OnDisable()
    {
        RestoreDefaultColor();
        SetPlayerControlEnabled(true);
    }

    private void UpdateHoverState()
    {
        bool shouldHighlight = IsPlayerInRange() && IsPointerOver();
        if (shouldHighlight == isHovered) return;

        isHovered = shouldHighlight;
        if (spriteRenderer != null)
            spriteRenderer.color = isHovered ? highlightColor : defaultColor;
    }

    private void HandleInteraction()
    {
        if (!isHovered || Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
        OpenCraft();
    }

    private void OpenCraft()
    {
        if (craftingUI == null) return;
        craftingUI.Open(OnPanelClosed);
        isOpen = true;
        RestoreDefaultColor();
        SetPlayerControlEnabled(false);
    }

    private void OnPanelClosed()
    {
        isOpen = false;
        SetPlayerControlEnabled(true);
    }

    private bool IsPlayerInRange()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null) player = playerObject.transform;
        }
        return player != null && Vector2.Distance(transform.position, player.position) <= interactionDistance;
    }

    private bool IsPointerOver()
    {
        if (targetCamera == null || Mouse.current == null || triggerCollider == null) return false;
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector2 worldPos = targetCamera.ScreenToWorldPoint(mousePos);
        return triggerCollider.OverlapPoint(worldPos);
    }

    private void SetPlayerControlEnabled(bool enabledState)
    {
        if (playerController != null) playerController.enabled = enabledState;
    }

    private void RestoreDefaultColor()
    {
        isHovered = false;
        if (spriteRenderer != null) spriteRenderer.color = defaultColor;
    }
}
