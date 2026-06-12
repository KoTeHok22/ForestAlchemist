using System.Collections;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// App UI loading overlay shown during scene transitions.
/// Singleton-ish: a single instance persists across scenes (DontDestroyOnLoad)
/// so the overlay survives the SceneManager.LoadSceneAsync call it drives.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public sealed class LoadingOverlayController : MonoBehaviour
{
    public static LoadingOverlayController Instance { get; private set; }

    private UIDocument document;
    private VisualElement root;
    private VisualElement panelRoot;
    private VisualElement screenLoad;
    private LinearProgress bar;

    private void Awake()
    {
        // One overlay per scene (created by GameOverlayBootstrap). No DontDestroyOnLoad:
        // this component is a child of the overlay prefab, not a root object.
        Instance = this;
    }

    private void OnEnable()
    {
        if (document == null) document = GetComponent<UIDocument>();
        root = document.rootVisualElement;
        if (root == null) return;

        panelRoot = root.Q<VisualElement>("panel-root");
        screenLoad = root.Q<VisualElement>("screen-load");
        bar = root.Q<LinearProgress>("load-bar");
        Hide();
    }

    public void Hide()
    {
        if (screenLoad != null) screenLoad.style.display = DisplayStyle.None;
        // Otherwise the Panel keeps eating all pointer events on top of Pause/HUD.
        if (panelRoot != null) panelRoot.pickingMode = PickingMode.Ignore;
    }

    private void ShowVisual()
    {
        if (screenLoad != null) screenLoad.style.display = DisplayStyle.Flex;
        if (panelRoot != null) panelRoot.pickingMode = PickingMode.Position;
        SetProgress(0f);
    }

    private void SetProgress(float value)
    {
        if (bar != null) bar.value = Mathf.Clamp01(value);
    }

    /// <summary>Show the overlay and load the given scene with a progress bar.</summary>
    public void LoadScene(string sceneName)
    {
        ShowVisual();
        StartCoroutine(LoadRoutine(sceneName));
    }

    private IEnumerator LoadRoutine(string sceneName)
    {
        SetProgress(0f);
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        if (op == null)
        {
            Hide();
            yield break;
        }

        while (!op.isDone)
        {
            SetProgress(Mathf.Clamp01(op.progress / 0.9f));
            yield return null;
        }

        SetProgress(1f);
        Hide();
    }
}
