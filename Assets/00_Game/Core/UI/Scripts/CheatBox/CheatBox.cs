using EventDispatcher;
using TMPro;
using UnityEngine.UI;

public class CheatBox : BaseBox<CheatBox>
{
    public Button btnClose;

    public Button btnNextLevel;
    public TMP_InputField inputNextLevel;

    public Button btnWin;
    public Button btnLose;
    public Button btnBuffCoin;
    public TMP_InputField inputBuffCoin;

    protected override void Init()
    {
        btnClose.OnClicked(Close);
        btnNextLevel.OnClicked(OnClickNextLevel);
        btnWin.OnClicked(OnClickWin);
        btnLose.OnClicked(OnClickLose);
        btnBuffCoin.OnClicked(OnClickBuffCoin);
    }

    protected override void InitState()
    {
        inputNextLevel.text = UseProfile.Level.Value.ToString();
        inputBuffCoin.text = "5000";
    }

    private void OnClickNextLevel()
    {
        if (int.TryParse(inputNextLevel.text, out int level))
            UseProfile.Level.Value = level;
        FXManager.Instance.LoadSceneWithIrisWipe(SceneName.GAME_PLAY);
    }

    private void OnClickWin()
    {
        GameFlow.Instance.ChangeState(GameState.Win);
    }

    private void OnClickLose()
    {
        GameFlow.Instance.TriggerLose();
    }

    private void OnClickBuffCoin()
    {
        if (int.TryParse(inputBuffCoin.text, out int amount))
            UseProfile.Coin.Value += amount;

        this.PostEvent(EventID.CHANGE_COIN);
    }
}