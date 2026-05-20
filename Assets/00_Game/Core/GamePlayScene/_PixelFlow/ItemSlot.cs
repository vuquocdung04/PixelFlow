using UnityEngine;
using DG.Tweening;

public class ItemSlot : MonoBehaviour
{
    public Transform itemParent;
    public Transform visual;
    public float returnDuration = 0.5f;

    private Vector3 nativeVisualRotation;

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

    public void MoveAlongPath(Vector3[] points, float duration, TweenCallback onComplete)
    {
        transform.DOPath(points, duration, PathType.CatmullRom, PathMode.Full3D, 50)
            .SetEase(Ease.Linear)
            .SetLookAt(0.01f, Vector3.left, Vector3.up)
            .OnComplete(onComplete);
    }

    public void PrepareToReceive(Vector3 targetPos, float duration)
    {
        transform.DOMove(targetPos, duration).SetEase(Ease.Linear);
        visual.DORotate(Vector3.zero, duration).SetEase(Ease.Linear);
    }

    public void DockShooter(Shooter shooter)
    {
        shooter.transform.SetParent(itemParent);
        shooter.transform.localPosition = Vector3.zero;
    }
}