using UnityEngine;
using UnityEngine.InputSystem;

public sealed class ResourceGatherer : MonoBehaviour
{
    [SerializeField] private GatherProgressDisplay progressDisplay;
    [SerializeField] private float maxInteractionDistance = 2f;
    [SerializeField] private TreeGatherConfig[] treeConfigs;

    private PlayerInventory inventory;
    private Camera cachedCamera;

    private bool isGathering;
    private float gatherTimer;
    private float gatherDuration;
    private string gatherItemName;
    private Collider2D currentTarget;

    public void Initialize(PlayerInventory inventory)
    {
        this.inventory = inventory;
    }

    private void Update()
    {
        if (cachedCamera == null)
            cachedCamera = Camera.main;

        if (Mouse.current == null)
            return;

        bool holdingLMB = Mouse.current.leftButton.isPressed;

        if (isGathering)
        {
            if (!holdingLMB || !IsTargetStillValid())
            {
                CancelGathering();
                return;
            }

            gatherTimer += Time.deltaTime;
            progressDisplay.SetProgress(gatherTimer / gatherDuration);

            if (gatherTimer >= gatherDuration)
            {
                CompleteGathering();
            }
            return;
        }

        if (holdingLMB && Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryStartGathering();
        }
    }

    private void TryStartGathering()
    {
        if (cachedCamera == null)
            return;

        Vector2 mouseWorld = cachedCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Collider2D hit = Physics2D.OverlapPoint(mouseWorld);

        if (hit == null)
            return;

        TreeGatherConfig config = FindConfigForTree(hit.gameObject.name);
        if (config == null)
            return;

        float distance = Vector2.Distance(transform.position, hit.transform.position);
        if (distance > config.interactionDistance)
            return;

        currentTarget = hit;
        gatherItemName = config.itemName;
        gatherDuration = config.gatherTime;
        gatherTimer = 0f;
        isGathering = true;
        progressDisplay.Show();
    }

    private void CompleteGathering()
    {
        isGathering = false;
        progressDisplay.Hide();

        if (inventory != null && !string.IsNullOrEmpty(gatherItemName))
        {
            inventory.AddItem(gatherItemName);
            QuestManager.Instance.ReportItemCollected(gatherItemName, 1);
        }

        currentTarget = null;
        gatherItemName = null;
    }

    public void OnHitResource(GameObject resource)
    {
        if (inventory == null) return;

        TreeGatherConfig config = FindConfigForTree(resource.name);
        if (config != null)
        {
            inventory.AddItem(config.itemName);
            QuestManager.Instance.ReportItemCollected(config.itemName, 1);
            Debug.Log($"[ResourceGatherer] Gathered {config.itemName} by hitting {resource.name}");
        }
        else if (resource.name.ToLower().Contains("tree") || resource.name.ToLower().Contains("wood"))
        {
            // Fallback for objects named "Tree" or similar if config is missing
            inventory.AddItem("Wood");
            Debug.Log($"[ResourceGatherer] Gathered Wood (fallback) by hitting {resource.name}");
        }
    }

    private void CancelGathering()
    {
        isGathering = false;
        progressDisplay.Hide();
        currentTarget = null;
        gatherItemName = null;
    }

    private bool IsTargetStillValid()
    {
        if (currentTarget == null)
            return false;

        float distance = Vector2.Distance(transform.position, currentTarget.transform.position);
        return distance <= maxInteractionDistance + 1f;
    }

    private TreeGatherConfig FindConfigForTree(string objectName)
    {
        if (treeConfigs == null)
            return null;

        for (int i = 0; i < treeConfigs.Length; i++)
        {
            if (objectName.Contains(treeConfigs[i].treeName))
                return treeConfigs[i];
        }
        return null;
    }
}
