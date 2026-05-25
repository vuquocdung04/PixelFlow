using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class LobbyBox : BaseBox<LobbyBox>
{
    [Header("Level Nodes")]
    [SerializeField] private List<LevelNode> levelNodes;

    [Header("Sprites")]
    [SerializeField] private Sprite mainHardSprite;
    [SerializeField] private Sprite lightHardSprite;


    public Button btnSetting;
    public Button btnAvatar;
    public Button btnNoAds;
    public Button btnPlay;

    protected override void Init()
    {
        var holder = LobbyController.Instance.topCanvas;
        btnSetting.OnClicked(delegate { _ = SettingLobbyBox.Setup(holder, box => { box.Show(); }); });
        btnAvatar.OnClicked(delegate { });
        btnNoAds.OnClicked(delegate { _ = NoAdsBox.Setup(holder, box => { box.Show(); }); });
        btnPlay.OnClicked(delegate
        {
            FXManager.Instance.LoadSceneWithIrisWipe(SceneName.GAME_PLAY);
        });

        RefreshLevels();
    }

    protected override void InitState()
    {
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
}