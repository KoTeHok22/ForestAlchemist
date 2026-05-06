public interface IPlayerQuestRepository
{
    PlayerQuestSave Load();
    void Save(PlayerQuestSave data);
}
