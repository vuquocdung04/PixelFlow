using System.Collections.Generic;
using UnityEngine;

public class LinkProp : PropHandler
{
    [SerializeField] private GameObject linkVisual;
    [SerializeField] private MeshRenderer ropeForShooter;
    [SerializeField] private MeshRenderer cylinderRope;
    [SerializeField] private Transform ropePoint;
    [SerializeField] private Transform connectionPivot;

    [Space(10), Header("Rope Config")]
    [SerializeField] private float ropeThickness = 0.1f;
    [SerializeField] private float ropeNativeLength = 1f;

    public List<Shooter> partners = new List<Shooter>();
    public Shooter ownedPartner;
    public LinkGroup group;

    public Transform RopePoint => ropePoint;
    public bool IsOwner => ownedPartner != null;
    public bool IsInGroup => group != null;

    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    private MaterialPropertyBlock _mpb;

    public override PropState Key => PropState.Link;

    public override void OnAttach(Shooter owner)
    {
        base.OnAttach(owner);
        if (linkVisual != null) linkVisual.SetActive(true);
    }

    public override void OnDetach()
    {
        if (linkVisual != null) linkVisual.SetActive(false);
        if (connectionPivot != null) connectionPivot.gameObject.SetActive(false);
        partners.Clear();
        ownedPartner = null;
        base.OnDetach();
    }

    public void Setup(Shooter partner, bool owner)
    {
        if (!partners.Contains(partner))
            partners.Add(partner);

        if (owner)
            ownedPartner = partner;

        if (ropeForShooter != null)
        {
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            ropeForShooter.GetPropertyBlock(_mpb);
            _mpb.SetColor(BaseColorID, Owner.BodyColor);
            ropeForShooter.SetPropertyBlock(_mpb);
        }

        if (owner)
        {
            connectionPivot.gameObject.SetActive(true);
            SetCylinderColors(Owner.BodyColor, partner.BodyColor);
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
        if (ownedPartner == null) return;

        LinkProp partnerLink = ownedPartner.GetProp<LinkProp>(); ;
        if (partnerLink == null || partnerLink.ropePoint == null) return;
        if (ropePoint == null || connectionPivot == null) return;

        Vector3 a = ropePoint.position;
        Vector3 b = partnerLink.ropePoint.position;
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

        foreach (var partner in partners)
        {
            if (partner == null) continue;
            LinkProp pl = partner.GetProp<LinkProp>();
            if (pl != null && pl.ownedPartner == Owner)
                pl.RefreshLink();
        }
    }
}
