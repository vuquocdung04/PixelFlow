using Cysharp.Threading.Tasks;
using EventDispatcher;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MoreLivesBox : BaseBox<MoreLivesBox>
{
    public Button btnClose;
    public Button btnCloseByPanel;
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
        btnCloseByPanel.OnClicked(Close);

        btnRefill.OnClicked(delegate
        {
            OnRefillByCoin();
        });

        btnRefillByAds.OnClicked(delegate
        {
           
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

        txtDisplayCooldownLives.BindCountdownRealtime(
            getTimeRemaining: () => HeartManager.Instance.GetTimeToNextHeart(),
            textWhenZero: "Full",
            checkUnlimited: () => UseProfile.IsUnlimitedHeart,
            token: this.GetCancellationTokenOnDestroy()
        ).Forget();
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

        if (!CurrencyManager.Instance.TrySpend(CurrencyType.Coin, cost)) return;

        HeartManager.Instance.TryAddHeart(1);
        AudioManager.Instance.PlaySfx("Reward");
        Close();
    }
}