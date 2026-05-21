using DG.Tweening;
using UnityEngine;

public partial class Shooter
{
    [Space(10), Header("Combat")]
    public string colorHex;
    public int projectileCount;
    public Transform shootPoint;
    public Projectile projectilePrefab;
    public ParticleSystem projectileFx;
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

        Projectile p = SimplePool2.Spawn(projectilePrefab, shootPoint.position, Quaternion.identity);
        p.Fire(target);

        projectileCount--;
        UpdateProjectileText();

        PlayShootFeedback();

        if (projectileCount <= 0)
        {
            if (linkGroup != null)
            {
                ShooterController.Instance.UnregisterCombat(this);
                if (linkGroup.AllEmpty())
                    DespawnGroup();
            }
            else
            {
                DespawnSelf();
            }
        }
    }

    private void DespawnSelf()
    {
        if (_despawning) return;
        _despawning = true;

        KillShootFeedback(); 
        
        ItemSlot slot = GetComponentInParent<ItemSlot>();
        ShooterController.Instance.UnregisterCombat(this);
        ShooterController.Instance.OnShooterDespawn();
        transform.SetParent(null);


        if (slot != null) slot.AbortAndReturn();

        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOScale(1.2f, 0.15f).SetEase(Ease.OutQuad));
        seq.Append(transform.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack));
        seq.OnComplete(() => Destroy(gameObject));
    }

    private void DespawnGroup()
    {
        if (linkGroup == null) return;

        foreach (var s in linkGroup.members)
            if (s != null) s.DespawnSelf();
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

        projectileFx.Play();

        shootSeq.Append(transform.DOLocalMoveZ(-recoilForce, feedbackDuration / 2f).SetEase(Ease.OutQuad));
        shootSeq.Join(transform.DOScale(new Vector3(1.15f, 0.75f, 1.15f), feedbackDuration / 2f).SetEase(Ease.OutQuad));

        shootSeq.Append(transform.DOLocalMoveZ(0f, feedbackDuration / 2f).SetEase(Ease.InOutSine));
        shootSeq.Join(transform.DOScale(Vector3.one, feedbackDuration / 2f).SetEase(Ease.InOutSine));
    }

    public void KillShootFeedback()
    {
        if (shootSeq != null && shootSeq.IsActive())
            shootSeq.Kill(complete: true);
    }
}