using EventDispatcher;
using TMPro;
using UnityEngine;

public partial class Shooter
{
    [Space(10), Header("Visual")]
    public MeshRenderer body;
    public MeshRenderer ropeForShooter;
    public MeshRenderer cylinderRope;
    public Transform ropePoint;
    public Transform connectionPivot;

    public GameObject bodyVisual;
    public TextMeshPro txtBody;

    [Space(10), Header("Prop Visuals")]
    public GameObject blindVisual;
    public GameObject iceVisual;
    public GameObject linkVisual;

    [Space(10), Header("Ice")]
    public int iceCount;
    public TMP_Text iceTxt;

    [Space(10), Header("Rope Config")]
    public float ropeThickness = 0.1f;
    public float ropeNativeLength = 1f;

    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    private MaterialPropertyBlock _mpb;
    private Color _myColor;
    public void SetColor(Color color)
    {
        _myColor = color;
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

    public void SetupLink(Shooter partner, bool owner)
    {
        if (!linkedPartners.Contains(partner))
            linkedPartners.Add(partner);

        if (owner)
            ownedLinkPartner = partner;

        AddProps(PropState.Link);

        if (ropeForShooter != null)
        {
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            ropeForShooter.GetPropertyBlock(_mpb);
            _mpb.SetColor(BaseColorID, _myColor);
            ropeForShooter.SetPropertyBlock(_mpb);
        }
        if (owner)
        {
            connectionPivot.gameObject.SetActive(true);
            SetCylinderColors(_myColor, partner._myColor);
            RefreshLink();
        }
        else
        {
            connectionPivot.gameObject.SetActive(false);
        }
    }

    private void SetCylinderColors(Color colorA, Color colorB)
    {
        if (_mpb == null) _mpb = new MaterialPropertyBlock();

        cylinderRope.GetPropertyBlock(_mpb, 0);
        _mpb.SetColor(BaseColorID, colorA);
        cylinderRope.SetPropertyBlock(_mpb, 0);

        cylinderRope.GetPropertyBlock(_mpb, 1);
        _mpb.SetColor(BaseColorID, colorB);
        cylinderRope.SetPropertyBlock(_mpb, 1);
    }

    public void RefreshLink()
    {
        if (ownedLinkPartner == null) return;
        if (ropePoint == null || ownedLinkPartner.ropePoint == null) return;
        if (connectionPivot == null) return;

        Vector3 a = ropePoint.position;
        Vector3 b = ownedLinkPartner.ropePoint.position;
        Vector3 dir = b - a;
        float dist = dir.magnitude;

        connectionPivot.position = a;
        if (dist > 0.0001f)
            connectionPivot.forward = dir / dist;

        connectionPivot.localScale = new Vector3(
            ropeThickness,
            ropeThickness,
            dist / ropeNativeLength
        );
    }
    public void RefreshAllLinks()
    {
        RefreshLink();

        foreach (var partner in linkedPartners)
        {
            if (partner != null && partner.ownedLinkPartner == this)
                partner.RefreshLink();
        }
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
                connectionPivot.gameObject.SetActive(false);
                linkedPartners.Clear();
                ownedLinkPartner = null;
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