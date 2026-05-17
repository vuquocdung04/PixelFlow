
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
    protected override void OnAwake()
    {
        base.OnAwake();
        //Init();
    }

    private void Init()
    {
        UseProfile.Level.Value  = 6;
        gameScene.Init();
        handAnimation.Init();
        boosterController.Init();
        inputController.Init();
        gameFlow.Init();

        FXManager.Instance.isNextSceneReady = true;
    }
}
