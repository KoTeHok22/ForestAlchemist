using UnityEngine;
using UnityEngine.UI;

public sealed class GatherProgressDisplay : MonoBehaviour
{
    [SerializeField] private Scrollbar progressBar;

    private void Awake()
    {
        Hide();
    }

    public void Show()
    {
        if (progressBar != null)
            progressBar.gameObject.SetActive(true);
        SetProgress(0f);
    }

    public void Hide()
    {
        if (progressBar != null)
            progressBar.gameObject.SetActive(false);
    }

    public void SetProgress(float value)
    {
        if (progressBar != null)
            progressBar.size = Mathf.Clamp01(value);
    }
}
