using UnityEngine;
using UnityEngine.InputSystem;

public sealed class InventoryToggleInput : MonoBehaviour
{
    [SerializeField] private InventoryDisplay inventoryDisplay;

    private void Awake()
    {
        if (inventoryDisplay == null)
        {
            InventoryDisplay[] displays = Object.FindObjectsByType<InventoryDisplay>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            inventoryDisplay = displays.Length > 0 ? displays[0] : null;
        }
    }

    private void Update()
    {
        if (inventoryDisplay == null || Keyboard.current == null || !Keyboard.current.iKey.wasPressedThisFrame)
        {
            return;
        }

        inventoryDisplay.Toggle();
    }
}
