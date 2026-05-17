using UnityEngine;
using DG.Tweening;

public class ItemSlot : MonoBehaviour
{
    public Transform itemParent;
    public float returnDuration = 0.5f;

    public void Return(float targetX)
    {
        transform.DOMoveX(targetX, returnDuration).SetEase(Ease.Linear);
        transform.DORotate(Vector3.zero, returnDuration).SetEase(Ease.Linear);
    }

    public void MoveAlongPath(Vector3[] points, float duration, TweenCallback onComplete)
    {
        transform.DOPath(points, duration, PathType.CatmullRom, PathMode.Full3D, 50)
            .SetEase(Ease.Linear)
            .SetLookAt(0.01f, Vector3.forward, Vector3.up)
            .OnComplete(onComplete);
    }
}