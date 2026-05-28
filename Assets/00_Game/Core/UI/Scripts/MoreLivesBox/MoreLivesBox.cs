using Cysharp.Threading.Tasks;
using EventDispatcher;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MoreLivesBox : BaseBox<MoreLivesBox>
{
    public Button btnClose;
    public TextMeshProUGUI txtDisplayLives;
    public TextMeshProUGUI txtDisplayCooldownLives;
    public Button btnRefill;
    public Button btnRefillByAds;
    public TextMeshProUGUI txtDisplayCoin;

    private int cost;

    protected override void Init()
    {
        cost = 900;
        btnClose.OnClicked(Close);
        btnRefill.OnClicked(delegate
        {
            OnRefillByCoin();
        });

        btnRefillByAds.OnClicked(delegate
        {
            ToastManager.Instance.ShowToast("Coming soon");
        });
        txtDisplayCoin.text = cost.ToString();
        this.RegisterListener(EventID.CHANGE_HEART, UpdateHeartUI);
    }

    protected override void InitState()
    {
        Refresh();
    }

    private void Refresh()
    {
        UpdateHeartUI(null);
        txtDisplayCooldownLives.BindHeartTimer(this);
    }

    private void UpdateHeartUI(object param)
    {
        txtDisplayLives.text = HeartManager.Instance.CurrentHeart.ToString();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        this.RemoveListener(EventID.CHANGE_HEART, UpdateHeartUI);
    }
    private void OnRefillByCoin()
    {
        if (HeartManager.Instance.IsFull || HeartManager.Instance.IsUnlimited)
        {
            ToastManager.Instance.ShowToast("Heart is full");
            return;
        }

        if (!CurrencyManager.Instance.CanAfford(CurrencyType.Coin, cost))
        {
            HandleNotEnoughCoin();
            return;
        }
        CurrencyManager.Instance.TrySpend(CurrencyType.Coin, cost);
        HeartManager.Instance.TryAddHeart(1);
        AudioManager.Instance.PlaySfx("sfx-RewardGiftbox");
        this.PostEvent(EventID.CHANGE_HEART);
        this.PostEvent(EventID.CHANGE_COIN);
        Close();
    }

    private void HandleNotEnoughCoin()
    {
        SceneUtils.ExecuteInScene(SceneName.LOBBY_SCENE, () =>
        {
            Close();
            NavController.Instance.NavigateTo(ENavType.Shop);
        });

        SceneUtils.ExecuteInScene(SceneName.GAME_PLAY, () =>
        {
            _ = ShopBox.Setup(GameScene.GetPopupHolder(), box => box.Show(BoxAnimationFactory.NoAnim));
        });
    }
}