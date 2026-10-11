using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class FXManager
{
    public const float SceneReadyProgress = 0.9f;

    public SquareTransition square;
    public float transitionDurationOut = 1f;
    public float transitionDurationIn = 1f;
    [Min(0f)] public float revealHoldTimeout = 10f;

    private bool isTransitioning;
    private int revealHolds;

    public bool IsTransitioning => isTransitioning;

    // Scene mới gọi lúc bắt đầu init để giữ màn che, xong thì gọi NotifySceneReady
    public void HoldReveal()
    {
        if (isTransitioning) revealHolds++;
    }

    public void NotifySceneReady()
    {
        if (revealHolds > 0) revealHolds--;
    }

    // Load scene ngầm, chưa activate cho tới khi màn che đóng xong
    public AsyncOperation PreloadScene(string sceneName)
    {
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[SceneTransition] Scene '{sceneName}' chưa có trong Build Profile.");
            return null;
        }

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;
        return operation;
    }

    public bool LoadScene(string sceneName, bool skipOutPhase = false)
    {
        if (IsBusy()) return false;

        AsyncOperation operation = PreloadScene(sceneName);
        if (operation == null) return false;

        TransitionAsync(operation, skipOutPhase).Forget();
        return true;
    }

    public bool LoadScene(AsyncOperation preloaded, bool skipOutPhase = false)
    {
        if (preloaded == null || IsBusy()) return false;

        TransitionAsync(preloaded, skipOutPhase).Forget();
        return true;
    }

    public void PrepareCovered()
    {
        square.Prepare();
        square.SetCovered();
    }

    private bool IsBusy()
    {
        if (!isTransitioning) return false;

        Debug.LogWarning("[SceneTransition] Đang chuyển scene, bỏ qua lệnh LoadScene mới.");
        return true;
    }

    private async UniTaskVoid TransitionAsync(AsyncOperation operation, bool skipOutPhase)
    {
        isTransitioning = true;
        revealHolds = 0;
        var token = destroyCancellationToken;

        try
        {
            square.Prepare();

            // Scene mới load song song trong lúc màn che đang đóng
            if (skipOutPhase)
                square.SetCovered();
            else
                await square.Cover(transitionDurationOut);

            await UniTask.WaitUntil(() => operation.progress >= SceneReadyProgress, cancellationToken: token);

            operation.allowSceneActivation = true;
            await UniTask.WaitUntil(() => operation.isDone, cancellationToken: token);

            square.SetCovered();

            // Chờ 1 frame để Awake/Start của scene mới kịp gọi HoldReveal
            await UniTask.NextFrame(token);
            await WaitRevealHoldsAsync(token);

            await square.Reveal(transitionDurationIn);
        }
        finally
        {
            if (square != null) square.Hide();

            isTransitioning = false;
            revealHolds = 0;
        }
    }

    private async UniTask WaitRevealHoldsAsync(System.Threading.CancellationToken token)
    {
        float elapsed = 0f;

        while (revealHolds > 0)
        {
            if (revealHoldTimeout > 0f && elapsed >= revealHoldTimeout)
            {
                Debug.LogWarning($"[SceneTransition] Scene chưa gọi NotifySceneReady sau {revealHoldTimeout}s, tự mở màn.");
                return;
            }

            await UniTask.NextFrame(token);
            elapsed += Time.unscaledDeltaTime;
        }
    }
}
