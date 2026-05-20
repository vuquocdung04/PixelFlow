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
        if (_loopEventPosted) return;
        if (aliveCount > conveyorCapacity) return;

        _loopEventPosted = true;
        this.PostEvent(EventID.LOOP_MODE_ENTERED);
    }


    void Update()
    {
        for (int i = combatShooters.Count - 1; i >= 0; i--)
        {
            var s = combatShooters[i];
            if (s == null) { combatShooters.RemoveAt(i); continue; }
            s.TickCombat();
        }
    }


}