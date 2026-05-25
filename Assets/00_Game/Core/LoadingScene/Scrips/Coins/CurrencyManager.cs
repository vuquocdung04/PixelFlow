using System;
using EventDispatcher;
using UnityEngine;
public enum CurrencyType
{
    Coin,
    Gem
}
public static class NumberFormatter
{
    public static string Format(int value)
    {
        if (value < 10000) return value.ToString();
        if (value < 1000000) return $"{value / 1000f:0.#}K";
        if (value < 1000000000) return $"{value / 1000000f:0.#}M";
        return $"{value / 1000000000f:0.#}B";
    }
}

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    public void Init()
    {
        Instance = this;
    }

    public int Get(CurrencyType type) => GetPref(type).Value;

    public bool TrySpend(CurrencyType type, int amount)
    {
        if (amount <= 0) return true;

        var pref = GetPref(type);
        if (pref.Value < amount)
        {
            ToastManager.Instance.ShowToast($"Not enough {type}");
            return false;
        }

        pref.Value -= amount;
        PostChangeEvent(type);
        return true;
    }

    public void Add(CurrencyType type, int amount)
    {
        if (amount <= 0) return;

        var pref = GetPref(type);
        pref.Value += amount;
        PostChangeEvent(type);
    }

    public bool CanAfford(CurrencyType type, int amount)
    {
        return GetPref(type).Value >= amount;
    }

    private PrefVar<int> GetPref(CurrencyType type) => type switch
    {
        CurrencyType.Coin => UseProfile.Coin,
        //CurrencyType.Gem => UseProfile.Gem,  // thêm vào UseProfile khi cần
        _ => throw new ArgumentException($"Unknown currency: {type}")
    };

    private void PostChangeEvent(CurrencyType type)
    {
        EventID eventId = type switch
        {
            CurrencyType.Coin => EventID.CHANGE_COIN,
            //CurrencyType.Gem => EventID.CHANGE_GEM,  // thêm vào EventID khi cần
            _ => EventID.CHANGE_COIN
        };
        this.PostEvent(eventId);
    }
}