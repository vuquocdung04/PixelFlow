using System.Collections.Generic;
using EventDispatcher;
using UnityEngine;

public static class RewardService
{
    // Cộng thưởng vào save ngay (không mất thưởng nếu thoát game giữa anim).
    // holdCoinEvent = true: coin đã cộng nhưng chưa bắn CHANGE_COIN, để RewardBox cho coin bay rồi mới đếm số.
    public static void Grant(IReadOnlyList<GameReward> rewards, bool holdCoinEvent = false)
    {
        bool coinChanged = false;
        bool heartChanged = false;

        for (int i = 0; i < rewards.Count; i++)
        {
            var r = rewards[i];
            if (r == null || r.amount <= 0) continue;

            switch (r.type)
            {
                case RewardType.Coin:
                    UseProfile.Coin.Value += r.amount;
                    coinChanged = true;
                    break;
                case RewardType.Booster0: UseProfile.Booster0.Value += r.amount; break;
                case RewardType.Booster1: UseProfile.Booster1.Value += r.amount; break;
                case RewardType.Booster2: UseProfile.Booster2.Value += r.amount; break;
                case RewardType.Heart:
                    // Thưởng được phép vượt max tim
                    if (!UseProfile.IsUnlimitedHeart) UseProfile.Heart.Value += r.amount;
                    heartChanged = true;
                    break;
                case RewardType.UnlimitedHeart:
                    if (HeartManager.Instance != null) HeartManager.Instance.TryAddUnlimited(r.amount);
                    heartChanged = true;
                    break;
            }
        }

        GamePrefs.Save();

        if (coinChanged && !holdCoinEvent) PostCoinChanged();
        if (heartChanged) EventDispatcher.EventDispatcher.Instance.PostEvent(EventID.CHANGE_HEART);
    }

    public static void PostCoinChanged() => EventDispatcher.EventDispatcher.Instance.PostEvent(EventID.CHANGE_COIN);

    // Cộng thưởng ngay (giữ event coin cho RewardBox), trả về list để đưa vào popup sau khi chạy anim.
    public static List<GameReward> GrantForPopup(IReadOnlyList<GameReward> rewards)
    {
        var list = new List<GameReward>(rewards.Count);
        for (int i = 0; i < rewards.Count; i++)
            if (rewards[i] != null && rewards[i].amount > 0) list.Add(rewards[i].Clone());

        if (list.Count > 0) Grant(list, holdCoinEvent: true);
        return list;
    }

    // Cộng thưởng + mở popup nhận thưởng
    public static void Claim(IReadOnlyList<GameReward> rewards, Transform holder)
    {
        var list = GrantForPopup(rewards);
        if (list.Count > 0) RewardBox.ShowRewards(list, holder);
    }
}
