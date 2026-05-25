using UnityEngine;

public partial class GameFlow
{
    private void HandleStateEntered(GameState state)
    {
        switch (state)
        {
            case GameState.Playing:
                ShooterController.Instance.Resume();
                Conveyor.Instance.Resume();
                break;
            case GameState.Paused:
                ShooterController.Instance.Pause();
                Conveyor.Instance.Pause();
                break;
            case GameState.Win:
                // show win popup, save progress
                _ = WinBox.Setup(popupHolder, box =>
               {
                   box.Show();
               });
                AudioManager.Instance.PlaySfx("sfx-Win");
                break;
            case GameState.Lose:
                _ = LoseBox.Setup(popupHolder, box =>
                {
                    box.Show();
                });
                AudioManager.Instance.PlaySfx("sfx-Lose");
                break;
            case GameState.BoosterActive:
                // open booster UI
                break;
            case GameState.Tutorial:
                // show tutorial overlay
                break;
        }
    }

    private void HandleStateExited(GameState state)
    {
        switch (state)
        {
            case GameState.Playing:
                break;
            case GameState.Paused:
                // hide pause UI
                break;
            case GameState.BoosterActive:
                // close booster UI
                break;
            case GameState.Tutorial:
                // hide tutorial overlay
                break;
        }
    }
}