using EventDispatcher;
using TMPro;
using UnityEngine;

public class CoinView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI txtCoin;

    private void OnEnable()
    {
        this.RegisterListener(EventID.CHANGE_COIN, OnCoinChanged);
        UpdateUI();
    }

    private void OnDisable()
    {
        this.RemoveListener(EventID.CHANGE_COIN, OnCoinChanged);
    }

    private void OnCoinChanged(object _) => UpdateUI();

    private void UpdateUI()
    {
        txtCoin.text = NumberFormatter.Format(CurrencyManager.Instance.Get(CurrencyType.Coin));
    }
}