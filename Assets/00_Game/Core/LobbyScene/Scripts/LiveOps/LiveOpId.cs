using System;
using UnityEngine;

public enum LiveOpId
{
    None = 0,
    DailyLogin = 1,
    DailyReward = 2,
}

// Logic riêng của từng liveop: có noti không, đếm ngược bao lâu, mở popup nào.
public static class LiveOps
{
    public static bool HasNoti(LiveOpId id) => id switch
    {
        LiveOpId.DailyLogin => DailyService.LoginCanClaim,
        LiveOpId.DailyReward => DailyService.RewardFreeCanClaim,
        _ => false
    };

    // > 0: hiện đồng hồ đếm ngược trên nút
    public static long SecondsRemaining(LiveOpId id) => id switch
    {
        LiveOpId.DailyReward => DailyService.RewardFreeSecondsRemaining,
        _ => 0L
    };

    public static void Open(LiveOpId id, Transform holder)
    {
        switch (id)
        {
            case LiveOpId.DailyLogin:
                _ = DailyLoginBox.Setup(holder, box => box.Show());
                break;
            case LiveOpId.DailyReward:
                _ = DailyRewardBox.Setup(holder, box => box.Show());
                break;
            default:
                Debug.LogWarning($"[LiveOps] Chưa có popup cho {id}");
                break;
        }
    }
}
