using UnityEngine;
using UnityEngine.UI;

public class SettingGameBox : BaseBox<SettingGameBox>
{
    public Button btnClose;
    public Button btnReturnHome;
    public Button btnRestart;
    public Button btnCheat;
    public Button btnSound;
    public Button btnMusic;
    public Button btnVib;

    public Image imgSound;
    public Image imgMusic;
    public Image imgVib;

    public Sprite sprOn, sprOff;

    protected override void Init()
    {
        btnClose.OnClicked(Close);

        btnSound.OnClicked(delegate
        {
            AudioManager.Instance.ToggleSound();
            imgSound.SetSprite(UseProfile.OnSound ? sprOn : sprOff);
        });

        btnVib.OnClicked(delegate
        {
            UseProfile.OnVib.Value = !UseProfile.OnVib;
            imgVib.SetSprite(UseProfile.OnVib ? sprOn : sprOff);
        });

        btnMusic.OnClicked(delegate
        {
            AudioManager.Instance.ToggleMusic();
            imgMusic.SetSprite(UseProfile.OnMusic ? sprOn : sprOff);
        });

        btnReturnHome.OnClicked(delegate
        {
            _ = QuitLevelBox.Setup(transform.parent, box => box.SetupAndShow(QuitLevelBox.Mode.Leave));
        });

        btnRestart.OnClicked(delegate
        {
            _ = QuitLevelBox.Setup(transform.parent, box => box.SetupAndShow(QuitLevelBox.Mode.Restart));
        });

        btnCheat.OnClicked(delegate
        {
            _ = CheatBox.Setup(transform.parent, box => box.Show());
        });

        Refresh();
    }

    protected override void InitState()
    {
    }

    private void Refresh()
    {
        imgSound.SetSprite(UseProfile.OnSound ? sprOn : sprOff);
        imgVib.SetSprite(UseProfile.OnVib ? sprOn : sprOff);
        imgMusic.SetSprite(UseProfile.OnMusic ? sprOn : sprOff);
    }
}