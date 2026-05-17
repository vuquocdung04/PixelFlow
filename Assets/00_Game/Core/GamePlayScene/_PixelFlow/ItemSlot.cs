using UnityEngine;
using DG.Tweening;

public class ItemSlot : MonoBehaviour
{
    public float returnDuration = 0.5f;

    public void Return(Transform returnPoint, float targetX)
    {
        transform.position = returnPoint.position;
        transform.DOMoveX(targetX, returnDuration);
        transform.rotation = Quaternion.identity;

    }

    public void MoveAlongPath(Vector3[] points, float duration, TweenCallback onComplete)
    {
        transform.DOPath(points, duration, PathType.CatmullRom, PathMode.Full3D, 50)
            .SetEase(Ease.Linear)
            .SetLookAt(0.01f, Vector3.forward, Vector3.up)
            .OnComplete(onComplete);
        transform.rotation = Quaternion.Euler(0, 0, 90f);
    }
}