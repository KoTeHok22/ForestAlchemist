using UnityEngine;

public sealed class GardenService : MonoBehaviour
{
    private static GardenService instance;
    public static GardenService Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("GardenService");
                instance = go.AddComponent<GardenService>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AdvanceGrowth()
    {
        var progress = GameCore.Instance.CurrentProgress;
        if (progress == null) return;

        var garden = progress.garden;
        if (garden.currentStage < garden.maxStage)
        {
            garden.expeditionsToNextStage--;
            if (garden.expeditionsToNextStage <= 0)
            {
                garden.currentStage++;
                garden.expeditionsToNextStage = Mathf.Clamp(garden.currentStage + 1, 1, 4);
            }
        }

        GameCore.Instance.SaveProgress();
    }

    public void ResetGarden()
    {
        var garden = GameCore.Instance.CurrentProgress.garden;
        garden.currentStage = 0;
        garden.expeditionsToNextStage = 1;
        GameCore.Instance.SaveProgress();
    }
}
