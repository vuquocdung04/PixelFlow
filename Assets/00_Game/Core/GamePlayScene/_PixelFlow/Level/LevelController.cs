using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;

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
    private const string LEVEL_PATH_PREFIX = "level_";
    private static int _maxLevel = -1;

    public static int MaxLevel
    {
        get
        {
            if (_maxLevel < 0) _maxLevel = ScanMaxLevel();
            return _maxLevel;
        }
    }

    private static int ScanMaxLevel()
    {
        int max = 0;
        while (Resources.Load<TextAsset>(LEVEL_PATH_PREFIX + (max + 1)) != null)
            max++;
        return max;
    }

    private LevelJsonData levelData;

    public override void Init()
    {
        LoadCurrentLevelData();
        GenerateTop();
        GenerateBottom();
    }

    private void LoadCurrentLevelData()
    {
        int level = Mathf.Min(UseProfile.Level.Value, MaxLevel);
        TextAsset asset = Resources.Load<TextAsset>(LEVEL_PATH_PREFIX + level);

        if (asset == null)
        {
            Debug.LogError($"[LevelController] Không load được level_{level}");
            return;
        }

        levelData = JsonConvert.DeserializeObject<LevelJsonData>(asset.text);
    }
}

