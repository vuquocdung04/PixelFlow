using UnityEngine;

public partial class Shooter
{
    [Space(10), Header("Prop Visuals")]
    public GameObject blindVisual;
    public GameObject iceVisual;
    public GameObject linkVisual;

    private void OnPropAdded(PropState state)
    {
        switch (state)
        {
            case PropState.Blind:
                blindVisual.SetActive(true);
                break;
            case PropState.Ice:
                iceVisual.SetActive(true);
                break;
            case PropState.Link:
                linkVisual.SetActive(true);
                break;
        }
    }

    private void OnPropRemoved(PropState state)
    {
        switch (state)
        {
            case PropState.Blind:
                blindVisual.SetActive(false);
                break;
            case PropState.Ice:
                iceVisual.SetActive(false);
                break;
            case PropState.Link:
                linkVisual.SetActive(false);
                break;
        }
    }

    private void OnAllPropsCleared()
    {

    }
}