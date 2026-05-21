using System.Collections.Generic;
using UnityEngine;

public enum PropState { Blind, Ice, Link }

public partial class Shooter : MonoBehaviour
{
    public LinkProp Link { get; private set; }
    public IceProp Ice { get; private set; }
    public BlindProp Blind { get; private set; }

    private readonly Dictionary<PropState, PropHandler> _props = new Dictionary<PropState, PropHandler>();
    private readonly Dictionary<PropState, PropHandler> _available = new Dictionary<PropState, PropHandler>();

    private ShooterState _currentState;

    public bool HasLink => HasProp(PropState.Link);
    public bool IsInGroup => Link != null && Link.IsInGroup;

    public LinkGroup linkGroup
    {
        get => Link != null ? Link.group : null;
        set { if (Link != null) Link.group = value; }
    }

    public bool IsBlocked =>
        currentAnimState == ShooterAnimState.Blocked ||
        HasProp(PropState.Ice);

    protected virtual void Awake()
    {
        Link = GetComponent<LinkProp>();
        Ice = GetComponent<IceProp>();
        Blind = GetComponent<BlindProp>();

        foreach (var h in GetComponents<PropHandler>())
            _available[h.Key] = h;
    }

    public bool HasProp(PropState key) => _props.ContainsKey(key);

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
        Link?.Setup(partner, owner);
    }

    public void RefreshAllLinks() => Link?.RefreshAllLinks();
    public void RefreshLink() => Link?.RefreshLink();
    public void SetIceCount(int count) => Ice?.SetCount(count);
}
