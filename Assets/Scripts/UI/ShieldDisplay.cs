using UnityEngine;
using UnityEngine.UI;
using TMPro;

public sealed class ShieldDisplay : MonoBehaviour
{
    [SerializeField] private PlayerBuffReceiver buffReceiver;
    [SerializeField] private Scrollbar shieldBar;
    [SerializeField] private GameObject shieldIndicator;

    private void Start()
    {
        if (buffReceiver == null) buffReceiver = FindFirstObjectByType<PlayerBuffReceiver>();
        if (buffReceiver != null)
        {
            buffReceiver.OnShieldChanged += UpdateDisplay;
            buffReceiver.OnShieldBroken += HideIndicator;
        }
        HideIndicator();
    }

    private void OnDestroy()
    {
        if (buffReceiver != null)
        {
            buffReceiver.OnShieldChanged -= UpdateDisplay;
            buffReceiver.OnShieldBroken -= HideIndicator;
        }
    }

    private void UpdateDisplay(float normalized)
    {
        if (shieldBar != null) shieldBar.size = normalized;
        if (shieldIndicator != null) shieldIndicator.SetActive(normalized > 0f);
    }

    private void HideIndicator()
    {
        if (shieldIndicator != null) shieldIndicator.SetActive(false);
        if (shieldBar != null) shieldBar.size = 0f;
    }
}