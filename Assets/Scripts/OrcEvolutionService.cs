using UnityEngine;

public sealed class OrcEvolutionService : MonoBehaviour
{
    private static OrcEvolutionService instance;
    public static OrcEvolutionService Instance => ResolveInstance();

    private static OrcEvolutionService ResolveInstance()
    {
        if (RuntimeSingletonGuard.IsShuttingDown) return null;
        if (instance == null)
        {
            instance = FindAnyObjectByType<OrcEvolutionService>(FindObjectsInactive.Include);
        }
        if (instance == null)
        {
            GameObject go = new GameObject("OrcEvolutionService");
            instance = go.AddComponent<OrcEvolutionService>();
            DontDestroyOnLoad(go);
        }
        return instance;
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

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public void Evolve(bool playerDied)
    {
        var progress = GameCore.Instance.CurrentProgress;
        if (progress == null) return;

        var orcs = progress.orcs;
        orcs.threatLevel++;
        orcs.statMultiplier = Mathf.Clamp(orcs.statMultiplier + (playerDied ? 0.1f : 0.05f), 1f, 4f);
        GameCore.Instance.SaveProgress();
    }
}
