
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameScene : StaffSingleton<GameScene>
{
    public Transform popupHolder;
    public Image darkPanel;

    public TextMeshProUGUI txtLevelDisplay;

    [Header("Button")]
    public Button btnSetting;
    public Button btnCoin;
    public override void Init()
    {
        btnSetting.OnClicked(delegate
        {
            _ = SettingGameBox.Setup(popupHolder, box => box.Show());
        });

        btnCoin.OnClicked(delegate
        {
            _ = ShopBox.Setup(popupHolder, box => box.Show());
        });
        txtLevelDisplay.text = $"Level {UseProfile.Level.Value}";
    }
    public static void EnableDarkPanel(bool state)
    {
        Instance.darkPanel.DOKill();
        if (state)
        {
            Instance.darkPanel.gameObject.SetActive(true);

            Color color = Instance.darkPanel.color;
            color.a = 0f;
            Instance.darkPanel.color = color;

            float targetAlpha = 0.9f;
            float fadeDuration = 0.15f;

            Instance.darkPanel.DOFade(targetAlpha, fadeDuration).SetUpdate(true);
        }
        else
        {
            Instance.darkPanel.gameObject.SetActive(false);
        }
    }
    public static Transform GetCoinBar() => Instance.btnCoin.transform;
    public static Transform GetPopupHolder() => Instance.popupHolder;
}
