using System;
using System.IO;
using UnityEngine;

[Serializable]
public sealed class PlayerScoreSaveData
{
    public int totalScore;
}

public interface IPlayerScoreRepository
{
    PlayerScoreSaveData Load();
    void Save(PlayerScoreSaveData data);
}

public sealed class JsonPlayerScoreRepository : IPlayerScoreRepository
{
    private readonly string saveFilePath;

    public JsonPlayerScoreRepository(string fileName = "player_score.json")
    {
        saveFilePath = Path.Combine(Application.persistentDataPath, fileName);
    }

    public PlayerScoreSaveData Load()
    {
        GameProgressData progress = GameCore.Instance.CurrentProgress;
        if (progress?.score != null)
        {
            return progress.score;
        }

        if (!File.Exists(saveFilePath))
        {
            return new PlayerScoreSaveData();
        }

        try
        {
            string json = File.ReadAllText(saveFilePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new PlayerScoreSaveData();
            }

            PlayerScoreSaveData data = JsonUtility.FromJson<PlayerScoreSaveData>(json);
            return data ?? new PlayerScoreSaveData();
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Failed to load player score from '{saveFilePath}': {exception.Message}");
            return new PlayerScoreSaveData();
        }
    }

    public void Save(PlayerScoreSaveData data)
    {
        GameProgressData progress = GameCore.Instance.CurrentProgress;
        if (progress != null)
        {
            progress.score = data ?? new PlayerScoreSaveData();
            GameProgressUtility.Touch(progress);
            GameCore.Instance.SaveProgress();
            return;
        }

        try
        {
            string directory = Path.GetDirectoryName(saveFilePath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonUtility.ToJson(data ?? new PlayerScoreSaveData(), true);
            File.WriteAllText(saveFilePath, json);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Failed to save player score to '{saveFilePath}': {exception.Message}");
        }
    }
}

public interface IPlayerScoreService
{
    int CurrentScore { get; }
    void AddScore(int amount);
    void ResetScore();
}

public sealed class PersistentPlayerScoreService : IPlayerScoreService
{
    private readonly IPlayerScoreRepository repository;
    private PlayerScoreSaveData cachedData;

    public PersistentPlayerScoreService(IPlayerScoreRepository repository)
    {
        this.repository = repository;
        cachedData = repository?.Load() ?? new PlayerScoreSaveData();
    }

    public int CurrentScore => Mathf.Max(0, cachedData?.totalScore ?? 0);

    public void AddScore(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        cachedData.totalScore = Mathf.Max(0, CurrentScore + amount);
        repository?.Save(cachedData);
    }

    public void ResetScore()
    {
        cachedData.totalScore = 0;
        repository?.Save(cachedData);
    }
}
