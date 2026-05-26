using System.Collections.Generic;
using EventDispatcher;
using UnityEngine;
using UnityEngine.UI;

public class LobbyBox : BaseBox<LobbyBox>
{
    [Header("Level Nodes")]
    [SerializeField] private List<LevelNode> levelNodes;

    [Header("Sprites")]
    [SerializeField] private Sprite mainHardSprite;
    [SerializeField] private Sprite lightHardSprite;

    [Header("Buttons")]
    public Button btnSetting;
    public Button btnAvatar;
    public Button btnNoAds;
    public Button btnPlay;

    [Header("Avatar")]
    public Image iconAvatar;

    protected override void Init()
    {
        var holder = LobbyController.Instance.topCanvas;
        btnSetting.OnClicked(delegate { _ = SettingLobbyBox.Setup(holder, box => box.Show()); });
        btnAvatar.OnClicked(delegate { _ = AvatarBox.Setup(holder, box => box.Show()); });
        btnNoAds.OnClicked(delegate { _ = NoAdsBox.Setup(holder, box => box.Show()); });
        btnPlay.OnClicked(delegate { FXManager.Instance.LoadSceneWithIrisWipe(SceneName.GAME_PLAY); });

        this.RegisterListener(EventID.CHANGE_AVATAR, OnAvatarChanged);

        RefreshLevels();
        RefreshAvatar();
    }

    protected override void InitState()
    {
    }

    private void OnAvatarChanged(object param) => RefreshAvatar();

    private void RefreshAvatar()
    {
        iconAvatar.sprite = DataRepo.Instance.avatarData.GetSpriteById(UseProfile.AvatarId.Value);
    }

    private void RefreshLevels()
    {
        int currentLevel = UseProfile.Level.Value;

        for (int i = 0; i < levelNodes.Count; i++)
        {
            int level = currentLevel + i;
            bool isHard = level % 3 == 0;

            levelNodes[i].Setup(level, isHard, mainHardSprite, lightHardSprite);
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        this.RemoveListener(EventID.CHANGE_AVATAR, OnAvatarChanged);
    }
}