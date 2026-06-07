using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerScoreProvider : MonoBehaviour
{
    private IPlayerScoreService scoreService;

    public IPlayerScoreService ScoreService
    {
        get
        {
            if (scoreService == null)
            {
                scoreService = new PersistentPlayerScoreService(new JsonPlayerScoreRepository());
            }

            return scoreService;
        }
    }

    private void Awake()
    {
        if (scoreService == null)
        {
            scoreService = new PersistentPlayerScoreService(new JsonPlayerScoreRepository());
        }
    }

    public void ReloadFromCurrentAccount()
    {
        scoreService = new PersistentPlayerScoreService(new JsonPlayerScoreRepository());
    }
}
