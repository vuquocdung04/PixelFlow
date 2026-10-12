using System;
using System.Collections.Generic;
using UnityEngine;

// Icon cho từng loại phần thưởng. Đặt trong Resources để mọi scene đều lấy được mà không cần gán tay.
[CreateAssetMenu(fileName = "RewardDataBase", menuName = "PixelFlow/RewardDataBase")]
public class RewardDataBase : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public RewardType type;
        public Sprite icon;
    }

    private const string ResourcePath = "RewardDataBase";

    [SerializeField] private List<Entry> entries = new();

    private static RewardDataBase _instance;
    public static RewardDataBase Instance => _instance != null ? _instance : _instance = Resources.Load<RewardDataBase>(ResourcePath);

    public Sprite GetIcon(RewardType type)
    {
        for (int i = 0; i < entries.Count; i++)
            if (entries[i].type == type) return entries[i].icon;
        return null;
    }

    public static string FormatAmount(GameReward reward) => FormatAmount(reward.type, reward.amount);

    public static string FormatAmount(RewardType type, int amount) => type switch
    {
        RewardType.Coin => NumberFormatter.Format(amount),
        RewardType.UnlimitedHeart => amount >= 60 && amount % 60 == 0 ? $"{amount / 60}h" : $"{amount}m",
        _ => $"x{amount}"
    };
}
