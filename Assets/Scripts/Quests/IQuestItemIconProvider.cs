using UnityEngine;
using UnityEngine.UI;

public interface IQuestItemIconProvider
{
    Sprite GetIcon(string itemName);
}
