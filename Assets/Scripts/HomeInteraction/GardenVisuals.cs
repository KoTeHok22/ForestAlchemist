using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class GardenVisuals : MonoBehaviour
{
    [SerializeField] private Sprite[] stageSprites;
    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        var progress = GameCore.Instance.CurrentProgress;
        if (progress == null || stageSprites == null || stageSprites.Length == 0) return;

        int stage = progress.garden.currentStage;
        if (sr != null)
        {
            sr.sprite = stageSprites[Mathf.Clamp(stage, 0, stageSprites.Length - 1)];
        }
    }
}
