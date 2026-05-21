using UnityEngine;

public class BlindProp : PropHandler
{
    [SerializeField] private GameObject blindVisual;

    public override PropState Key => PropState.Blind;

    public override void OnAttach(Shooter owner)
    {
        base.OnAttach(owner);
        if (blindVisual != null) blindVisual.SetActive(true);
        owner.SetBodyVisible(false);
    }

    public override void OnDetach()
    {
        if (blindVisual != null) blindVisual.SetActive(false);
        Owner?.SetBodyVisible(true);
        base.OnDetach();
    }
}
