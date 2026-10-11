using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class GameManager : ManagerSingleton<GameManager>
{
    [SerializeField] private DataRepo dataRepo;
    [SerializeField] private FXManager fxManager;
    [SerializeField] private AudioManager audioManager;
    public LocalizationManager localizationManager;
    public HeartManager heartManager;
    public CurrencyManager currencyManager;
    [SerializeField] private LoadingBox loadingBox;
    public ToastManager toastManager;

    public bool isSkipOutPhase;
    [Min(0f)] public float loadingDuration = 1f;
    public ThreadPriority loadPriority = ThreadPriority.BelowNormal;

    private AsyncOperationHandle<Sprite> _uiWarmupHandle;


    protected override void OnAwake()
    {
        Init().Forget();
    }
    private async UniTaskVoid Init()
    {
        Application.targetFrameRate = 60;
        loadingBox.Init();

        var token = destroyCancellationToken;
        var initTask = InitManagersAsync().Preserve();
        AsyncOperation operation = null;

        ThreadPriority previousPriority = Application.backgroundLoadingPriority;
        Application.backgroundLoadingPriority = loadPriority;

        try
        {
            float elapsed = 0f;

            while (loadingBox.Progress < 1f)
            {
                await UniTask.NextFrame(token);
                elapsed += Time.unscaledDeltaTime;

                // Init xong mới preload: scene đang giữ ở 90% sẽ chặn các AsyncOperation khác (Addressables) xếp sau nó
                if (operation == null && initTask.Status.IsCompleted())
                {
                    operation = fxManager.PreloadScene(SceneName.GAME_PLAY);
                    if (operation == null) return;
                }

                // Nửa đầu thanh chờ init, nửa sau chờ scene load; không chạy nhanh hơn loadingDuration
                float timeProgress = loadingDuration > 0f ? elapsed / loadingDuration : 1f;
                float loadProgress = operation == null ? 0f : operation.progress / FXManager.SceneReadyProgress;

                loadingBox.SetProgress(Mathf.Min(timeProgress, 0.5f + 0.5f * loadProgress));
            }
        }
        finally
        {
            Application.backgroundLoadingPriority = previousPriority;
        }

        await initTask;

        if (isSkipOutPhase) fxManager.PrepareCovered();
        fxManager.LoadScene(operation, isSkipOutPhase);
    }

    private async UniTask InitManagersAsync()
    {
        await GamePrefs.Init();
        //firebaseSetup.Init();
        //await UniTask.WaitUntil(() => firebaseSetup.IsActiveRemote);
        dataRepo.Init();
        fxManager.Init();
        audioManager.Init();
        heartManager.Init();
        currencyManager.Init();
        toastManager.Init();
        await WarmupUIAddressables();
    }

    private async UniTask WarmupUIAddressables()
    {
        try
        {
            string key = "DummyWarmup";

            _uiWarmupHandle = Addressables.LoadAssetAsync<Sprite>(key);
            await _uiWarmupHandle.Task;

            if (_uiWarmupHandle.Status == AsyncOperationStatus.Succeeded)
                Debug.Log("[GameManager] Đã mount bundle popup (giữ qua dummy)");
            else
                Debug.LogWarning("[GameManager] Mount bundle thất bại; UI có thể delay lần mở đầu!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GameManager] Lỗi khi thực hiện giữ ấm UI: {e.Message}");
        }
    }
    private void OnDestroy()
    {
        if (_uiWarmupHandle.IsValid())
        {
            Addressables.Release(_uiWarmupHandle);
        }
    }
}