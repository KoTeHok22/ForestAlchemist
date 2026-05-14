using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class GardenHarvestInteraction : MonoBehaviour
{
    [SerializeField] private float interactionDistance = 2f;
    [SerializeField] private Color highlightColor = new Color(0.4f, 1f, 0.4f, 1f);
    [SerializeField] private GardenHarvestTable harvestTable;

    private SpriteRenderer sr;
    private Collider2D col;
    private Color defaultColor;
    private bool isHovered;
    private bool hasHarvestedThisVisit;
    private Transform player;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        if (sr != null) defaultColor = sr.color;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    private void Update()
    {
        UpdateHoverState();
        HandleInteraction();
    }

    private void UpdateHoverState()
    {
        bool canHarvest = CanHarvest();
        bool shouldHighlight = canHarvest && IsPlayerInRange() && IsPointerOver();
        if (shouldHighlight == isHovered) return;

        isHovered = shouldHighlight;
        if (sr != null) sr.color = isHovered ? highlightColor : defaultColor;
    }

    private void HandleInteraction()
    {
        if (!isHovered || Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
        if (!CanHarvest()) return;

        Harvest();
    }

    private bool CanHarvest()
    {
        if (hasHarvestedThisVisit) return false;

        var progress = GameCore.Instance.CurrentProgress;
        if (progress == null) return false;

        return progress.garden.currentStage > 0;
    }

    private void Harvest()
    {
        hasHarvestedThisVisit = true;

        var progress = GameCore.Instance.CurrentProgress;
        if (progress == null) return;

        int stage = progress.garden.currentStage;
        var homeStorage = InventoryService.Instance.HomeStorage;
        if (homeStorage == null || harvestTable == null) return;

        GardenHarvestEntry[] drops = harvestTable.GetDropsForStage(stage);
        for (int i = 0; i < drops.Length; i++)
        {
            if (Random.value <= drops[i].chance)
            {
                int amount = Random.Range(drops[i].minAmount, drops[i].maxAmount + 1);
                if (amount > 0)
                {
                    homeStorage.AddItem(drops[i].itemName, amount);
                    Debug.Log($"[Garden] Собрано {amount}x {drops[i].itemName}");
                }
            }
        }

        if (sr != null) sr.color = defaultColor;
    }

    public void ResetHarvestState()
    {
        hasHarvestedThisVisit = false;
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
