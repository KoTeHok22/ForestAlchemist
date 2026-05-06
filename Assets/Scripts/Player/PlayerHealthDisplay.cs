using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerHealthDisplay : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Image[] healthIcons;

    private void OnEnable()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged += UpdateDisplay;
    }

    private void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged -= UpdateDisplay;
    }

    private void Start()
    {
        if (playerHealth == null)
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>();
            if (playerHealth != null)
                playerHealth.OnHealthChanged += UpdateDisplay;
        }

        if (playerHealth != null)
            UpdateDisplay(playerHealth.CurrentHealth, playerHealth.MaxHealth);
    }

    private void UpdateDisplay(int current, int max)
    {
        if (healthIcons == null || healthIcons.Length == 0)
            return;

        float healthPerIcon = (float)max / healthIcons.Length;

        for (int i = 0; i < healthIcons.Length; i++)
        {
            if (healthIcons[i] == null)
                continue;

            float threshold = healthPerIcon * (i + 1);
            healthIcons[i].gameObject.SetActive(current >= threshold);
        }
    }
}
