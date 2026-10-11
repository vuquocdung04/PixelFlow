
using Cysharp.Threading.Tasks;
using EventDispatcher;
using UnityEngine;

public class GamePlayController : LeaderSingleton<GamePlayController>
{
    public Camera cameraUI;
    public Camera cameraGameplay;
    public GameScene gameScene;
    public BoosterController boosterController;
    public HandAnimation handAnimation;
    public GameFlow gameFlow;
    public InputController inputController;

    public WaitAreaController waitAreaController;
    public Conveyor conveyor;
    public LevelController levelController;
    public ShooterController shooterController;
    public BrickGrid brickGrid;

    public LevelIntro levelIntro = new LevelIntro();

    protected override void OnAwake()
    {
        base.OnAwake();
        Init().Forget();
    }

    private async UniTaskVoid Init()
    {
        FXManager.Instance.HoldReveal();
        gameScene.Init();
        handAnimation.Init();
        boosterController.Init();
        inputController.Init();
        gameFlow.Init();
        levelController.Init();
        brickGrid.Init();
        conveyor.Init();
        waitAreaController.Init();

        gameFlow.RequestPause();
        levelIntro.Prepare(levelController);

        AudioManager.Instance.PlayMusic("Normal Level Music (Cover) 1");

        await UniTask.WaitForEndOfFrame(this);
        FXManager.Instance.NotifySceneReady();

        // Load ngầm các popup trong trận để lúc mở/thắng/thua không bị delay
        UniTask.WhenAll(
            SettingGameBox.Preload(),
            QuitLevelBox.Preload(),
            WinBox.Preload(),
            LoseBox.Preload(),
            ShopBox.Preload(),
            BuyBoosterBox.Preload(),
            BoosterUnlockBox.Preload()).Forget();

        await levelIntro.Play(gameScene.txtLevelDisplay);
        if (LevelController.IsHardLevel(UseProfile.Level.Value))
            await HardLevelFx.Play(gameScene.popupHolder);
        gameFlow.RequestResume();
    }
}
