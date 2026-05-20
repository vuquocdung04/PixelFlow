using UnityEngine;

public partial class Shooter
{
    [Space(10), Header("Combat")]
    public string colorHex;
    public int projectileCount;
    public Transform shootPoint;
    public Projectile projectilePrefab;

    public void SetProjectileCount(int count) => projectileCount = count;
    
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

    private void Fire(Block target)
    {
        target.Claim();

        Projectile p = SimplePool2.Spawn(
            projectilePrefab,
            shootPoint.position,
            Quaternion.identity);

        //p.transform.position = shootPoint.position;

        p.Fire(target);

        PlayShootFeedback();
        //EventDispatcher.EventDispatcher.Instance.PostEvent(EventID.SHOOTER_FIRED, this);
    }

    private void PlayShootFeedback()
    {
    }
}