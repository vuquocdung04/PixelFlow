using UnityEngine;
using DG.Tweening;

public class ItemSlot : MonoBehaviour
{
    public Transform itemParent;
    public Transform visual;
    public float returnDuration = 0.5f;
    private Vector3 nativeVisualRotation;
    private Shooter dockedShooter;
    public void AbortAndReturn()
    {
        transform.DOKill();
        Conveyor.Instance.ReturnSlot(this);
    }

    public void Init()
    {
        nativeVisualRotation = visual.localEulerAngles;
    }

    public void MoveToX(float targetX)
    {
        transform.DOMoveX(targetX, returnDuration).SetEase(Ease.Linear);
    }
    public void ResetVisualRotation()
    {
        visual.DORotate(nativeVisualRotation, returnDuration).SetEase(Ease.Linear);
    }

    private Tween _pathTween;
    private Tween _speedTween;

    // Tăng tốc ngay cả khi slot đang chạy giữa vòng (không phải đợi hết vòng)
    public void SpeedUpPath(float multiplier, float blend = 0.3f)
    {
        if (_pathTween == null || !_pathTween.IsActive()) return;

        _speedTween?.Kill();
        var t = _pathTween;
        _speedTween = DOTween.To(() => t.timeScale, x => { if (t.IsActive()) t.timeScale = x; }, t.timeScale * multiplier, blend)
            .SetLink(gameObject);
    }

    public void MoveAlongPath(Vector3[] points, float duration, TweenCallback onComplete)
    {
        _speedTween?.Kill();
        if (dockedShooter != null) dockedShooter.ResetFireLine();

        var tween = transform.DOPath(points, duration, PathType.CatmullRom, PathMode.Full3D, 50)
            .SetEase(Ease.Linear)
            .SetLookAt(0.01f, Vector3.left, Vector3.up);
        _pathTween = tween;

        tween.OnUpdate(() =>
        {
            if (dockedShooter != null && dockedShooter.HasLink)
                dockedShooter.RefreshAllLinks();
        });

        tween.OnComplete(onComplete);
    }
    public void PrepareToReceive(Vector3 targetPos, float duration, TweenCallback onArrived = null)
    {
        var tween = transform.DOMove(targetPos, duration).SetEase(Ease.InOutSine);
        if (onArrived != null) tween.OnComplete(onArrived);
        visual.DORotate(Vector3.zero, duration).SetEase(Ease.Linear);
    }

    public void DockShooter(Shooter shooter)
    {
        shooter.transform.SetParent(itemParent);
        shooter.transform.localPosition = Vector3.zero;
        dockedShooter = shooter;

        // Khay nảy nhẹ khi shooter đáp xuống
        visual.DOKill(true);
        visual.DOPunchScale(visual.localScale * 0.18f, 0.25f, 6, 0.6f);
    }

    public void Pause()
    {
        transform.DOPause();
    }

    public void Resume()
    {
        transform.DOPlay();
    }
}