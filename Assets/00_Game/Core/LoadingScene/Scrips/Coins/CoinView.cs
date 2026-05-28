using System.Threading;
using Cysharp.Threading.Tasks;
using EventDispatcher;
using TMPro;
using UnityEngine;

public class CoinView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI txtCoin;

    private int _displayed;
    private CancellationTokenSource _cts;

    private void OnEnable()
    {
        this.RegisterListener(EventID.CHANGE_COIN, OnCoinChanged);

        _displayed = CurrencyManager.Instance.Get(CurrencyType.Coin);
        txtCoin.text = NumberFormatter.Format(_displayed); 
    }

    private void OnDisable()
    {
        this.RemoveListener(EventID.CHANGE_COIN, OnCoinChanged);
        CancelCurrent();
    }

    private void OnCoinChanged(object _)
    {
        int target = CurrencyManager.Instance.Get(CurrencyType.Coin);
        if (target == _displayed) return;

        CancelCurrent();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());

        int from = _displayed;
        _displayed = target;         

        txtCoin.CountTo(target, from: from, format: NumberFormatter.Format, token: _cts.Token).Forget();
    }

    private void CancelCurrent()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }
}