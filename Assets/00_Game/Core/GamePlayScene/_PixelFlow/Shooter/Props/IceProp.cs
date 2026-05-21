using EventDispatcher;
using TMPro;
using UnityEngine;

public class IceProp : PropHandler
{
    [SerializeField] private GameObject iceVisual;
    [SerializeField] private TMP_Text iceTxt;
    [SerializeField] private int iceCount;

    public override PropState Key => PropState.Ice;

    public int Count => iceCount;

    public void SetCount(int count)
    {
        iceCount = count;
        if (iceTxt != null) iceTxt.text = count.ToString();
    }

    public override void OnAttach(Shooter owner)
    {
        base.OnAttach(owner);
        if (iceVisual != null) iceVisual.SetActive(true);
        owner.SetBodyVisible(false);
        this.RegisterListener(EventID.BLOCK_DESTROYED, OnBlockDestroyed);
    }

    public override void OnDetach()
    {
        this.RemoveListener(EventID.BLOCK_DESTROYED, OnBlockDestroyed);
        if (iceVisual != null) iceVisual.SetActive(false);
        Owner?.SetBodyVisible(true);
        base.OnDetach();
    }

    private void OnBlockDestroyed(object param)
    {
        iceCount--;
        if (iceTxt != null) iceTxt.text = iceCount.ToString();

        if (iceCount <= 0 && Owner != null)
            Owner.RemoveProps(PropState.Ice);
    }
}
