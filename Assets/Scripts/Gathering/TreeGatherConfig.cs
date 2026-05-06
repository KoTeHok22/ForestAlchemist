using UnityEngine;

[CreateAssetMenu(fileName = "NewTreeConfig", menuName = "Game/Tree Config")]
public sealed class TreeGatherConfig : ScriptableObject
{
    public string treeName;
    public string itemName;
    public float gatherTime = 3f;
    public float interactionDistance = 2f;
}
