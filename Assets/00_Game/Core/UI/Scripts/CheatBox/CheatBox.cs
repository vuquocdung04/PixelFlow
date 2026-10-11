using EventDispatcher;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CheatBox : BaseBox<CheatBox>
{
    public Button btnClose;

    public Button btnPrevLevel;
    public Button btnNextLevel;
    public TMP_Text txtPrevLevel;
    public TMP_Text txtNextLevel;

    public Button btnGoToLevel;
    public TMP_InputField inputLevel;

    public Button btnWin;
    public Button btnLose;
    public Button btnBuffCoin;
    public TMP_InputField inputBuffCoin;

    protected override void Init()
    {
        btnClose.OnClicked(Close);
        btnPrevLevel.OnClicked(() => LoadLevel(UseProfile.Level.Value - 1));
        btnNextLevel.OnClicked(() => LoadLevel(UseProfile.Level.Value + 1));
        btnGoToLevel.OnClicked(OnClickGoToLevel);
        inputLevel.onSubmit.AddListener(_ => OnClickGoToLevel());
        btnWin.OnClicked(OnClickWin);
        btnLose.OnClicked(OnClickLose);
        btnBuffCoin.OnClicked(OnClickBuffCoin);
    }

    protected override void InitState()
    {
        int level = UseProfile.Level.Value;
        inputLevel.text = level.ToString();
        inputBuffCoin.text = "5000";

        btnPrevLevel.interactable = level > 1;
        btnNextLevel.interactable = level < LevelController.MaxLevel;
        if (txtPrevLevel != null) txtPrevLevel.text = $"< Lv {Mathf.Max(1, level - 1)}";
        if (txtNextLevel != null) txtNextLevel.text = $"Lv {Mathf.Min(LevelController.MaxLevel, level + 1)} >";
    }

    private void OnClickGoToLevel()
    {
        if (int.TryParse(inputLevel.text, out int level))
            LoadLevel(level);
    }

    private void LoadLevel(int level)
    {
        UseProfile.Level.Value = Mathf.Clamp(level, 1, LevelController.MaxLevel);
        Close();
        FXManager.Instance.LoadScene(SceneName.GAME_PLAY);
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
