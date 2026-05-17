using System.Collections.Generic;
using UnityEngine;

public partial class Shooter : MonoBehaviour
{
    public enum PropState { Blind, Ice, Link }

    public HashSet<PropState> activeProps = new HashSet<PropState>();


    public void AddProps(PropState state)
    {
        if (activeProps.Add(state))
            OnPropAdded(state);
    }
    public void RemoveProps(PropState state)
    {
        if (activeProps.Remove(state))
            OnPropRemoved(state);

        if (activeProps.Count == 0)
            OnAllPropsCleared();
    }
}