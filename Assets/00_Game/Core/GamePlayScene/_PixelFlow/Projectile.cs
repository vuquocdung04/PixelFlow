using UnityEngine;
using DG.Tweening;

public class Projectile : MonoBehaviour
{
    private Block target;

    public void Fire(Block target, float duration)
    {
        this.target = target;
        transform.SetParent(target.transform);
        transform.DOLocalMove(Vector3.zero, duration)
            .SetEase(Ease.Linear)
            .OnComplete(OnArrive);
    }

    void OnArrive()
    {
        transform.SetParent(null);
        if (target != null) target.Break();
        target = null;
        SimplePool2.Despawn(gameObject);
    }
}