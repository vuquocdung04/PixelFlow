using DG.Tweening;
using UnityEngine;

public enum ShooterAnimState { Idle, Combat, Blocked }

public partial class Shooter
{
    [Space(10), Header("Animation")]
    public float jumpDuration = 0.5f;
    public ShooterAnimState currentAnimState = ShooterAnimState.Idle;
    Tween stateTween;
    Vector3 baseScale;
    bool baseScaleCached;
    private Tween _wobbleTween;
    private readonly IdleState _idleState = new IdleState();
    private readonly CombatState _combatState = new CombatState();
    private readonly BlockedState _blockedState = new BlockedState();

    public void JumpTo(Vector3 target, TweenCallback onComplete = null)
    {
        var tween = transform.DOJump(target, 2f, 1, jumpDuration).SetEase(Ease.OutCubic);

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

    public void PlayRejectWobble()
    {
        if (_wobbleTween != null && _wobbleTween.IsActive() && _wobbleTween.IsPlaying()) return;
        _wobbleTween = transform.DOPunchRotation(new Vector3(0f, 0f, 15f), 0.35f, vibrato: 10, elasticity: 1f);

    }
}
