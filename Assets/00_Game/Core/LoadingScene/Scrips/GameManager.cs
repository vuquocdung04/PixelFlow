using Cysharp.Threading.Tasks;
using UnityEngine;

public class GameManager : ManagerSingleton<GameManager>
{
    [SerializeField] private DataRepo dataRepo;
    [SerializeField] private FXManager fxManager;
    [SerializeField] private AudioManager audioManager;
    public LocalizationManager localizationManager;
    public LivesManager livesManager;
    [SerializeField] private LoadingBox loadingBox;
    public ToastManager toastManager;
    
    public bool isSkipOutPhase;
    public float loadingStepDuration = 1f;
    public float loadingFadeOutDuration = 1f;
    
    protected override void OnAwake()
    {
        Init().Forget();
    } 
    private async UniTaskVoid Init()
    {
        UseProfile.Heart.Value = 4;
        Application.targetFrameRate = 60;
        loadingBox.Init();
        
        var load50Task = loadingBox.LoadingAsync(0.5f, loadingStepDuration);
        //firebaseSetup.Init();
        //await UniTask.WaitUntil(() => firebaseSetup.IsActiveRemote);
        dataRepo.Init();
        fxManager.Init();
        audioManager.Init();
        livesManager.Init();
        toastManager.Init();
        await load50Task;
        await loadingBox.LoadingAsync(1f, loadingStepDuration);
        fxManager.PrepareWipeClosed();
        await loadingBox.CloseAsync(loadingFadeOutDuration);
        
        //Init final
        fxManager.LoadSceneWithIrisWipe(SceneName.LOBBY_SCENE, isSkipOutPhase);
    }
}