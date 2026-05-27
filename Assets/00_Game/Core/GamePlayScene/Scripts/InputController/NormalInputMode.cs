using EventDispatcher;
using UnityEngine;

public class NormalInputMode : InputMode
{
    public override void HandleClick(RaycastHit hit)
    {
        Shooter shooter = hit.collider.GetComponentInParent<Shooter>();
        if (shooter == null) return;

        if (Conveyor.Instance.IsGroupStaggering)
        {
            return;
        }

        if (shooter.IsInGroup)
        {
            HandleGroupClick(shooter, shooter.linkGroup);
            return;
        }

        if (shooter.IsBlocked)
        {
            shooter.PlayRejectWobble();
            return;
        }

        if (Conveyor.Instance.itemSlots.Count == 0)
        {
            Controller.PostEvent(EventID.CONVEYOR_NOT_ENOUGH_SLOT);
            return;
        }

        Conveyor.Instance.TakeFirstSlot(shooter);
        LevelController.Instance.OnShooterTaken(shooter);
    }

    private void HandleGroupClick(Shooter clicked, LinkGroup group)
    {
        int gridX = LevelController.Instance.gridBottomX;

        if (!group.CanClick(clicked, gridX))
        {
            clicked.PlayRejectWobble();
            return;
        }

        if (Conveyor.Instance.itemSlots.Count < group.Count)
        {
            Controller.PostEvent(EventID.CONVEYOR_NOT_ENOUGH_SLOT);
            return;
        }

        var takenShooters = new System.Collections.Generic.List<Shooter>(group.members);

        Conveyor.Instance.TakeFirstSlotsForGroup(group);

        for (int i = 0; i < takenShooters.Count; i++)
        {
            Shooter captured = takenShooters[i];
            float delay = i * 0.2f;

            if (delay > 0f)
                DG.Tweening.DOVirtual.DelayedCall(delay, () => LevelController.Instance.OnShooterTaken(captured));
            else
                LevelController.Instance.OnShooterTaken(captured);
        }
    }
}