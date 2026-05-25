using System;
using UnityEngine;

public class UnlimitedHeartState : HeartState
{
    public override void Tick()
    {
        if (GetUnlimitedTimeRemaining().TotalSeconds <= 0)
        {
            UseProfile.IsUnlimitedHeart.Value = false;
            Owner.SwitchToNormal();
        }
    }

    public override bool TryUseHeart()
    {
        Debug.Log("Sử dụng tim (Unlimited)");
        return true;
    }

    public override void AddUnlimited(int minutes)
    {
        DateTime endTime = UseProfile.TimeUnlimitedHeart.AddMinutes(minutes);
        UseProfile.TimeUnlimitedHeart = endTime;
        Debug.Log($"Cộng dồn {minutes} phút unlimited. Hết hạn: {endTime}");
    }

    public override double GetTimeToNextHeart() => 0;

    public override TimeSpan GetUnlimitedTimeRemaining()
    {
        var remain = UseProfile.TimeUnlimitedHeart - TimeManager.GetCurrentTime();
        return remain.TotalSeconds > 0 ? remain : TimeSpan.Zero;
    }
}