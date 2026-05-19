using System.Collections.Generic;
using UnityEngine;
public enum PropState { Blind, Ice, Link }

public partial class Shooter : MonoBehaviour
{
    public HashSet<PropState> activeProps = new HashSet<PropState>();

    public bool IsBlocked =>
    currentAnimState == AnimState.Blocked ||
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