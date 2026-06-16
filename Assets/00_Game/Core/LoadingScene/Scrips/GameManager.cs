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
    public float loadingStepDuration = 1f;
    public float loadingFadeOutDuration = 1f;

    private AsyncOperationHandle<Sprite> _uiWarmupHandle;

    protected override void OnAwake()
    {
        Init().Forget();
    }
    private async UniTaskVoid Init()
    {
        Application.targetFrameRate = 60;
        loadingBox.Init();
        var load50Task = loadingBox.LoadingAsync(0.5f, loadingStepDuration);
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
        await load50Task;
        await loadingBox.LoadingAsync(1f, loadingStepDuration);
        fxManager.PrepareWipeClosed();
        await loadingBox.CloseAsync(loadingFadeOutDuration);

        //Init final
        fxManager.LoadSceneWithIrisWipe(SceneName.GAME_PLAY, isSkipOutPhase);
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