using System.Collections.Generic;
using UnityEngine;

public class ShooterController : StaffSingleton<ShooterController>
{
    private List<Shooter> combatShooters = new List<Shooter>();
    private List<Shooter> aliveShooters = new List<Shooter>();

    public int TotalAlive => aliveShooters.Count;
    public override void Init()
    {

    }
    public void RegisterCombat(Shooter s)
    {
        combatShooters.Add(s);
    }

    public void UnregisterCombat(Shooter s) => combatShooters.Remove(s);
    public void RegisterAlive(Shooter s)
    {
        aliveShooters.Add(s);
    }
    public void UnregisterAlive(Shooter s) => aliveShooters.Remove(s);
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