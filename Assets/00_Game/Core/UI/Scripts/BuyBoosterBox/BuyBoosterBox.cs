using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuyBoosterBox : BaseBox<BuyBoosterBox>
{
    public Button btnClose;
    public Button btnBuyByCoin;
    public TextMeshProUGUI txtTitle;
    public TextMeshProUGUI textDes;
    public TextMeshProUGUI txtPrice;
    public Image icon;

    [Header("Buy Config")]
    [SerializeField] private int pricePerBooster = 0;
    [SerializeField] private int amountPerPurchase = 1;

    private BoosterType _currentType;

    protected override void Init()
    {
        btnClose.OnClicked(Close);
        btnBuyByCoin.OnClicked(OnBuyClicked);
    }

    protected override void InitState()
    {
    }

    public void SetupAndShow(BoosterType type)
    {
        _currentType = type;

        var item = BoosterController.Instance.FindItem(type);
        if (item != null) icon.sprite = item.IconSprite;
        //icon.FitToTargetHeight(381.59f);
        txtTitle.text = GetTitle(type);
        textDes.text = GetDescription(type);
        txtPrice.text = pricePerBooster.ToString();

        Show();
    }

    private void OnBuyClicked()
    {
        int coin = UseProfile.Coin.Value;
        if (coin < pricePerBooster)
        {
            ToastManager.Instance.ShowToast("Not enough coin!");
            return;
        }
        AudioManager.Instance.PlaySfx("sfx-RewardGiftbox");
        UseProfile.Coin.Value = coin - pricePerBooster;
        AddBoosterQuantity(_currentType, amountPerPurchase);

        var item = BoosterController.Instance.FindItem(_currentType);
        if (item != null) item.RefreshFromQuantity();

        Close();
    }

    private void AddBoosterQuantity(BoosterType type, int amount)
    {
        switch (type)
        {
            case BoosterType.Booster0: UseProfile.Booster0.Value += amount; break;
            case BoosterType.Booster1: UseProfile.Booster1.Value += amount; break;
            case BoosterType.Booster2: UseProfile.Booster2.Value += amount; break;
        }
    }

    private string GetTitle(BoosterType type) => type switch
    {
        BoosterType.Booster0 => "Hand",
        BoosterType.Booster1 => "Shuffle",
        BoosterType.Booster2 => "Super Pig",
        _ => ""
    };

    private string GetDescription(BoosterType type) => type switch
    {
        BoosterType.Booster0 => "Pick any pig or item in the queue",
        BoosterType.Booster1 => "Shuffle the pigs in queues",
        BoosterType.Booster2 => "Select a color to shoot with super powers",
        _ => ""
    };
}