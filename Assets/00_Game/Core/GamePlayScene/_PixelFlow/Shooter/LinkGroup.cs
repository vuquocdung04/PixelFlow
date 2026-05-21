using System.Collections.Generic;

public class LinkGroup
{
    public List<Shooter> members = new List<Shooter>();

    public Shooter Leader => members.Count > 0 ? members[0] : null;
    public int Count => members.Count;

    public bool AllAtRow0()
    {
        foreach (var s in members)
            if (s.currentAnimState == ShooterAnimState.Blocked) return false;
        return true;
    }

    public bool AnyBlocked()
    {
        foreach (var s in members)
            if (s.IsBlocked) return true;
        return false;
    }

    public bool AllEmpty()
    {
        foreach (var s in members)
            if (s.projectileCount > 0) return false;
        return true;
    }
}