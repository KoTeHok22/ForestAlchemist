using UnityEngine;
using UnityEngine.InputSystem;

public sealed class ResourceGatherer : MonoBehaviour
{
    private enum GatherInputMode
    {
        None,
        Mouse,
        Keyboard
    }

    [SerializeField] private GatherProgressDisplay progressDisplay;

    private PlayerInventory inventory;
    private Camera cachedCamera;
    private GatherableResourceInteraction currentResource;
    private GatherInputMode currentInputMode;
    private bool isGathering;
    private float gatherTimer;

    public bool IsBusy => isGathering;

    private void Awake()
    {
        EnsureProgressDisplay();
    }

    public void Initialize(PlayerInventory inventory)
    {
        this.inventory = inventory ?? ExpeditionManager.Instance?.ExpeditionInventory;
        ExpeditionItemTrace.LogInventory(
            "ResourceGatherer.Initialize",
            this.inventory,
            $"gatherer={GetHashCode()} on={gameObject.name}");
    }

    private PlayerInventory ResolveInventory()
    {
        PlayerInventory current = ExpeditionManager.Instance?.ExpeditionInventory;
        if (current != null)
        {
            inventory = current;
        }

        return inventory;
    }

    public void ConfigureProgressDisplay(GatherProgressDisplay display)
    {
        progressDisplay = display;
    }

    public bool IsGathering(GatherableResourceInteraction resource)
    {
        return isGathering && currentResource == resource;
    }

    public bool TryStartGathering(GatherableResourceInteraction resource, bool useMouseInput)
    {
        EnsureProgressDisplay();

        if (resource == null || !resource.CanBeginGathering(requirePointer: useMouseInput))
        {
            return false;
        }

        if (isGathering && currentResource != resource)
        {
            return false;
        }

        currentResource = resource;
        currentInputMode = useMouseInput ? GatherInputMode.Mouse : GatherInputMode.Keyboard;
        gatherTimer = 0f;
        isGathering = true;

        AudioHooks.Bridge?.PlayGatherStart();

        if (progressDisplay != null)
        {
            progressDisplay.Show();
            progressDisplay.SetProgress(0f);
        }

        return true;
    }

    public void CancelGathering(GatherableResourceInteraction resource = null)
    {
        EnsureProgressDisplay();

        if (!isGathering)
        {
            return;
        }

        if (resource != null && resource != currentResource)
        {
            return;
        }

        isGathering = false;
        gatherTimer = 0f;
        currentInputMode = GatherInputMode.None;
        currentResource = null;

        AudioHooks.Bridge?.PlayGatherCancel();

        if (progressDisplay != null)
        {
            progressDisplay.Hide();
        }
    }

    public bool ShouldBlockPrimaryAttack()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
        {
            return false;
        }

        if (isGathering)
        {
            return currentInputMode == GatherInputMode.Mouse;
        }

        GatherableResourceInteraction resource = FindGatherableUnderCursor();
        return resource != null && resource.CanBeginGathering(requirePointer: true);
    }

    private void Update()
    {
        EnsureProgressDisplay();

        if (!isGathering)
        {
            return;
        }

        if (currentResource == null || !currentResource.CanContinueGathering(currentInputMode == GatherInputMode.Mouse))
        {
            CancelGathering();
            return;
        }

        float duration = Mathf.Max(0.1f, currentResource.GatherTime);
        gatherTimer += Time.deltaTime;

        if (progressDisplay != null)
        {
            progressDisplay.SetProgress(gatherTimer / duration);
        }

        if (gatherTimer >= duration)
        {
            CompleteGathering();
        }
    }

    private void CompleteGathering()
    {
        if (currentResource == null)
        {
            CancelGathering();
            return;
        }

        string gatheredItem = currentResource.ItemName;
        string resourceName = currentResource.gameObject.name;
        ExpeditionItemTrace.Log(
            "Gather.Complete",
            $"resource={resourceName} item={gatheredItem} gatherer={GetHashCode()} on={gameObject.name}");

        currentResource.NotifyGatherCompleted();
        CancelGathering();

        if (!string.IsNullOrEmpty(gatheredItem))
        {
            PlayerInventory pack = ResolveInventory();
            if (pack == null)
            {
                ExpeditionItemTrace.Log(
                    "Gather.AddItem",
                    $"FAILED item={gatheredItem}: ExpeditionInventory is NULL (gatherer={GetHashCode()})");
            }
            else
            {
                ExpeditionItemTrace.LogInventory("Gather.AddItem.Before", pack, $"item={gatheredItem}");
                pack.AddItem(gatheredItem);
                ExpeditionItemTrace.LogInventory("Gather.AddItem.After", pack, $"item={gatheredItem}");
                QuestManager.Instance.ReportItemCollected(gatheredItem, 1);
                AudioHooks.Bridge?.PlayGatherComplete(gatheredItem);
            }
        }
        else
        {
            ExpeditionItemTrace.Log("Gather.Complete", $"resource={resourceName} has empty item id");
        }
    }

    private GatherableResourceInteraction FindGatherableUnderCursor()
    {
        if (cachedCamera == null)
        {
            cachedCamera = Camera.main;
        }

        if (cachedCamera == null || Mouse.current == null)
        {
            return null;
        }

        Vector2 worldPoint = cachedCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Collider2D[] hits = Physics2D.OverlapPointAll(worldPoint);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null)
            {
                continue;
            }

            GatherableResourceInteraction resource = hit.GetComponent<GatherableResourceInteraction>();
            if (resource == null)
            {
                resource = hit.GetComponentInParent<GatherableResourceInteraction>();
            }

            if (resource != null)
            {
                return resource;
            }
        }

        return null;
    }

    private void EnsureProgressDisplay()
    {
        if (progressDisplay == null)
        {
            progressDisplay = GetComponent<GatherProgressDisplay>();
        }
    }
}
