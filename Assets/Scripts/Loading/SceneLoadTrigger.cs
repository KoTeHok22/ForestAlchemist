using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class SceneLoadTrigger : MonoBehaviour
{
    [SerializeField] private string sceneToLoad = "Level";
    [SerializeField] private LoadingPanelView loadingView;
    [SerializeField] private AddressableSceneLoader sceneLoader;

    private bool isLoading;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isLoading)
            return;

        if (!other.CompareTag("Player"))
            return;

        isLoading = true;
        loadingView.Show();
        StartCoroutine(sceneLoader.LoadSceneAsync(
            sceneToLoad,
            progress => loadingView.SetProgress(progress),
            () => loadingView.Hide()
        ));
    }
}
