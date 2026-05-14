using UnityEngine;
using UnityEngine.UI;

public sealed class LevelQuestIconProvider : MonoBehaviour, IQuestItemIconProvider
{
    [SerializeField] private Sprite sakuraIcon;
    [SerializeField] private Sprite oakIcon;
    [SerializeField] private Sprite appleIcon;
    [SerializeField] private Sprite orcDropIcon;
    [SerializeField] private Sprite orcBloodIcon;

    public Sprite GetIcon(string itemName)
    {
        itemName = ItemCatalog.Normalize(itemName);

        Sprite icon = itemName switch
        {
            ItemCatalog.SakuraSapling => sakuraIcon,
            ItemCatalog.OakSapling => oakIcon,
            ItemCatalog.AppleSapling => appleIcon,
            ItemCatalog.GreenOrcDrop => orcDropIcon,
            ItemCatalog.OrcBlood => orcBloodIcon,
            _ => null
        };

        if (icon != null)
        {
            return icon;
        }

        return Resources.Load<Sprite>($"Game/Items/{itemName}");
    }
}
