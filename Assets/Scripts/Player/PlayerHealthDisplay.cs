using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerHealthDisplay : MonoBehaviour
{
    private const string HealthPanelPath = "Canvas/Main/PlayerInfo/HealthPanel";

    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Image[] healthIcons;

    private void OnEnable()
    {
        ResolveReferences();
        Subscribe();

        if (playerHealth != null)
            UpdateDisplay(playerHealth.CurrentHealth, playerHealth.MaxHealth);
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Start()
    {
        ResolveReferences();

        if (playerHealth != null)
            UpdateDisplay(playerHealth.CurrentHealth, playerHealth.MaxHealth);
    }

    private void UpdateDisplay(int current, int max)
    {
        if (healthIcons == null || healthIcons.Length == 0)
            return;

        int visibleIcons = 0;
        if (current > 0 && max > 0)
        {
            visibleIcons = Mathf.CeilToInt((float)current / max * healthIcons.Length);
            visibleIcons = Mathf.Clamp(visibleIcons, 0, healthIcons.Length);
        }

        for (int i = 0; i < healthIcons.Length; i++)
        {
            if (healthIcons[i] == null)
                continue;

            healthIcons[i].gameObject.SetActive(i < visibleIcons);
        }
    }

    private void ResolveReferences()
    {
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();

        if (healthIcons != null && healthIcons.Length > 0)
            return;

        Transform healthPanel = transform.childCount >= 3 ? transform : FindHealthPanel();
        if (healthPanel == null)
            return;

        healthIcons = new Image[3];
        healthIcons[0] = healthPanel.Find("Health1")?.GetComponent<Image>();
        healthIcons[1] = healthPanel.Find("Health2")?.GetComponent<Image>();
        healthIcons[2] = healthPanel.Find("Health3")?.GetComponent<Image>();
    }

    private void Subscribe()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged += UpdateDisplay;
    }

    private void Unsubscribe()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged -= UpdateDisplay;
    }

    private static Transform FindHealthPanel()
    {
        GameObject healthPanel = GameObject.Find(HealthPanelPath);
        return healthPanel != null ? healthPanel.transform : null;
    }
}
