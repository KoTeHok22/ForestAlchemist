using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class AddressableSceneLoader : MonoBehaviour, ISceneLoader
{
    public IEnumerator LoadSceneAsync(string sceneName, Action<float> onProgress, Action onReady)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        while (operation.progress < 0.9f)
        {
            onProgress?.Invoke(operation.progress / 0.9f);
            yield return null;
        }

        onProgress?.Invoke(1f);
        yield return new WaitForSeconds(1f);

        operation.allowSceneActivation = true;
        onReady?.Invoke();
    }
}
