using System;
using System.Collections;

public interface ISceneLoader
{
    IEnumerator LoadSceneAsync(string sceneName, Action<float> onProgress, Action onReady);
}
