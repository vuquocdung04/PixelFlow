using DG.Tweening;
using UnityEngine;

public partial class Shooter
{
    [Space(10), Header("Combat")]
    public string colorHex;
    public int projectileCount;
    public Transform shootPoint;
    public Projectile projectilePrefab;
    private bool _despawning;
    private Sequence shootSeq;
    public void SetProjectileCount(int count)
    {
        projectileCount = count;
        UpdateProjectileText();
    }

    public void TickCombat()
    {
        if (projectileCount <= 0) return;

        if (!BrickGrid.Instance.GetSideAndLine(transform.position, out var side, out int line))
            return;

        Block outer = BrickGrid.Instance.GetOuterBlock(side, line);
        if (outer == null) return;
        if (outer.IsClaimed) return;
        if (outer.colorHex != colorHex) return;

        Fire(outer);
    }

    private void Fire(Block target)
    {
        target.Claim();

        Projectile p = SimplePool2.Spawn(
            projectilePrefab,
            shootPoint.position,
            Quaternion.identity);

        //p.transform.position = shootPoint.position;

        p.Fire(target);

        projectileCount--;
        UpdateProjectileText();

        PlayShootFeedback();
        //EventDispatcher.EventDispatcher.Instance.PostEvent(EventID.SHOOTER_FIRED, this);

        if (projectileCount <= 0) Despawn();
    }

    private void Despawn()
    {
        if (_despawning) return;
        _despawning = true;

        ItemSlot slot = GetComponentInParent<ItemSlot>();


        ShooterController.Instance.UnregisterCombat(this);
        transform.SetParent(null);

        if (slot != null)
        {
            slot.AbortAndReturn();
        }

        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOLocalRotate(new Vector3(0, 360f, 0), 0.5f, RotateMode.FastBeyond360));
        seq.Join(transform.DOScale(Vector3.zero, 0.5f));
        seq.OnComplete(() => Destroy(gameObject));
    }
    private void UpdateProjectileText()
    {
        txtBody.text = projectileCount.ToString();
    }
    private void PlayShootFeedback()
    {
        if (shootSeq != null && shootSeq.IsActive() && shootSeq.IsPlaying()) return;

        float recoilForce = 0.3f;
        float feedbackDuration = 0.2f;

        shootSeq = DOTween.Sequence();

        shootSeq.Append(transform.DOLocalMoveZ(-recoilForce, feedbackDuration / 2f).SetEase(Ease.OutQuad));
        shootSeq.Join(transform.DOScale(new Vector3(1.15f, 0.75f, 1.15f), feedbackDuration / 2f).SetEase(Ease.OutQuad));

        shootSeq.Append(transform.DOLocalMoveZ(0f, feedbackDuration / 2f).SetEase(Ease.InOutSine));
        shootSeq.Join(transform.DOScale(Vector3.one, feedbackDuration / 2f).SetEase(Ease.InOutSine));
    }
}