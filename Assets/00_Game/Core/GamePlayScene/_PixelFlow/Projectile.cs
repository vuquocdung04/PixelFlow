using UnityEngine;
using DG.Tweening;

public class Projectile : MonoBehaviour
{
    public float speed = 20f;
    private Block target;
    [SerializeField] private TrailRenderer trail;
    public void Fire(Block target)
    {
        this.target = target;

        if (trail != null)
            trail.Clear();

        float distance = Vector3.Distance(transform.position, target.transform.position);
        float duration = speed > 0.0001f ? distance / speed : 0f;

        transform.SetParent(target.transform);

        transform.DOLocalMove(Vector3.up, duration)
            .SetEase(Ease.Linear)
            .OnComplete(OnArrive);
    }

    private void OnArrive()
    {
        transform.SetParent(null);
        if (target != null) target.Break();
        target = null;
        if (trail != null) trail.Clear();

        SimplePool2.Despawn(gameObject);
    }
}