using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class InventoryToggleInput : MonoBehaviour
{
    [SerializeField] private ExpeditionInventoryUI expeditionInventoryUI;

    private void Awake()
    {
        if (expeditionInventoryUI == null)
        {
            expeditionInventoryUI = GetComponent<ExpeditionInventoryUI>();
        }

        if (expeditionInventoryUI == null)
        {
            expeditionInventoryUI = FindFirstObjectByType<ExpeditionInventoryUI>();
        }
    }

    private void Update()
    {
        if (expeditionInventoryUI == null || !GameControls.WasPressedThisFrame(ControlBindingId.Inventory))
        {
            return;
        }

        if (SceneManager.GetActiveScene().name != "Level")
        {
            return;
        }

        if (Time.timeScale <= 0f && !expeditionInventoryUI.IsOpen)
        {
            return;
        }

        expeditionInventoryUI.Toggle();
    }
}
