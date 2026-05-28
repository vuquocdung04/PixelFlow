
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

    protected override void OnAwake()
    {
        base.OnAwake();
        Init().Forget();
    }

    private async UniTaskVoid Init()
    {
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

        AudioManager.Instance.PlayMusic("Normal Level Music (Cover) 1");

        await UniTask.WaitForEndOfFrame(this);
        await UniTask.Delay(500);
        FXManager.Instance.isNextSceneReady = true;
        await UniTask.Delay(500);
        gameFlow.RequestResume();
    }
}
