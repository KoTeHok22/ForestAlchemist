using UnityEngine;
using UnityEngine.InputSystem;

public sealed class AltarInteraction : MonoBehaviour
{
    [SerializeField] private float interactionDistance = 2f;
    [SerializeField] private Color highlightColor = new Color(1f, 0.6f, 0.9f, 1f);
    [SerializeField] private ElementType altarElement = ElementType.Fire;

    private SpriteRenderer sr;
    private Collider2D col;
    private Color defaultColor;
    private bool isHovered;
    private bool isActivated;
    private Transform player;
    private string altarId;

    public string AltarId => altarId;
    public ElementType AltarElement => altarElement;
    public bool IsActivated => isActivated;

    public static event System.Action<AltarInteraction> OnAltarActivated;

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
        RebuildIdentity();
        if (sr != null) defaultColor = sr.color;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    private void Update()
    {
        if (player == null || isActivated) return;
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
            Activate();
        }
    }

    private void Activate()
    {
        if (isActivated) return;

        isActivated = true;
        if (sr != null) sr.color = Color.white;

        OnAltarActivated?.Invoke(this);
        QuestManager.Instance.ReportAltarActivated(AltarId, altarElement);
    }

    public void ConfigureRuntimeElement(ElementType element)
    {
        altarElement = element;
        RebuildIdentity();
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

    private void RebuildIdentity()
    {
        altarId = $"altar_{altarElement}";
    }
}
