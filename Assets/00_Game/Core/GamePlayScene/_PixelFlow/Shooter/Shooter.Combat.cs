using UnityEngine;

public partial class Shooter
{
    [Space(10), Header("Combat")]

    public GameObject projectilePrefab;

    public string colorHex;
    public Transform shootPoint;

    public void TickCombat()
    {
        if (!BrickGrid.Instance.GetSideAndLine(transform.position, out var side, out int line))
            return;

        Block outer = BrickGrid.Instance.GetOuterBlock(side, line);
        if (outer == null) return;
        if (outer.colorHex != colorHex) return;

        outer.Break();
    }
}