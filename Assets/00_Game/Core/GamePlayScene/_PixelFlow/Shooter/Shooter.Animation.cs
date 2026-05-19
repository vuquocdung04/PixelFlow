using DG.Tweening;
using UnityEngine;

public partial class Shooter
{
    public enum AnimState { Idle, Combat, Blocked }

    [Space(10), Header("Animation")]
    public float jumpPower = 1f;
    public float jumpDuration = 0.5f;
    public float idleScaleAmount = 0.05f;
    public float idleDuration = 1f;
    public float combatScaleAmount = 0.12f;
    public float combatDuration = 0.35f;
    public float wobbleAngle = 15f;
    public float wobbleDuration = 0.3f;

    public AnimState currentAnimState = AnimState.Idle;

    Tween stateTween;
    Vector3 baseScale;
    bool baseScaleCached;

    public void JumpTo(Vector3 target, TweenCallback onComplete = null)
    {
        var tween = transform.DOJump(target, jumpPower, 1, jumpDuration).SetEase(Ease.OutCubic);
        if (onComplete != null) tween.OnComplete(onComplete);
    }
    public void SetAnimState(AnimState state)
    {
        AnimState prev = currentAnimState;

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
            case AnimState.Idle:
                stateTween = transform.DOScale(baseScale * (1f + idleScaleAmount), idleDuration)
                    .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
                break;
            case AnimState.Combat:
                stateTween = transform.DOScale(baseScale * (1f + combatScaleAmount), combatDuration)
                    .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
                break;
            case AnimState.Blocked:
                stateTween = transform.DOPunchRotation(
                    new Vector3(0f, 0f, wobbleAngle), wobbleDuration, 10, 1f);
                break;
        }

        if (state == AnimState.Combat && prev != AnimState.Combat)
            ShooterController.Instance.RegisterCombat(this);
        else if (prev == AnimState.Combat && state != AnimState.Combat)
            ShooterController.Instance.UnregisterCombat(this);
    }
}