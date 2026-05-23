using UnityEngine;

public class Booster2InputMode : InputMode
{
    public override void HandleClick(RaycastHit hit)
    {
        Block block = hit.collider.GetComponentInParent<Block>();
        if (block == null) return;

        BoosterController.Instance.TryUseOnBlock(block);
    }
}