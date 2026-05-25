using System;
using UnityEngine;

public class NormalHeartState : HeartState
{
    public override void OnEnter(HeartManager owner)
    {
        base.OnEnter(owner);
        Owner.RefillOfflineHearts();
    }

    public override void Tick()
    {
        Owner.RefillOfflineHearts();
    }

    public override bool TryUseHeart()
    {
        Owner.RefillOfflineHearts();

        if (UseProfile.Heart <= 0)
        {
            Debug.Log("Không còn tim");
            return false;
        }

        bool wasAtMax = UseProfile.Heart == Owner.MaxHearts;
        UseProfile.Heart.Value -= 1;

        if (wasAtMax)
            UseProfile.TimeLastOverHeart = TimeManager.GetCurrentTime();

        return true;
    }

    public override void AddUnlimited(int minutes)
    {
        DateTime endTime = TimeManager.GetCurrentTime().AddMinutes(minutes);
        UseProfile.IsUnlimitedHeart.Value = true;
        UseProfile.TimeUnlimitedHeart = endTime;

        Owner.SwitchToUnlimited();
        Debug.Log($"Kích hoạt {minutes} phút unlimited. Hết hạn: {endTime}");
    }

    public override double GetTimeToNextHeart()
    {
        if (UseProfile.Heart >= Owner.MaxHearts) return 0;

        TimeSpan elapsed = TimeManager.GetCurrentTime() - UseProfile.TimeLastOverHeart;
        return Math.Max(0, Owner.RefillSeconds - elapsed.TotalSeconds);
    }

    public override TimeSpan GetUnlimitedTimeRemaining() => TimeSpan.Zero;
}