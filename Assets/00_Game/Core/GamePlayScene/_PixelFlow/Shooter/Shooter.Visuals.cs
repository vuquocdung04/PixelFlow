using TMPro;
using UnityEngine;

public partial class Shooter
{
    [Space(10), Header("Visual")]
    public Renderer body;


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
        if (body == null)
        {
            Debug.LogWarning($"[Shooter] '{name}' chưa gán body Renderer.", this);
            return;
        }

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