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
        return itemName switch
        {
            "СаженецСакуры" => sakuraIcon,
            "СаженецДуба" => oakIcon,
            "СаженецЯблони" => appleIcon,
            "ДропСЗеленогоОрка" => orcDropIcon,
            "КровьОрка" => orcBloodIcon,
            _ => null
        };
    }
}
