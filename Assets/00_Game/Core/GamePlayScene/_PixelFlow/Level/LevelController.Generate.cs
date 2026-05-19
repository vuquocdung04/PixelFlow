using System.Text.RegularExpressions;
using Sirenix.OdinInspector;
using UnityEngine;

public partial class LevelController
{
    [Space(10), Header("Generate")]
    public TextAsset levelTest;
    public float spacingX, spacingY;
    private int gridX, gridY;

    public Block blockPrefab;

    [Button("GENERATE", ButtonSizes.Large), GUIColor(0.5f, 1f, 0.6f)]
    public void Generate()
    {
        if (levelTest == null) { Debug.LogError("[Generate] Thiếu levelTest."); return; }
        if (blockPrefab == null) { Debug.LogError("[Generate] Thiếu blockPrefab."); return; }

        ClearGenerated();

        string json = levelTest.text;

        // ----- Parse gridX, gridY -----
        var mx = Regex.Match(json, "\"gridX\"\\s*:\\s*(\\d+)");
        var my = Regex.Match(json, "\"gridY\"\\s*:\\s*(\\d+)");
        if (!mx.Success || !my.Success)
        {
            Debug.LogError("[Generate] JSON thiếu gridX hoặc gridY.");
            return;
        }
        gridX = int.Parse(mx.Groups[1].Value);
        gridY = int.Parse(my.Groups[1].Value);

        // ----- Parse color entries: "#XXXXXX": [n, n, ...] -----
        var entries = Regex.Matches(json, "\"(#[0-9A-Fa-f]{6,8})\"\\s*:\\s*\\[([\\d,\\s]*)\\]");
        if (entries.Count == 0)
        {
            Debug.LogWarning("[Generate] Không tìm thấy entry màu nào trong JSON.");
            return;
        }

        // ----- Center origin so grid được canh giữa transform.position -----
        float offsetX = -(gridX - 1) * spacingX * 0.5f;
        float offsetY = (gridY - 1) * spacingY * 0.5f;

        int total = 0;
        foreach (Match m in entries)
        {
            string hex = m.Groups[1].Value;
            string indicesStr = m.Groups[2].Value;

            if (!ColorUtility.TryParseHtmlString(hex, out Color color))
            {
                Debug.LogWarning($"[Generate] Bỏ qua hex không hợp lệ: {hex}");
                continue;
            }

            var parts = indicesStr.Split(',');
            for (int i = 0; i < parts.Length; i++)
            {
                var s = parts[i].Trim();
                if (string.IsNullOrEmpty(s)) continue;
                if (!int.TryParse(s, out int idx)) continue;
                
                int row = idx / gridX;
                int col = idx % gridX;

                float lx = offsetX + col * spacingX;
                float ly = offsetY - row * spacingY; // row 0 = top → +Y; row tăng → -Y

                var block = Instantiate(blockPrefab, transform);
                block.name = $"Block_{idx}_{hex}";
                block.transform.localPosition = new Vector3(lx, 0f, ly); 
                block.SetColor(color);
                total++;
            }
        }

        Debug.Log($"<color=cyan>[Generate]</color> grid {gridX}×{gridY}, " +
                  $"spawned <b>{total}</b> blocks ({entries.Count} màu)");
    }

    [Button("Clear", ButtonSizes.Medium), GUIColor(1f, 0.7f, 0.7f)]
    public void ClearGenerated()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var go = transform.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }
    }
}