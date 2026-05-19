using UnityEngine;

public partial class Shooter
{
    public string colorHex;
    public Transform shootPoint;
    public Projectile projectilePrefab;
    public float projectileDuration = 0.4f;

    public void TickCombat()
    {
        if (!BrickGrid.Instance.GetSideAndLine(transform.position, out var side, out int line))
            return;

        Block outer = BrickGrid.Instance.GetOuterBlock(side, line);
        if (outer == null) return;
        if (outer.IsClaimed) return;
        if (outer.colorHex != colorHex) return;

        Fire(outer);
    }

    void Fire(Block target)
    {
        target.Claim();

        Projectile p = SimplePool2.Spawn<Projectile>(
            projectilePrefab,
            shootPoint.position,
            Quaternion.identity);
        p.Fire(target, projectileDuration);

        PlayShootFeedback();
        EventDispatcher.EventDispatcher.Instance.PostEvent(EventID.SHOOTER_FIRED, this);
    }

    void PlayShootFeedback()
    {
    }
}