using UnityEngine;
using UnityEngine.InputSystem;

public sealed class EvacuationPoint : MonoBehaviour
{
    [SerializeField] private float interactionDistance = 2f;
    [SerializeField] private Color highlightColor = Color.green;

    private SpriteRenderer sr;
    private Collider2D col;
    private Color defaultColor;
    private bool isHovered;
    private Transform player;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
        }
        if (sr != null) defaultColor = sr.color;
        
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    private void Update()
    {
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
        if (isHovered && Mouse.current.leftButton.wasPressedThisFrame)
        {
            ExpeditionManager.Instance.TryUnlockReturn("evacuation_point");
            QuestManager.Instance.ReportReturnPointReached("evacuation_point");
            AudioHooks.Bridge?.PlayEvacuation();
            AudioHooks.Bridge?.PlayLocationReached();
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
        if (Camera.main == null) return false;
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector2 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
        return col.OverlapPoint(worldPos);
    }
}
