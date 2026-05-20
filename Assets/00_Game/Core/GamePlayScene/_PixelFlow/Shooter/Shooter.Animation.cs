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

    public void JumpTo(Vector3 target, TweenCallback onComplete = null)
    {
        var tween = transform.DOJump(target, jumpPower, 1, jumpDuration).SetEase(Ease.OutCubic);
        if (onComplete != null) tween.OnComplete(onComplete);
    }
    public void SetAnimState(ShooterAnimState state)
    {
        ShooterAnimState prev = currentAnimState;

        if (!baseScaleCached)
        {
            baseScale = transform.localScale;
            baseScaleCached = true;
        }

        currentAnimState = state;
        stateTween?.Kill(true);
        transform.localScale = baseScale;

        switch (state)
        {
            case ShooterAnimState.Idle:

                break;
            case ShooterAnimState.Combat:

                break;
            case ShooterAnimState.Blocked:

                break;
        }

        if (state == ShooterAnimState.Combat && prev != ShooterAnimState.Combat)
            ShooterController.Instance.RegisterCombat(this);
        else if (prev == ShooterAnimState.Combat && state != ShooterAnimState.Combat)
            ShooterController.Instance.UnregisterCombat(this);
    }
}