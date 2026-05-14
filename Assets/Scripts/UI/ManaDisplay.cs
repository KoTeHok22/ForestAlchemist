using UnityEngine;
using UnityEngine.UI;
using TMPro;

public sealed class ManaDisplay : MonoBehaviour
{
    [SerializeField] private PlayerSpellCaster spellCaster;
    [SerializeField] private Scrollbar manaBar;
    [SerializeField] private TMP_Text manaText;

    private void Start()
    {
        if (spellCaster == null) spellCaster = FindFirstObjectByType<PlayerSpellCaster>();
        if (spellCaster != null)
        {
            spellCaster.OnManaChanged += UpdateDisplay;
            UpdateDisplay(spellCaster.CurrentMana, spellCaster.MaxMana);
        }
    }

    private void OnDestroy()
    {
        if (spellCaster != null) spellCaster.OnManaChanged -= UpdateDisplay;
    }

    private void UpdateDisplay(float current, float max)
    {
        if (manaBar != null) manaBar.size = max > 0f ? current / max : 0f;
        if (manaText != null) manaText.text = $"{(int)current}/{(int)max}";
    }
}