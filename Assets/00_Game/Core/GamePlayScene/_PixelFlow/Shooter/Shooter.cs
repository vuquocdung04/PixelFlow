using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class LinkConnection
{
    public Shooter partner;
    public MeshRenderer cylinderRope;
    public bool isOwner;
}
public enum PropState { Blind, Ice, Link }
public partial class Shooter : MonoBehaviour
{
    public HashSet<PropState> activeProps = new HashSet<PropState>();

    [Space(10), Header("Link")]
    [Space(10), Header("Link")]
    public List<Shooter> linkedPartners = new List<Shooter>();
    public Shooter ownedLinkPartner; 
    public bool HasLink => activeProps.Contains(PropState.Link);
    public bool IsLinkOwner => ownedLinkPartner != null;
    public bool IsBlocked =>
    currentAnimState == ShooterAnimState.Blocked ||
    activeProps.Contains(PropState.Ice);
    public void AddProps(PropState state)
    {
        if (activeProps.Add(state))
            OnPropAdded(state);
    }
    public void RemoveProps(PropState state)
    {
        if (activeProps.Remove(state))
        {
            OnPropRemoved(state);
            if (activeProps.Count == 0)
                OnAllPropsCleared();
        }
    }
}