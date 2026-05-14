using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class InstructionsManager : MonoBehaviour
{
    public Button continueButton;
    public string nextSceneName = "MainMenu";

    private void Awake()
    {
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinuePressed);
        }
    }

    private void OnContinuePressed()
    {
        SceneManager.LoadScene(nextSceneName);
    }
}
