using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public partial class FXManager
{
    private bool transitionPlaying;
    public AsyncOperation preparedSceneLoad;
    public string preparedSceneName;
    public Canvas wipeCanvas;
    public float transitionDurationOut = 0.45f;
    public float transitionDurationIn = 0.35f;

    private Material cachedWipeMat;

    private Material WipeMat
    {
        get
        {
            if (cachedWipeMat == null)
                cachedWipeMat = new Material(Resources.Load<Shader>("SquareWipe"));
            wipeCanvas.GetComponentInChildren<Image>(true).material = cachedWipeMat;
            return cachedWipeMat;
        }
    }

    public void LoadSceneWithSquareWipe(string sceneName)
    {
        if (transitionPlaying) return;
        transitionPlaying = true;
        SquareWipeAsync(sceneName).Forget();
    }

    private async UniTaskVoid SquareWipeAsync(string sceneName)
    {
        SetupCanvasCamera();
        SetWipeState(0f, 0f);
        await WipeMat.DOFloat(1f, "_Progress", transitionDurationOut).ToUniTask();

        AsyncOperation asyncLoad = preparedSceneName == sceneName ? preparedSceneLoad : null;
        preparedSceneLoad = null;
        preparedSceneName = null;
        if (asyncLoad == null || asyncLoad.isDone)
            asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        if (asyncLoad == null)
        {
            Debug.LogError($"[FXManager] Could not start loading scene: {sceneName}");
            transitionPlaying = false;
            return;
        }

        if (!asyncLoad.isDone)
            asyncLoad.allowSceneActivation = true;
        while (!asyncLoad.isDone)
        {
            await UniTask.Yield();
        }

        // Scene mới đã load xong -> Cập nhật lại camera mới cho Canvas
        SetupCanvasCamera();
        SetWipeState(1f, 0f);

        // Scene is active and loaded; reveal immediately, as in DrinkPacking.
        await WipeMat.DOFloat(1f, "_Progress", transitionDurationIn).ToUniTask();

        Debug.Log("Completed Transition");
        wipeCanvas.gameObject.SetActive(false);
        transitionPlaying = false;
    }

    public void PrepareWipeClosed()
    {
        SetupCanvasCamera();
        SetWipeState(1f, 0f);
    }

    private void SetupCanvasCamera()
    {
        if (!wipeCanvas.gameObject.activeSelf)
        {
            wipeCanvas.gameObject.SetActive(true);
        }
        GameObject camObj = GameObject.FindGameObjectWithTag("MainCamera");
        if (camObj != null)
        {
            wipeCanvas.worldCamera = camObj.GetComponent<Camera>();
        }
    }

    private void SetWipeState(float isInvert, float radius)
    {
        WipeMat.SetFloat("_IsInvert", isInvert);
        WipeMat.SetFloat("_Progress", radius);
    }
}
