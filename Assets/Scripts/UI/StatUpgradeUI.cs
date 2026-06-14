using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// App UI character upgrade board on Home. Opened via StatUpgradeInteraction.
/// </summary>
public sealed class StatUpgradeUI : MonoBehaviour
{
    private const string ViewPath = "Assets/UI/HomePanels/StatUpgradeView.uxml";
    private const string SettingsPath = "Assets/UI/HomePanels/StatUpgradePanelSettings.asset";
    private const string ViewResourcePath = "UI/HomePanels/StatUpgradeView";
    private const string SettingsResourcePath = "UI/HomePanels/StatUpgradePanelSettings";
    private const string ChildName = "StatUpgradeOverlay_AppUI";

    private UIDocument document;
    private VisualElement root;
    private VisualElement panelRoot;
    private VisualElement dimBg;
    private VisualElement bloodIcon;
    private Label bloodCount;
    private Label summary;
    private ScrollView statsList;
    private AppUIClickRouter clickRouter;

    private bool built;
    private bool isOpen;

    public bool IsOpen => isOpen;

    private void OnDestroy()
    {
        if (PlayerUpgradeService.Instance != null)
        {
            PlayerUpgradeService.Instance.OnUpgradesChanged -= Refresh;
        }

        if (isOpen)
        {
            HomeUIBlocker.Release();
            isOpen = false;
        }
    }

    public void Toggle()
    {
        if (isOpen) Close();
        else Open();
    }

    public void Open()
    {
        if (!EnsureBuilt())
        {
            Debug.LogError("[StatUpgradeUI] Не удалось открыть панель улучшений: UI не собран.");
            return;
        }

        if (dimBg != null) dimBg.style.display = DisplayStyle.Flex;
        if (panelRoot != null) panelRoot.pickingMode = PickingMode.Position;

        PlayerUpgradeService.Instance.OnUpgradesChanged -= Refresh;
        PlayerUpgradeService.Instance.OnUpgradesChanged += Refresh;
        Refresh();

        if (!isOpen)
        {
            HomeUIBlocker.Acquire();
            isOpen = true;
        }
    }

    public void Close()
    {
        if (dimBg != null) dimBg.style.display = DisplayStyle.None;
        if (panelRoot != null) panelRoot.pickingMode = PickingMode.Ignore;

        if (PlayerUpgradeService.Instance != null)
        {
            PlayerUpgradeService.Instance.OnUpgradesChanged -= Refresh;
        }

        if (isOpen)
        {
            HomeUIBlocker.Release();
            isOpen = false;
        }
    }

    private bool EnsureBuilt()
    {
        if (built && panelRoot != null && dimBg != null)
        {
            return true;
        }

        Transform existing = transform.Find(ChildName);
        GameObject host = existing != null ? existing.gameObject : new GameObject(ChildName);
        if (existing == null) host.transform.SetParent(null, false);

        document = host.GetComponent<UIDocument>();
        if (document == null)
        {
            document = host.AddComponent<UIDocument>();
        }

        if (!HomePanelUiLoader.AssignAssets(document, ViewResourcePath, SettingsResourcePath, ViewPath, SettingsPath))
        {
            Debug.LogError("[StatUpgradeUI] visualTreeAsset или panelSettings не найдены.");
            return false;
        }

        if (!HomePanelUiLoader.TryResolveShell(document, out root, out panelRoot, out dimBg))
        {
            Debug.LogError("[StatUpgradeUI] Разметка панели неполная (panel-root/dim-bg).");
            return false;
        }

        bloodIcon = root.Q<VisualElement>("blood-icon");
        bloodCount = root.Q<Label>("blood-count");
        summary = root.Q<Label>("summary");
        statsList = root.Q<ScrollView>("stats-list");

        clickRouter = new AppUIClickRouter(root);
        VisualElement closeX = root.Q<VisualElement>("btn-close-x");
        if (closeX != null) clickRouter.Add(closeX, Close);

        Sprite bloodSprite = Resources.Load<Sprite>("Game/Items/5.1_krov_orka");
        if (bloodIcon != null && bloodSprite != null)
        {
            bloodIcon.style.backgroundImage = new StyleBackground(bloodSprite);
        }

        dimBg.style.display = DisplayStyle.None;
        panelRoot.pickingMode = PickingMode.Ignore;
        built = true;
        return true;
    }

