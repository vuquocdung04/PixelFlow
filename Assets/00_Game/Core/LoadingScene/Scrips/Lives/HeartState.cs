using System;

public abstract class HeartState
{
    protected HeartManager Owner;

    public virtual void OnEnter(HeartManager owner) { Owner = owner; }
    public virtual void OnExit() { }

    public abstract void Tick();
    public abstract bool TryUseHeart();
    public abstract void AddUnlimited(int minutes);
    public abstract double GetTimeToNextHeart();
    public abstract TimeSpan GetUnlimitedTimeRemaining();
}