using UnityEngine;

public class DisabledInputMode : InputMode
{
    public override void HandleClick(RaycastHit hit)
    {
        // Do nothing
    }
}