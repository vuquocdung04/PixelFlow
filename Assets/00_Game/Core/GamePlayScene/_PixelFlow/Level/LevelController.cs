using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class LevelJsonData
{
    public TopData top;
    public BottomData bottom;
}

[System.Serializable]
public class TopData
{
    public int gridX;
    public int gridY;
    public Dictionary<string, List<int>> colors;
}

[System.Serializable]
public class BottomData
{
    public int gridX;
    public int gridY;
    public Dictionary<string, Dictionary<int, int>> colors;
    public List<int> blinds;
    public Dictionary<int, int> ices;
    public Dictionary<int, TunnelData> tunnels;
    public List<List<int>> links;
}

[System.Serializable]
public class TunnelColor { public string hex; public int count; }

[System.Serializable]
public class TunnelData
{
    public int spawnAtID;
    public List<TunnelColor> colors;
}


public partial class LevelController : StaffSingleton<LevelController>
{
    public override void Init()
    {
        GenerateTop();
        GenerateBottom();
    }
}

