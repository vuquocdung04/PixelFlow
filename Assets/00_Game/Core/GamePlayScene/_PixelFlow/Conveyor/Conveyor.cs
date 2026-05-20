using EventDispatcher;
using UnityEngine;

public partial class Conveyor : StaffSingleton<Conveyor>
{
    public override void Init()
    {
        GetComponentInChildren<ConveyorArrows>().Init();
        SpawnItems();
        this.RegisterListener(EventID.LOOP_MODE_ENTERED, OnLoopEntered);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        this.RemoveListener(EventID.LOOP_MODE_ENTERED, OnLoopEntered);
    }
}