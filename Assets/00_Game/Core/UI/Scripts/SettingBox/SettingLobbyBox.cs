using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class SettingLobbyBox : BaseBox<SettingLobbyBox>
{
    public Button btnClose;
    public Button btnCloseByPanel;

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
        btnCloseByPanel.OnClicked(Close);

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