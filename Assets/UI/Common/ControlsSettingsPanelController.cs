using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UIElements;

/// <summary>
/// App UI panel for rebinding keyboard/mouse controls. Shared by MainMenu settings and Pause.
/// </summary>
public sealed class ControlsSettingsPanelController
{
    private readonly VisualElement root;
    private readonly ScrollView listHost;
    private readonly Label statusLabel;
    private readonly VisualElement btnReset;
    private readonly VisualElement btnBack;

    private AppUIClickRouter clickRouter;
    private readonly Dictionary<string, Label> bindingLabels = new Dictionary<string, Label>();
    private string pendingBindingId;
    private string currentCategory;

    /// <param name="accountService">
    /// Unused — control bindings are now machine-wide (see <see cref="GlobalSettingsStore"/>).
    /// Kept in the signature so existing call sites need not change.
    /// </param>
    public ControlsSettingsPanelController(VisualElement root, IMenuAccountService accountService)
    {
        this.root = root;
        listHost = root.Q<ScrollView>("controls-list");
        statusLabel = root.Q<Label>("controls-rebind-status");
        btnReset = root.Q<VisualElement>("btn-controls-reset");
        btnBack = root.Q<VisualElement>("btn-controls-back");

        clickRouter = new AppUIClickRouter(root);
        if (btnReset != null) clickRouter.Add(btnReset, ResetToDefaults);
        if (btnBack != null) clickRouter.Add(btnBack, () =>
        {
            CancelRebind();
            OnBackRequested?.Invoke();
        });

        RebuildList();
    }

    public System.Action OnBackRequested;

    public void Refresh()
    {
        RebuildList();
        SetStatus(string.Empty);
        CancelRebind();
    }

    public void CancelRebind()
    {
        pendingBindingId = null;
        GameControls.SetListeningForRebind(false);
        InputSystem.onAfterUpdate -= PollRebindInput;
    }

    private void RebuildList()
    {
        if (listHost == null)
        {
            return;
        }

        VisualElement host = listHost.contentContainer;
        host.Clear();
        clickRouter?.RemoveDead();
        bindingLabels.Clear();
        currentCategory = string.Empty;

        IReadOnlyList<ControlBindingDefinition> all = ControlBindingCatalog.All;
        for (int i = 0; i < all.Count; i++)
        {
            ControlBindingDefinition definition = all[i];
            if (definition.Category != currentCategory)
            {
                currentCategory = definition.Category;
                Label section = new Label(currentCategory);
                section.AddToClassList("section-title");
                host.Add(section);
            }

            VisualElement row = new VisualElement();
            row.AddToClassList("control-bind-row");

            Label name = new Label(definition.Label);
            name.AddToClassList("control-bind-label");
            row.Add(name);

            VisualElement button = new VisualElement();
            button.AddToClassList("control-bind-btn");
            button.userData = definition.Id;

            Label value = new Label(GameControls.GetDisplayName(definition.Id));
            value.AddToClassList("control-bind-btn__text");
            button.Add(value);
            bindingLabels[definition.Id] = value;

            clickRouter.Add(button, () => BeginRebind(definition.Id));
            row.Add(button);
            host.Add(row);
        }
    }

    private void BeginRebind(string bindingId)
    {
        pendingBindingId = bindingId;
        GameControls.SetListeningForRebind(true);
        InputSystem.onAfterUpdate -= PollRebindInput;
        InputSystem.onAfterUpdate += PollRebindInput;
        SetStatus("Нажмите клавишу или кнопку мыши… (Esc — отмена)");
    }

    private void PollRebindInput()
    {
        if (string.IsNullOrEmpty(pendingBindingId))
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            CancelRebind();
            SetStatus("Отменено");
            return;
        }

        if (keyboard != null)
        {
            foreach (KeyControl key in keyboard.allKeys)
            {
                if (key != null && key.wasPressedThisFrame)
                {
                    ApplyBinding(key);
                    return;
                }
            }
        }

        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        if (mouse.leftButton.wasPressedThisFrame)
        {
            ApplyBinding(mouse.leftButton);
        }
        else if (mouse.rightButton.wasPressedThisFrame)
        {
            ApplyBinding(mouse.rightButton);
        }
        else if (mouse.middleButton.wasPressedThisFrame)
        {
            ApplyBinding(mouse.middleButton);
        }
    }

    private void ApplyBinding(InputControl control)
    {
        string path = InputBindingUtility.NormalizeControlPath(control);
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        GameControls.SetPath(pendingBindingId, path);
        if (bindingLabels.TryGetValue(pendingBindingId, out Label label))
        {
            label.text = GameControls.GetDisplayName(pendingBindingId);
        }

        SaveBindings();
        SetStatus("Сохранено");
        CancelRebind();
    }

    private void ResetToDefaults()
    {
        GameControls.ResetToDefaults();
        SaveBindings();
        RebuildList();
        SetStatus("Сброшено к стандартным");
        CancelRebind();
    }

    private void SaveBindings()
    {
        // Control bindings are machine-wide (shared by every account on this PC).
        MenuSettingsData settings = GlobalSettingsStore.Current;
        GameControls.WriteTo(settings);
        GlobalSettingsStore.Save(settings);
        GameControls.LoadFrom(settings);
    }

    private void SetStatus(string message)
    {
        if (statusLabel != null)
        {
            statusLabel.text = message ?? string.Empty;
        }
    }
}
