using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Opens the character upgrade panel on Home with U (toggle).
/// </summary>
public sealed class StatUpgradeToggleInput : MonoBehaviour
{
    [SerializeField] private StatUpgradeUI statUpgradeUI;

    private void Awake()
    {
        if (statUpgradeUI == null)
        {
            statUpgradeUI = GetComponent<StatUpgradeUI>();
        }

        if (statUpgradeUI == null)
        {
            statUpgradeUI = FindFirstObjectByType<StatUpgradeUI>();
        }
    }

    private void Update()
    {
        if (statUpgradeUI == null || !GameControls.WasPressedThisFrame(ControlBindingId.Upgrades))
        {
            return;
        }

        if (HomeUIBlocker.IsBlocked && !statUpgradeUI.IsOpen)
        {
            return;
        }

        statUpgradeUI.Toggle();
    }
}
