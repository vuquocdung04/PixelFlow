using System.Collections.Generic;
using EventDispatcher;
using UnityEngine;

public class ShooterController : StaffSingleton<ShooterController>
{
    private List<Shooter> combatShooters = new List<Shooter>();
    public int aliveCount;
    public int conveyorCapacity;
    private bool _loopEventPosted = false;
    public bool IsInLoopMode => _loopEventPosted;

    private bool _paused = false;

    public void Pause() => _paused = true;
    public void Resume() => _paused = false;
    public override void Init()
    {

    }
    public void RegisterCombat(Shooter s)
    {
        combatShooters.Add(s);
    }

    public void UnregisterCombat(Shooter s) => combatShooters.Remove(s);

    public void OnShooterDespawn()
    {
        aliveCount--;
        TryPostLoopEvent();
    }
    public void TryPostLoopEvent()
    {
        if (_loopEventPosted)
        {
            Debug.Log("[ShooterController] Loop event already posted, skip");
            return;
        }
        if (aliveCount > conveyorCapacity)
        {
            Debug.Log($"[ShooterController] Not yet — aliveCount {aliveCount} > capacity {conveyorCapacity}");
            return;
        }

        _loopEventPosted = true;
        Debug.Log("[ShooterController] LOOP_MODE_ENTERED posted!");
        this.PostEvent(EventID.LOOP_MODE_ENTERED);
    }


    void Update()
    {
        if (_paused) return;

        for (int i = combatShooters.Count - 1; i >= 0; i--)
        {
            var s = combatShooters[i];
            if (s == null) { combatShooters.RemoveAt(i); continue; }
            s.TickCombat();
        }
    }


}