using System.Collections.Generic;
using UnityEngine;

public enum PropState { Blind, Ice, Link }

public partial class Shooter : MonoBehaviour
{
    [System.NonSerialized] public int gridIdx;
    private readonly Dictionary<PropState, PropHandler> _props = new Dictionary<PropState, PropHandler>();
    private readonly Dictionary<PropState, PropHandler> _available = new Dictionary<PropState, PropHandler>();

    private ShooterState _currentState;

    public bool HasLink => HasProp(PropState.Link);
    public bool IsInGroup
    {
        get
        {
            var link = GetProp<LinkProp>();
            return link != null && link.IsInGroup;
        }
    }

    public LinkGroup linkGroup
    {
        get => GetProp<LinkProp>()?.group;
        set
        {
            var link = GetProp<LinkProp>();
            if (link != null) link.group = value;
        }
    }

    public bool IsBlocked =>
        currentAnimState == ShooterAnimState.Blocked ||
        HasProp(PropState.Ice);

    public bool HasProp(PropState key) => _props.ContainsKey(key);

    public T GetProp<T>() where T : PropHandler
    {
        foreach (var h in _available.Values)
            if (h is T t) return t;
        return null;
    }

    public void AddProps(PropState key)
    {
        if (_props.ContainsKey(key)) return;
        if (!_available.TryGetValue(key, out var h)) return;
        _props[key] = h;
        h.OnAttach(this);
    }

    public void RemoveProps(PropState key)
    {
        if (!_props.TryGetValue(key, out var h)) return;
        h.OnDetach();
        _props.Remove(key);
    }

    public void ChangeState(ShooterState next)
    {
        if (next == null) return;
        if (_currentState != null && _currentState.GetType() == next.GetType()) return;
        _currentState?.OnExit();
        _currentState = next;
        _currentState.OnEnter(this);
    }

    public void SetupLink(Shooter partner, bool owner)
    {
        AddProps(PropState.Link);
        GetProp<LinkProp>()?.Setup(partner, owner);
    }

    public void RefreshAllLinks() => GetProp<LinkProp>()?.RefreshAllLinks();
    public void RefreshLink() => GetProp<LinkProp>()?.RefreshLink();
    public void SetIceCount(int count) => GetProp<IceProp>()?.SetCount(count);
}
