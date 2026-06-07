public sealed class JsonPlayerQuestRepository : IPlayerQuestRepository
{
    public PlayerQuestSave Load()
    {
        return GameCore.Instance.CurrentProgress?.quests ?? new PlayerQuestSave();
    }

    public void Save(PlayerQuestSave data)
    {
        GameProgressData progress = GameCore.Instance.CurrentProgress;
        if (progress == null)
        {
            return;
        }

        progress.quests = data ?? new PlayerQuestSave();
        progress.quests.boardQuestIds ??= new System.Collections.Generic.List<string>();
        progress.quests.activeQuestIds ??= new System.Collections.Generic.List<string>();
        GameProgressUtility.Touch(progress);
        GameCore.Instance.SaveProgress();
    }
}
