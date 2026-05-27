
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
        Init();
    }

    private void Init()
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

        AudioManager.Instance.PlayMusic("Normal Level Music (Cover) 1");
        FXManager.Instance.isNextSceneReady = true;
    }
}