    private void Refresh()
    {
        if (root == null) return;

        PlayerUpgradeService service = PlayerUpgradeService.Instance;
        if (service == null) return;

        if (bloodCount != null)
        {
            bloodCount.text = service.GetBlood().ToString();
        }

        if (summary != null)
        {
            int totalLevels = 0;
            foreach (PlayerUpgradeStat stat in PlayerUpgradeService.AllStats)
            {
                totalLevels += service.GetLevel(stat);
            }

            summary.text =
                $"Суммарный уровень улучшений: {totalLevels}/{PlayerUpgradeService.MaxLevel * PlayerUpgradeService.AllStats.Length}";
        }

        PopulateStats(service);
    }

    private void PopulateStats(PlayerUpgradeService service)
    {
        if (statsList == null) return;
        statsList.contentContainer.Clear();
        clickRouter?.RemoveDead();

        foreach (PlayerUpgradeStat stat in PlayerUpgradeService.AllStats)
        {
            PlayerUpgradeStat captured = stat;
            VisualElement row = BuildStatRow(service, captured);
            statsList.contentContainer.Add(row);
        }
    }

    private VisualElement BuildStatRow(PlayerUpgradeService service, PlayerUpgradeStat stat)
    {
        bool maxed = service.IsMaxed(stat);
        bool affordable = service.CanUpgrade(stat);

        var row = new VisualElement();
        row.AddToClassList("upgrade-row");

        var icon = new VisualElement();
        icon.AddToClassList("upgrade-row__icon");
        Sprite sprite = Resources.Load<Sprite>(PlayerUpgradeService.GetIconResourcePath(stat));
        if (sprite != null) icon.style.backgroundImage = new StyleBackground(sprite);
        row.Add(icon);

        var info = new VisualElement();
        info.AddToClassList("upgrade-row__info");

        var title = new Label(service.GetDisplayName(stat));
        title.AddToClassList("upgrade-row__title");
        info.Add(title);

        var desc = new Label(service.GetDescription(stat));
        desc.AddToClassList("upgrade-row__desc");
        info.Add(desc);

        int level = service.GetLevel(stat);
        var levelLabel = new Label($"Уровень {level}/{PlayerUpgradeService.MaxLevel}");
        levelLabel.AddToClassList("upgrade-row__level");
        info.Add(levelLabel);

        var values = new VisualElement();
        values.AddToClassList("upgrade-row__values");

        var current = new Label(service.GetValueText(stat));
        current.AddToClassList("upgrade-row__current");
        values.Add(current);

        if (!maxed)
        {
            var arrow = new Label("→");
            arrow.AddToClassList("upgrade-row__arrow");
            values.Add(arrow);

            var next = new Label(service.GetNextValueText(stat));
            next.AddToClassList("upgrade-row__next");
            values.Add(next);
        }

        info.Add(values);

        var barTrack = new VisualElement();
        barTrack.AddToClassList("upgrade-row__bar-track");
        var barFill = new VisualElement();
        barFill.AddToClassList("upgrade-row__bar-fill");
        float fill = PlayerUpgradeService.MaxLevel > 0 ? (float)level / PlayerUpgradeService.MaxLevel : 0f;
        barFill.style.width = Length.Percent(Mathf.Clamp01(fill) * 100f);
        barTrack.Add(barFill);
        info.Add(barTrack);

        row.Add(info);

        var action = new VisualElement();
        action.AddToClassList("upgrade-row__action");

        if (maxed)
        {
            var maxLabel = new Label("Макс.");
            maxLabel.AddToClassList("upgrade-row__max");
            action.Add(maxLabel);
        }
        else
        {
            var btn = new VisualElement();
            btn.AddToClassList("upgrade-btn");
            if (!affordable) btn.AddToClassList("upgrade-btn--disabled");

            var btnLabel = new Label("Улучшить");
            btnLabel.AddToClassList("upgrade-btn__label");
            btn.Add(btnLabel);

            var priceRow = new VisualElement();
            priceRow.AddToClassList("upgrade-btn__price-row");

            var price = new Label(service.GetUpgradeCost(stat).ToString());
            price.AddToClassList("upgrade-btn__price");
            priceRow.Add(price);

            var priceIcon = new VisualElement();
            priceIcon.AddToClassList("upgrade-btn__price-icon");
            Sprite bloodSprite = Resources.Load<Sprite>("Game/Items/5.1_krov_orka");
            if (bloodSprite != null) priceIcon.style.backgroundImage = new StyleBackground(bloodSprite);
            priceRow.Add(priceIcon);
            btn.Add(priceRow);

            if (affordable)
            {
                clickRouter.Add(btn, () =>
                {
                    if (service.TryUpgrade(stat))
                    {
                        Refresh();
                    }
                });
            }

            action.Add(btn);
        }

        row.Add(action);
        return row;
    }
}
