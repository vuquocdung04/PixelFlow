public abstract class ShooterState
{
    protected Shooter Owner { get; private set; }

    public virtual void OnEnter(Shooter owner) { Owner = owner; }
    public virtual void OnExit() { }
}

public class IdleState : ShooterState
{
    public override void OnEnter(Shooter owner)
    {
        base.OnEnter(owner);
        owner.currentAnimState = ShooterAnimState.Idle;
    }
}

public class CombatState : ShooterState
{
    public override void OnEnter(Shooter owner)
    {
        base.OnEnter(owner);
        owner.currentAnimState = ShooterAnimState.Combat;
        ShooterController.Instance.RegisterCombat(owner);
    }

    public override void OnExit()
    {
        if (Owner != null)
            ShooterController.Instance.UnregisterCombat(Owner);
    }
}

public class BlockedState : ShooterState
{
    public override void OnEnter(Shooter owner)
    {
        base.OnEnter(owner);
        owner.currentAnimState = ShooterAnimState.Blocked;
    }
}
