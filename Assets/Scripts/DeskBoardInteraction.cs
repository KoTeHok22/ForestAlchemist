using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public sealed class DeskBoardInteraction : MonoBehaviour
{
    [SerializeField] private DeskBoardAppUI boardUI;
    [SerializeField] private QuestBoardGenerator boardGenerator;
    [SerializeField] private Behaviour playerController;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float interactionDistance = 1f;
    [SerializeField] private Color highlightColor = new Color(1f, 0.95f, 0.45f, 1f);

    private SpriteRenderer boardRenderer;
    private Collider2D boardCollider;
    private Color defaultColor;
    private bool isHovered;
    private bool isOpen;

    private void Awake()
    {
        boardRenderer = GetComponent<SpriteRenderer>();
        boardCollider = GetComponent<Collider2D>();
        defaultColor = boardRenderer.color;

        if (targetCamera == null) targetCamera = Camera.main;
        if (playerController == null) playerController = FindFirstObjectByType<PlayerTopDownController>();
        if (boardUI == null) boardUI = GetComponent<DeskBoardAppUI>();
        if (boardUI == null) boardUI = gameObject.AddComponent<DeskBoardAppUI>();
        if (boardGenerator == null) boardGenerator = GetComponent<QuestBoardGenerator>();

        CloseDesk();
    }

    private void Update()
    {
        if (isOpen || HomeUIBlocker.IsBlocked)
        {
            RestoreDefaultColor();
            return;
        }

        UpdateHoverState();
        HandleClick();
    }

    private void OnDisable()
    {
        RestoreDefaultColor();
    }

    private void UpdateHoverState()
    {
        bool shouldHighlight = IsPlayerInRange() && IsPointerOverBoard();
        if (shouldHighlight == isHovered) return;
        isHovered = shouldHighlight;
        boardRenderer.color = isHovered ? highlightColor : defaultColor;
    }

    private void HandleClick()
    {
        if (!isHovered || Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
        OpenDesk();
    }

    private void OpenDesk()
    {
        if (!IsPlayerInRange()) return;
        if (boardUI == null) return;

        boardUI.Open(OnBoardClosed);
        if (!boardUI.IsOpen)
        {
            return;
        }

        // Build quest cards now that the UI tree exists.
        if (boardGenerator != null) boardGenerator.GenerateBoard();

        isOpen = true;
        RestoreDefaultColor();
    }

    private void CloseDesk()
    {
        if (boardUI != null) boardUI.Close();
        isOpen = false;
    }

    private void OnBoardClosed()
    {
        isOpen = false;
    }

    private bool IsPointerOverBoard()
    {
        if (targetCamera == null || Mouse.current == null) return false;
        Vector3 mousePosition = Mouse.current.position.ReadValue();
        Vector3 worldPoint = targetCamera.ScreenToWorldPoint(mousePosition);
        Vector2 point = new Vector2(worldPoint.x, worldPoint.y);
        return boardCollider.OverlapPoint(point);
    }

    private bool IsPlayerInRange()
    {
        if (playerController == null) return false;
        float distanceToPlayer = Vector2.Distance(transform.position, playerController.transform.position);
        return distanceToPlayer <= interactionDistance;
    }

    private void RestoreDefaultColor()
    {
        isHovered = false;
        if (boardRenderer != null) boardRenderer.color = defaultColor;
    }
}
