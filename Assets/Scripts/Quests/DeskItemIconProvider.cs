using UnityEngine;

public sealed class DeskItemIconProvider : MonoBehaviour, IQuestItemIconProvider
{
    [SerializeField] private Sprite sakuraIcon;
    [SerializeField] private Sprite oakIcon;
    [SerializeField] private Sprite appleIcon;
    [SerializeField] private Sprite orcDropIcon;
    [SerializeField] private Sprite orcBloodIcon;

    public Sprite GetIcon(string itemName)
    {
        itemName = ItemCatalog.Normalize(itemName);

        return itemName switch
        {
            ItemCatalog.SakuraSapling => sakuraIcon,
            ItemCatalog.OakSapling => oakIcon,
            ItemCatalog.AppleSapling => appleIcon,
            ItemCatalog.GreenOrcDrop => orcDropIcon,
            ItemCatalog.OrcBlood => orcBloodIcon,
            _ => null
        };
    }
}
