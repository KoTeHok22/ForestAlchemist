using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public sealed class ChestInteraction : MonoBehaviour
{
    [SerializeField] private GameObject chestPanel;
    [SerializeField] private HomeStorageUI homeStorageUI;
    [SerializeField] private float interactionDistance = 2f;
    [SerializeField] private Color highlightColor = Color.cyan;

    private SpriteRenderer sr;
    private Collider2D col;
    private Color defaultColor;
    private bool isHovered;
    private Transform player;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        if (sr != null) defaultColor = sr.color;

        if (homeStorageUI == null)
        {
            homeStorageUI = FindFirstObjectByType<HomeStorageUI>();
        }
        
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
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
        bool inRange = IsPlayerInRange();
        bool over = IsPointerOver();
        bool shouldHighlight = inRange && over;

        if (shouldHighlight != isHovered)
        {
            isHovered = shouldHighlight;
            if (sr != null) sr.color = isHovered ? highlightColor : defaultColor;
        }
    }

    private void HandleInteraction()
    {
        if (isHovered && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            OpenChest();
        }
    }

    private void OpenChest()
    {
        if (homeStorageUI == null)
        {
            homeStorageUI = FindFirstObjectByType<HomeStorageUI>();
        }

        if (homeStorageUI != null)
        {
            homeStorageUI.Open();
            return;
        }

        if (chestPanel != null) chestPanel.SetActive(true);
    }

    private bool IsPlayerInRange()
    {
        if (player == null) return false;
        return Vector2.Distance(transform.position, player.position) <= interactionDistance;
    }

    private bool IsPointerOver()
    {
        if (Camera.main == null) return false;
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector2 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
        return col.OverlapPoint(worldPos);
    }

    private void ClearHighlight()
    {
        if (!isHovered) return;
        isHovered = false;
        if (sr != null) sr.color = defaultColor;
    }
}
