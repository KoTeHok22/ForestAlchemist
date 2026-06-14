using UnityEngine;

[RequireComponent(typeof(PlayerTopDownController))]
public sealed class VisibilitySystem : MonoBehaviour
{
    [Header("Speed Penalty")]
    [SerializeField] private float minSpeedMultiplier = 0.4f;
    [SerializeField] private float visibilityPerItem = 0.08f;
    [SerializeField] private float speedPenaltyPerVisibility = 0.06f;

    [Header("Detection")]
    [SerializeField] private float baseDetectionRange = 8f;
    [SerializeField] private float detectionBonusPerVisibility = 1.5f;

    private PlayerInventory expeditionInventory;
    private float currentVisibility;
    private float currentSpeedMultiplier = 1f;

    public float CurrentVisibility => currentVisibility;
    public float CurrentSpeedMultiplier => currentSpeedMultiplier;
    public float CurrentDetectionBonus => currentVisibility * detectionBonusPerVisibility;
    public float EffectiveDetectionRange => baseDetectionRange + CurrentDetectionBonus;

    public event System.Action<float> OnVisibilityChanged;

    private void Start()
    {
        expeditionInventory = ExpeditionManager.Instance.ExpeditionInventory;
        if (expeditionInventory != null)
        {
            expeditionInventory.OnInventoryChanged += Recalculate;
            Recalculate();
        }
    }

    private void OnDestroy()
    {
        if (expeditionInventory != null)
            expeditionInventory.OnInventoryChanged -= Recalculate;
    }

    private void Recalculate()
    {
        float visibility = 0f;
        if (expeditionInventory != null)
        {
            foreach (var slot in expeditionInventory.GetAllSlots())
            {
                visibility += slot.count * visibilityPerItem;
            }
        }

        currentVisibility = visibility;
        currentSpeedMultiplier = Mathf.Max(minSpeedMultiplier, 1f - visibility * speedPenaltyPerVisibility);

        ApplySpeedPenalty();
        OnVisibilityChanged?.Invoke(currentVisibility);
    }

    private void ApplySpeedPenalty()
    {
        PlayerTopDownController controller = GetComponent<PlayerTopDownController>();
        if (controller != null)
        {
            controller.SetSpeedMultiplier(currentSpeedMultiplier);
        }
    }
}
