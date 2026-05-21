using System.Collections.Generic;

public class LinkGroup
{
    public List<Shooter> members = new List<Shooter>();

    public Shooter Leader => members.Count > 0 ? members[0] : null;
    public int Count => members.Count;

    public bool AllEmpty()
    {
        foreach (var s in members)
            if (s.projectileCount > 0) return false;
        return true;
    }

    public bool CanClick(Shooter clicked, int gridX)
    {
        foreach (var s in members)
            if (s.HasProp(PropState.Ice)) return false;

        if (clicked.gridIdx / gridX != 0) return false;

        var memberSet = new HashSet<Shooter>(members);
        foreach (var s in members)
        {
            if (s == clicked) continue;

            int sRow = s.gridIdx / gridX;
            if (sRow == 0) continue;

            int aboveIdx = s.gridIdx - gridX;
            if (!LevelController.Instance.shooterMap.TryGetValue(aboveIdx, out var above))
                return false;
            if (!memberSet.Contains(above))
                return false;
        }

        return true;
    }
}