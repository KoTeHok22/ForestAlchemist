using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public sealed class DeskBoardInteraction : MonoBehaviour
{
    [SerializeField] private GameObject deskPanel;
    [SerializeField] private Button closeButton;
    [SerializeField] private Behaviour playerController;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float interactionDistance = 1f;
    [SerializeField] private Color highlightColor = new Color(1f, 0.95f, 0.45f, 1f);

    private SpriteRenderer boardRenderer;
    private Collider2D boardCollider;
    private Color defaultColor;
    private bool isHovered;

    private void Awake()
    {
        boardRenderer = GetComponent<SpriteRenderer>();
        boardCollider = GetComponent<Collider2D>();
        defaultColor = boardRenderer.color;

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerTopDownController>();
        }

        if (closeButton == null && deskPanel != null)
        {
            closeButton = deskPanel.GetComponentInChildren<Button>(true);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseDesk);
            closeButton.onClick.AddListener(CloseDesk);
        }

        CloseDesk();
    }

    private void Update()
    {
        if (IsDeskOpen())
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
        SetPlayerControlEnabled(true);
    }

    private void UpdateHoverState()
    {
        bool shouldHighlight = IsPlayerInRange() && IsPointerOverBoard();
        if (shouldHighlight == isHovered)
        {
            return;
        }

        isHovered = shouldHighlight;
        boardRenderer.color = isHovered ? highlightColor : defaultColor;
    }

    private void HandleClick()
    {
        if (!isHovered || Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
        {
            return;
        }

        OpenDesk();
    }

    private void OpenDesk()
    {
        if (!IsPlayerInRange())
        {
            return;
        }

        if (deskPanel != null)
        {
            deskPanel.SetActive(true);
        }

        RestoreDefaultColor();
        SetPlayerControlEnabled(false);
    }

    private void CloseDesk()
    {
        if (deskPanel != null)
        {
            deskPanel.SetActive(false);
        }

        SetPlayerControlEnabled(true);
    }

    private bool IsDeskOpen()
    {
        return deskPanel != null && deskPanel.activeSelf;
    }

    private bool IsPointerOverBoard()
    {
        if (targetCamera == null || Mouse.current == null)
        {
            return false;
        }

        Vector3 mousePosition = Mouse.current.position.ReadValue();
        Vector3 worldPoint = targetCamera.ScreenToWorldPoint(mousePosition);
        Vector2 point = new Vector2(worldPoint.x, worldPoint.y);
        return boardCollider.OverlapPoint(point);
    }

    private bool IsPlayerInRange()
    {
        if (playerController == null)
        {
            return false;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, playerController.transform.position);
        return distanceToPlayer <= interactionDistance;
    }

    private void SetPlayerControlEnabled(bool isEnabled)
    {
        if (playerController != null)
        {
            playerController.enabled = isEnabled;
        }
    }

    private void RestoreDefaultColor()
    {
        isHovered = false;

        if (boardRenderer != null)
        {
            boardRenderer.color = defaultColor;
        }
    }
}
