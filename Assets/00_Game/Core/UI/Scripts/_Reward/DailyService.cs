using System;
using System.Globalization;
using EventDispatcher;

// Trạng thái Daily Login + Daily Reward. UI (box, nút liveop) chỉ đọc từ đây.
public static class DailyService
{
    private static string Today => DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
    private static long Now => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    private static long NextMidnight => new DateTimeOffset(DateTime.Today.AddDays(1)).ToUnixTimeSeconds();

    public static void NotifyChanged()
    {
        if (EventDispatcher.EventDispatcher.HasInstance())
            EventDispatcher.EventDispatcher.Instance.PostEvent(EventID.DAILY_CHANGED);
    }

    // ================= DAILY LOGIN =================

    public static bool LoginCanClaim => UseProfile.DailyLoginClaimDate.Value != Today;

    // Ngày hiện tại trong chuỗi (0-based). Hết chuỗi thì quay lại ngày 1 khi sang ngày mới.
    public static int LoginDay(int totalDays)
    {
        int day = UseProfile.DailyLoginDay.Value;
        if (LoginCanClaim && totalDays > 0 && day >= totalDays)
        {
            day = 0;
            UseProfile.DailyLoginDay.Value = 0;
        }
        return day;
    }

    public static DailyState LoginState(int index, int totalDays)
    {
        int day = LoginDay(totalDays);
        if (index < day) return DailyState.Claimed;
        if (index == day && LoginCanClaim) return DailyState.Unlock;
        return DailyState.Lock;
    }

    public static bool TryClaimLogin(int index, int totalDays)
    {
        if (!LoginCanClaim || index != LoginDay(totalDays)) return false;

        UseProfile.DailyLoginDay.Value = index + 1;
        UseProfile.DailyLoginClaimDate.Value = Today;
        NotifyChanged();
        return true;
    }

    // ================= DAILY REWARD =================
    // Slot 0: free, mở lại lúc 0h hằng ngày. Slot 1..n: xem quảng cáo, mở lần lượt sau khi đã nhận slot free.

    private static void CheckRewardNewDay()
    {
        if (UseProfile.DailyRewardDate.Value == Today) return;
        UseProfile.DailyRewardDate.Value = Today;
        UseProfile.DailyRewardProgress.Value = 0;
    }

    public static long RewardFreeSecondsRemaining => Math.Max(0L, UseProfile.DailyRewardNextFree.Value - Now);
    public static bool RewardFreeCanClaim => RewardFreeSecondsRemaining <= 0L;

    // Số slot quảng cáo đã nhận hôm nay
    public static int RewardProgress
    {
        get
        {
            CheckRewardNewDay();
            return UseProfile.DailyRewardProgress.Value;
        }
    }

    public static bool RewardSlotClaimed(int index) => index == 0 ? !RewardFreeCanClaim : index <= RewardProgress;
    public static bool RewardSlotUnlocked(int index) => index == 0 || (!RewardFreeCanClaim && index <= RewardProgress + 1);
    public static bool RewardSlotCanClaim(int index) => RewardSlotUnlocked(index) && !RewardSlotClaimed(index);

    public static int RewardClaimedSteps(int totalSlots)
    {
        int steps = RewardProgress + (RewardFreeCanClaim ? 0 : 1);
        return Math.Min(steps, totalSlots);
    }

    public static bool TryClaimReward(int index)
    {
        if (!RewardSlotCanClaim(index)) return false;

        if (index == 0) UseProfile.DailyRewardNextFree.Value = NextMidnight;
        else UseProfile.DailyRewardProgress.Value = index;

        NotifyChanged();
        return true;
    }

    // ================= CHEAT =================

    public static void CheatNextDay()
    {
        string yesterday = DateTime.Now.AddDays(-1).ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        if (UseProfile.DailyLoginClaimDate.Value == Today) UseProfile.DailyLoginClaimDate.Value = yesterday;
        UseProfile.DailyRewardDate.Value = yesterday;
        UseProfile.DailyRewardNextFree.Value = 0L;
        NotifyChanged();
    }
}

public enum DailyState
{
    Lock = 0,
    Unlock = 1,
    Claimed = 2
}
