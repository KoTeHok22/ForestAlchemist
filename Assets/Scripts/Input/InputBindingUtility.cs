using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public static class InputBindingUtility
{
    public static bool IsPressed(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        if (TryGetMouseButton(path, out ButtonControl button))
        {
            return button != null && button.isPressed;
        }

        if (TryGetKey(path, out KeyControl keyControl))
        {
            return keyControl != null && keyControl.isPressed;
        }

        return false;
    }

    public static bool WasPressedThisFrame(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        if (TryGetMouseButton(path, out ButtonControl button))
        {
            return button != null && button.wasPressedThisFrame;
        }

        if (TryGetKey(path, out KeyControl keyControl))
        {
            return keyControl != null && keyControl.wasPressedThisFrame;
        }

        return false;
    }

    public static string FormatDisplayName(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return "—";
        }

        if (path.Contains("leftButton"))
        {
            return "ЛКМ";
        }

        if (path.Contains("rightButton"))
        {
            return "ПКМ";
        }

        if (path.Contains("middleButton"))
        {
            return "СКМ";
        }

        if (path.Contains("leftShift") || path.Contains("rightShift"))
        {
            return "Shift";
        }

        if (path.Contains("leftCtrl") || path.Contains("rightCtrl"))
        {
            return "Ctrl";
        }

        if (path.Contains("leftAlt") || path.Contains("rightAlt"))
        {
            return "Alt";
        }

        if (path.Contains("escape"))
        {
            return "Esc";
        }

        int slash = path.LastIndexOf('/');
        if (slash >= 0 && slash < path.Length - 1)
        {
            string tail = path.Substring(slash + 1);
            if (tail.Length == 1)
            {
                return tail.ToUpperInvariant();
            }

            if (tail.StartsWith("digit"))
            {
                return tail.Substring(5);
            }

            return tail;
        }

        return path;
    }

    public static string NormalizeControlPath(InputControl control)
    {
        if (control == null)
        {
            return string.Empty;
        }

        if (control.device is Mouse)
        {
            if (control is ButtonControl)
            {
                return $"<Mouse>/{control.name}";
            }
        }

        if (control.device is Keyboard)
        {
            return $"<Keyboard>/{control.name}";
        }

        return control.path;
    }

    private static bool TryGetMouseButton(string path, out ButtonControl button)
    {
        button = null;
        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return false;
        }

        if (path.Contains("leftButton"))
        {
            button = mouse.leftButton;
            return true;
        }

        if (path.Contains("rightButton"))
        {
            button = mouse.rightButton;
            return true;
        }

        if (path.Contains("middleButton"))
        {
            button = mouse.middleButton;
            return true;
        }

        return false;
    }

    private static bool TryGetKey(string path, out KeyControl keyControl)
    {
        keyControl = null;
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return false;
        }

        keyControl = InputControlPath.TryFindControl(keyboard, path) as KeyControl;
        if (keyControl != null)
        {
            return true;
        }

        string normalized = path.StartsWith("/") ? $"<Keyboard>{path}" : path;
        keyControl = InputControlPath.TryFindControl(keyboard, normalized) as KeyControl;
        return keyControl != null;
    }
}
