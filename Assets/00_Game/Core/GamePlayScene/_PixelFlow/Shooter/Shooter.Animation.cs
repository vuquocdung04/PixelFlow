using DG.Tweening;
using UnityEngine;

public enum ShooterAnimState { Idle, Combat, Blocked }

public partial class Shooter
{
    [Space(10), Header("Animation")]
    public float jumpPower = 1f;
    public float jumpDuration = 0.5f;
    public float idleScaleAmount = 0.05f;
    public float idleDuration = 1f;
    public float combatScaleAmount = 0.12f;
    public float combatDuration = 0.35f;
    public float wobbleAngle = 15f;
    public float wobbleDuration = 0.3f;

    public ShooterAnimState currentAnimState = ShooterAnimState.Idle;

    Tween stateTween;
    Vector3 baseScale;
    bool baseScaleCached;

    private readonly IdleState _idleState = new IdleState();
    private readonly CombatState _combatState = new CombatState();
    private readonly BlockedState _blockedState = new BlockedState();

    public void JumpTo(Vector3 target, TweenCallback onComplete = null)
    {
        var tween = transform.DOJump(target, jumpPower, 1, jumpDuration).SetEase(Ease.OutCubic);

        if (HasLink)
            tween.OnUpdate(() => RefreshAllLinks());

        if (onComplete != null) tween.OnComplete(onComplete);
    }

    public void SetAnimState(ShooterAnimState state)
    {
        if (!baseScaleCached)
        {
            baseScale = transform.localScale;
            baseScaleCached = true;
        }

        stateTween?.Kill(true);
        transform.localScale = baseScale;

        ShooterState next = state switch
        {
            ShooterAnimState.Idle => _idleState,
            ShooterAnimState.Combat => _combatState,
            ShooterAnimState.Blocked => _blockedState,
            _ => null,
        };

        ChangeState(next);
    }
}
