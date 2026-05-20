using EventDispatcher;
using TMPro;
using UnityEngine;

public partial class Shooter
{
    [Space(10), Header("Visual")]
    public MeshRenderer body;
    public GameObject bodyVisual;
    public TextMeshPro txtBody;

    [Space(10), Header("Prop Visuals")]
    public GameObject blindVisual;
    public GameObject iceVisual;
    public GameObject linkVisual;

    [Space(10), Header("Ice")]
    public int iceCount;
    public TMP_Text iceTxt;

    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    private MaterialPropertyBlock _mpb;

    public void SetColor(Color color)
    {
        if (_mpb == null) _mpb = new MaterialPropertyBlock();
        body.GetPropertyBlock(_mpb);
        _mpb.SetColor(BaseColorID, color);
        body.SetPropertyBlock(_mpb);
    }
    public void SetIceCount(int count)
    {
        iceCount = count;
        if (iceTxt != null) iceTxt.text = count.ToString();
    }
    private void OnPropAdded(PropState state)
    {
        switch (state)
        {
            case PropState.Blind:
                blindVisual.SetActive(true);
                bodyVisual.SetActive(false);
                break;
            case PropState.Ice:
                iceVisual.SetActive(true);
                bodyVisual.SetActive(false);
                this.RegisterListener(EventID.BLOCK_DESTROYED, OnBlockDestroyed);
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
                bodyVisual.SetActive(true);
                break;
            case PropState.Ice:
                iceVisual.SetActive(false);
                bodyVisual.SetActive(true);
                this.RemoveListener(EventID.BLOCK_DESTROYED, OnBlockDestroyed);
                break;
            case PropState.Link:
                linkVisual.SetActive(false);
                break;
        }
    }
    private void OnBlockDestroyed(object param)
    {
        iceCount--;
        if (iceTxt != null) iceTxt.text = iceCount.ToString();

        if (iceCount <= 0)
            RemoveProps(PropState.Ice);
    }

    private void OnAllPropsCleared()
    {

    }
}