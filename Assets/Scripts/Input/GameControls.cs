using System.Collections.Generic;

public static class GameControls
{
    private static readonly Dictionary<string, string> Overrides = new Dictionary<string, string>();

    public static bool IsListeningForRebind { get; private set; }

    public static void SetListeningForRebind(bool listening)
    {
        IsListeningForRebind = listening;
    }

    public static void LoadFrom(MenuSettingsData settings)
    {
        Overrides.Clear();
        if (settings?.controlBindings == null)
        {
            return;
        }

        for (int i = 0; i < settings.controlBindings.Count; i++)
        {
            ControlBindingEntry entry = settings.controlBindings[i];
            if (entry == null || string.IsNullOrEmpty(entry.id) || string.IsNullOrEmpty(entry.path))
            {
                continue;
            }

            Overrides[entry.id] = entry.path;
        }
    }

    public static void WriteTo(MenuSettingsData settings)
    {
        if (settings == null)
        {
            return;
        }

        if (settings.controlBindings == null)
        {
            settings.controlBindings = new List<ControlBindingEntry>();
        }

        settings.controlBindings.Clear();
        IReadOnlyList<ControlBindingDefinition> all = ControlBindingCatalog.All;
        for (int i = 0; i < all.Count; i++)
        {
            settings.controlBindings.Add(new ControlBindingEntry
            {
                id = all[i].Id,
                path = GetPath(all[i].Id)
            });
        }
    }

    public static string GetPath(string bindingId)
    {
        if (Overrides.TryGetValue(bindingId, out string path) && !string.IsNullOrEmpty(path))
        {
            return path;
        }

        return ControlBindingCatalog.GetDefaultPath(bindingId);
    }

    public static string GetDisplayName(string bindingId)
    {
        return InputBindingUtility.FormatDisplayName(GetPath(bindingId));
    }

    public static void SetPath(string bindingId, string path)
    {
        if (string.IsNullOrEmpty(bindingId) || string.IsNullOrEmpty(path))
        {
            return;
        }

        Overrides[bindingId] = path;
    }

    public static void ResetToDefaults()
    {
        Overrides.Clear();
    }

    public static bool IsPressed(string bindingId)
    {
        return InputBindingUtility.IsPressed(GetPath(bindingId));
    }

    public static bool WasPressedThisFrame(string bindingId)
    {
        return InputBindingUtility.WasPressedThisFrame(GetPath(bindingId));
    }

    public static bool WasHotbarSlotPressed(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 10)
        {
            return false;
        }

        string bindingId = slotIndex switch
        {
            0 => ControlBindingId.Hotbar1,
            1 => ControlBindingId.Hotbar2,
            2 => ControlBindingId.Hotbar3,
            3 => ControlBindingId.Hotbar4,
            4 => ControlBindingId.Hotbar5,
            5 => ControlBindingId.Hotbar6,
            6 => ControlBindingId.Hotbar7,
            7 => ControlBindingId.Hotbar8,
            8 => ControlBindingId.Hotbar9,
            9 => ControlBindingId.Hotbar0,
            _ => string.Empty
        };

        return WasPressedThisFrame(bindingId);
    }
}
