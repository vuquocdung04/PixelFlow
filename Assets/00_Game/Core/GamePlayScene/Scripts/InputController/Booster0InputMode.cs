using UnityEngine;

public class Booster0InputMode : InputMode
{
    public override void HandleClick(RaycastHit hit)
    {
        Shooter shooter = hit.collider.GetComponentInParent<Shooter>();
        if (shooter == null) return;

        BoosterController.Instance.TryUseOnShooter(shooter);
    }
}