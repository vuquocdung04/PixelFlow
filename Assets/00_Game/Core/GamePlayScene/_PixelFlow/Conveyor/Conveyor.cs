using EventDispatcher;
using UnityEngine;

public partial class Conveyor : StaffSingleton<Conveyor>
{
    private ConveyorArrows conveyorArrows;
    public override void Init()
    {
        SpawnItems();
        conveyorArrows = GetComponentInChildren<ConveyorArrows>();
        conveyorArrows.Init();
        this.RegisterListener(EventID.LOOP_MODE_ENTERED, OnLoopEntered);
         this.RegisterListener(EventID.CONVEYOR_NOT_ENOUGH_SLOT, OnNotEnoughSlot);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        this.RemoveListener(EventID.LOOP_MODE_ENTERED, OnLoopEntered);
         this.RemoveListener(EventID.CONVEYOR_NOT_ENOUGH_SLOT, OnNotEnoughSlot);
    }

    public void Pause()
    {
        var allSlots = GetComponentsInChildren<ItemSlot>();
        foreach (var s in allSlots) s.Pause();
        conveyorArrows.Pause();
    }

    public void Resume()
    {
        var allSlots = GetComponentsInChildren<ItemSlot>();
        foreach (var s in allSlots) s.Resume();
        conveyorArrows.Resume();
    }
}