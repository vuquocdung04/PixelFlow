using UnityEngine;
using DG.Tweening;

public class Projectile : MonoBehaviour
{
    public AudioClip hitSFX;
    public float speed = 20f;
    private Block target;
    public void Fire(Block target)
    {
        this.target = target;

        Vector3 direction = (target.transform.position - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(direction);

        float distance = Vector3.Distance(transform.position, target.transform.position);
        float duration = speed > 0.0001f ? distance / speed : 0f;

        transform.SetParent(target.transform);

        transform.DOLocalMove(Vector3.up, duration)
            .SetEase(Ease.Linear)
            .OnComplete(OnArrive);
    }
    public void FireJumpBooster(Block target)
    {
        this.target = target;

        Vector3 direction = (target.transform.position - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(direction);

        float distance = Vector3.Distance(transform.position, target.transform.position);
        float duration = speed > 0.0001f ? distance / speed : 0f;

        transform.SetParent(target.transform);

        transform.DOLocalJump(Vector3.up, jumpPower: 10f, numJumps: 1, duration)
            .SetEase(Ease.Linear)
            .OnComplete(OnArrive);
    }

    private void OnArrive()
    {
        transform.SetParent(null);
        if (target != null) target.Break();
        target = null;
        AudioManager.Instance.PlaySfx(hitSFX);
        SimplePool2.Despawn(gameObject);
    }
}