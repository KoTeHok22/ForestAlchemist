using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PortalObject : MonoBehaviour
{
    [SerializeField] private float interactionDistance = 2f;
    [SerializeField] private Color highlightColor = new Color(0.5f, 0.8f, 1f, 1f);
    [SerializeField] private float portalChance = 0.3f;

    private SpriteRenderer sr;
    private Collider2D col;
    private Color defaultColor;
    private bool isHovered;
    private Transform player;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            sr = gameObject.AddComponent<SpriteRenderer>();
        }

        col = GetComponent<Collider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
        }
        if (sr != null) defaultColor = sr.color;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        if (!gameObject.name.Contains("GuaranteedPortal") && Random.value > portalChance)
        {
            gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (player == null) return;
        UpdateHoverState();
        HandleInteraction();
    }

    private void UpdateHoverState()
    {
        bool shouldHighlight = IsPlayerInRange() && IsPointerOver();
        if (shouldHighlight == isHovered) return;

        isHovered = shouldHighlight;
        if (sr != null) sr.color = isHovered ? highlightColor : defaultColor;
    }

    private void HandleInteraction()
    {
        if (isHovered && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            ExpeditionManager.Instance.TryUnlockReturn("portal");
            AudioHooks.Bridge?.PlayPortalEnter();
            ExpeditionManager.Instance.EndExpedition(ExpeditionResult.Success);
        }
    }

    private bool IsPlayerInRange()
    {
        if (player == null) return false;
        return Vector2.Distance(transform.position, player.position) <= interactionDistance;
    }

    private bool IsPointerOver()
    {
        if (Camera.main == null || Mouse.current == null) return false;
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector2 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
        return col.OverlapPoint(worldPos);
    }
}
