using System.Collections.Generic;

public readonly struct ControlBindingDefinition
{
    public readonly string Id;
    public readonly string Category;
    public readonly string Label;
    public readonly string DefaultPath;

    public ControlBindingDefinition(string id, string category, string label, string defaultPath)
    {
        Id = id;
        Category = category;
        Label = label;
        DefaultPath = defaultPath;
    }
}

public static class ControlBindingCatalog
{
    private static readonly List<ControlBindingDefinition> AllBindings = new List<ControlBindingDefinition>
    {
        new ControlBindingDefinition(ControlBindingId.MoveUp, "Движение", "Вперёд", "<Keyboard>/w"),
        new ControlBindingDefinition(ControlBindingId.MoveDown, "Движение", "Назад", "<Keyboard>/s"),
        new ControlBindingDefinition(ControlBindingId.MoveLeft, "Движение", "Влево", "<Keyboard>/a"),
        new ControlBindingDefinition(ControlBindingId.MoveRight, "Движение", "Вправо", "<Keyboard>/d"),
        new ControlBindingDefinition(ControlBindingId.Run, "Движение", "Бег", "<Keyboard>/leftShift"),
        new ControlBindingDefinition(ControlBindingId.Attack, "Бой", "Атака", "<Mouse>/leftButton"),
        new ControlBindingDefinition(ControlBindingId.Gather, "Мир", "Сбор ресурса (удержание)", "<Keyboard>/e"),
        new ControlBindingDefinition(ControlBindingId.Inventory, "Интерфейс", "Рюкзак (Level)", "<Keyboard>/i"),
        new ControlBindingDefinition(ControlBindingId.Pause, "Интерфейс", "Пауза", "<Keyboard>/escape"),
        new ControlBindingDefinition(ControlBindingId.Upgrades, "Дом", "Прокачка (Home)", "<Keyboard>/u"),
        new ControlBindingDefinition(ControlBindingId.Hotbar1, "Хотбар", "Слот 1", "<Keyboard>/1"),
        new ControlBindingDefinition(ControlBindingId.Hotbar2, "Хотбар", "Слот 2", "<Keyboard>/2"),
        new ControlBindingDefinition(ControlBindingId.Hotbar3, "Хотбар", "Слот 3", "<Keyboard>/3"),
        new ControlBindingDefinition(ControlBindingId.Hotbar4, "Хотбар", "Слот 4", "<Keyboard>/4"),
        new ControlBindingDefinition(ControlBindingId.Hotbar5, "Хотбар", "Слот 5", "<Keyboard>/5"),
        new ControlBindingDefinition(ControlBindingId.Hotbar6, "Хотбар", "Слот 6", "<Keyboard>/6"),
        new ControlBindingDefinition(ControlBindingId.Hotbar7, "Хотбар", "Слот 7", "<Keyboard>/7"),
        new ControlBindingDefinition(ControlBindingId.Hotbar8, "Хотбар", "Слот 8", "<Keyboard>/8"),
        new ControlBindingDefinition(ControlBindingId.Hotbar9, "Хотбар", "Слот 9", "<Keyboard>/9"),
        new ControlBindingDefinition(ControlBindingId.Hotbar0, "Хотбар", "Слот 0", "<Keyboard>/0"),
    };

    public static IReadOnlyList<ControlBindingDefinition> All => AllBindings;

    public static bool TryGet(string id, out ControlBindingDefinition definition)
    {
        for (int i = 0; i < AllBindings.Count; i++)
        {
            if (AllBindings[i].Id == id)
            {
                definition = AllBindings[i];
                return true;
            }
        }

        definition = default;
        return false;
    }

    public static string GetDefaultPath(string id)
    {
        return TryGet(id, out ControlBindingDefinition definition) ? definition.DefaultPath : string.Empty;
    }
}
