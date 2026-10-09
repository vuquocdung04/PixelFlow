using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System;

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

    public float loadingFadeOutDuration = 0.35f;
    public float minimumLoadingDuration = 3f;
    public ThreadPriority loadingPriority = ThreadPriority.BelowNormal;
    public ThreadPriority finishLoadingPriority = ThreadPriority.Normal;

    protected override void OnAwake()
    {
        Init().Forget();
    }
    private async UniTaskVoid Init()
    {
        Application.targetFrameRate = 60;
        loadingBox.Init();
        // Let Unity render the loading screen before doing initialization work.
        await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
        Application.backgroundLoadingPriority = loadingPriority;
        float loadingStartedAt = Time.realtimeSinceStartup;

        await GamePrefs.Init();
        //firebaseSetup.Init();
        //await UniTask.WaitUntil(() => firebaseSetup.IsActiveRemote);
        dataRepo.Init();
        fxManager.Init();
        audioManager.Init();
        heartManager.Init();
        currencyManager.Init();
        toastManager.Init();
        var sceneLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(SceneName.GAME_PLAY);
        sceneLoad.allowSceneActivation = false;
        fxManager.preparedSceneLoad = sceneLoad;
        fxManager.preparedSceneName = SceneName.GAME_PLAY;
        while (sceneLoad.progress < 0.9f || Time.realtimeSinceStartup - loadingStartedAt < minimumLoadingDuration)
        {
            if (loadingBox == null || loadingBox.fill == null)
                return;

            float realProgress = Mathf.Clamp01(sceneLoad.progress / 0.9f);
            float timedProgress = minimumLoadingDuration > 0f
                ? Mathf.Clamp01((Time.realtimeSinceStartup - loadingStartedAt) / minimumLoadingDuration)
                : 1f;
            loadingBox.fill.fillAmount = Mathf.Max(realProgress, timedProgress);
            await UniTask.Yield();
        }
        if (loadingBox == null || loadingBox.fill == null)
            return;

        loadingBox.fill.fillAmount = 1f;
        // Activate the prepared scene immediately once the bar reaches 100%.
        Application.backgroundLoadingPriority = finishLoadingPriority;
        sceneLoad.allowSceneActivation = true;
        await UniTask.WaitUntil(() => sceneLoad.isDone);
    }


}
