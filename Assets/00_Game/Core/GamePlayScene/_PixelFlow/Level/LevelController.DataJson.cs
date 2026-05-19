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
    public Dictionary<string, List<int>> colors;
    public List<int> blinds;
    public List<IceData> ices;
    public List<TunnelData> tunnels;
}

[System.Serializable]
public class IceData
{
    public int id;
    public int count;
}

[System.Serializable]
public class TunnelData
{
    public int tunnelID;
    public int spawnAtID;
    public List<string> colors;
}