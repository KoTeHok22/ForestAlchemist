using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public sealed class ExpeditionResultAppUI : MonoBehaviour
{
    private UIDocument document;
    private VisualElement root;
    private VisualElement panelRoot;
    private VisualElement dimBg;
    private Label title;
    private Label subtitle;
    private Label body;
    private VisualElement btnClose;
    private AppUIClickRouter clickRouter;

    private void Awake()
    {
        document = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        if (document == null) document = GetComponent<UIDocument>();
        root = document.rootVisualElement;
        if (root == null) return;
        panelRoot = root.Q<VisualElement>("panel-root");
        dimBg = root.Q<VisualElement>("dim-bg");
        title = root.Q<Label>("title");
        subtitle = root.Q<Label>("subtitle");
        body = root.Q<Label>("body");
        clickRouter = new AppUIClickRouter(root);
        btnClose = root.Q<VisualElement>("btn-close");
        if (btnClose != null) clickRouter.Add(btnClose, Hide);
        var btnCloseX = root.Q<VisualElement>("btn-close-x");
        if (btnCloseX != null) clickRouter.Add(btnCloseX, Hide);
        Hide();

        if (ExpeditionManager.Instance != null)
        {
            ExpeditionManager.Instance.OnExpeditionEnded += ShowResult;
            if (ExpeditionManager.Instance.TryConsumePendingResult(out ExpeditionResult pending))
                ShowResult(pending);
        }
    }

    private void OnDisable()
    {
        if (ExpeditionManager.Instance != null)
            ExpeditionManager.Instance.OnExpeditionEnded -= ShowResult;
    }

    public void ShowResult(ExpeditionResult result)
    {
        if (root == null) return;
        if (dimBg != null) dimBg.style.display = DisplayStyle.Flex;
        if (panelRoot != null) panelRoot.pickingMode = PickingMode.Position;
        ExpeditionStats stats = ExpeditionManager.Instance.GetLastResultStatsSnapshot();

        string titleText;
        switch (result)
        {
            case ExpeditionResult.Success:  titleText = "Экспедиция успешно завершена"; break;
            case ExpeditionResult.Death:    titleText = "Экспедиция провалена: алхимик погиб"; break;
            case ExpeditionResult.Abandoned: titleText = "Экспедиция прервана"; break;
            default: titleText = "Экспедиция завершена"; break;
        }

        if (title != null)
        {
            title.text = titleText;
            title.style.color = result == ExpeditionResult.Success
                ? new StyleColor(new Color(0.18f, 0.42f, 0.16f, 1f))
                : new StyleColor(new Color(0.54f, 0.16f, 0.10f, 1f));
        }
        if (subtitle != null) subtitle.text = BuildMilestoneText(result, stats);
        if (body != null) body.text = $"Успешных походов: {stats.successfulExpeditions}\nСмертей: {stats.totalDeaths}\nМаксимальная угроза: {stats.deepestThreatReached}";
    }

    private static string BuildMilestoneText(ExpeditionResult result, ExpeditionStats stats)
    {
        if (result == ExpeditionResult.Success && stats.successfulExpeditions == 1)
            return "Первый успешный поход завершён.";
        if (result == ExpeditionResult.Success && stats.successfulExpeditions == 3)
            return "Ты закрепился в лесу. Домашняя база начинает развиваться быстрее.";
        if (result == ExpeditionResult.Death && stats.totalDeaths == 1)
            return "Первая потеря напомнила: жадность в лесу опасна.";
        if (stats.deepestThreatReached >= 5)
            return "Лес ответил силой. Орки стали гораздо опаснее.";
        return "Каждый поход меняет лес и укрепляет базу дома.";
    }

    private void Hide()
    {
        if (dimBg != null) dimBg.style.display = DisplayStyle.None;
        // Otherwise the Panel keeps eating all pointer events on top of Pause/HUD.
        if (panelRoot != null) panelRoot.pickingMode = PickingMode.Ignore;
    }
}
