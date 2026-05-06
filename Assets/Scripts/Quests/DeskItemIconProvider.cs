using UnityEngine;
using UnityEngine.UI;

public sealed class DeskItemIconProvider : MonoBehaviour, IQuestItemIconProvider
{
    [SerializeField] private Transform itemsContainer;

    public Sprite GetIcon(string itemName)
    {
        if (itemsContainer == null)
            return null;

        Transform item = itemsContainer.Find(itemName);
        if (item == null)
            return null;

        Image image = item.GetComponent<Image>();
        return image != null ? image.sprite : null;
    }
}
