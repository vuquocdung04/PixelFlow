using System;
using EventDispatcher;
using Sirenix.OdinInspector;
using UnityEngine;

public enum GameState
{
    Playing = 0,
    Paused = 1,
    Win = 2,
    Lose = 3,
    BoosterActive = 4,
    Tutorial = 5
}

public partial class GameFlow : StaffSingleton<GameFlow>
{
    public GameState CurrentState { get; private set; }
    public event Action<GameState> OnStateEntered;
    public event Action<GameState> OnStateExited;

    private int pauseRequest;

    private Transform popupHolder;
    public override void Init()
    {
        popupHolder = GameScene.GetPopupHolder();
        OnStateEntered += HandleStateEntered;
        OnStateExited += HandleStateExited;

        this.RegisterListener(EventID.LEVEL_COMPLETE, OnLevelComplete);
        this.RegisterListener(EventID.POPUP_OPENED, OnPopupOpened);
        this.RegisterListener(EventID.POPUP_CLOSED, OnPopupClosed);
    }
    protected override void OnDestroy()
    {
        base.OnDestroy();
        OnStateEntered -= HandleStateEntered;
        OnStateExited -= HandleStateExited;

        this.RemoveListener(EventID.LEVEL_COMPLETE, OnLevelComplete);
        this.RemoveListener(EventID.POPUP_OPENED, OnPopupOpened);
        this.RemoveListener(EventID.POPUP_CLOSED, OnPopupClosed);
    }

    public bool ChangeState(GameState next)
    {
        if (CurrentState == next) return false;
        if (!CanTransition(CurrentState, next)) return false;

        var prev = CurrentState;
        OnStateExited?.Invoke(prev);
        CurrentState = next;
        OnStateEntered?.Invoke(next);
        return true;
    }
    private bool CanTransition(GameState from, GameState to)
    {
        switch (from)
        {
            case GameState.Playing: return true;

            case GameState.Paused:
                return to == GameState.Playing ||
                       to == GameState.Win ||
                       to == GameState.Lose ||
                       to == GameState.BoosterActive ||
                       to == GameState.Tutorial;

            case GameState.BoosterActive:
                return to == GameState.Playing || to == GameState.Lose;

            case GameState.Tutorial:
                return to == GameState.Playing || to == GameState.Lose;

            case GameState.Win: return false;
            case GameState.Lose: return false;
        }
        return false;
    }
    private void OnPopupOpened(object _) => RequestPause();
    private void OnPopupClosed(object _) => RequestResume();
    public void RequestPause()
    {
        pauseRequest++;
        if (pauseRequest == 1) ChangeState(GameState.Paused);
    }

    public void RequestResume()
    {
        pauseRequest = Mathf.Max(0, pauseRequest - 1);
        if (pauseRequest == 0 && CurrentState == GameState.Paused)
            ChangeState(GameState.Playing);
    }

    void OnLevelComplete(object _) => ChangeState(GameState.Win);
    public void TriggerLose() => ChangeState(GameState.Lose);
    public void EnterBooster() => ChangeState(GameState.BoosterActive);
    public void ExitBooster() => ChangeState(GameState.Playing);
    public void EnterTutorial() => ChangeState(GameState.Tutorial);
    public void ExitTutorial() => ChangeState(GameState.Playing);

}