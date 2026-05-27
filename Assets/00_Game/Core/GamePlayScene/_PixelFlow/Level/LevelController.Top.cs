using System.Collections.Generic;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

public partial class LevelController
{
    [Space(10), Header("Top")]
    public float spacingX, spacingY;
    private int gridX, gridY;

    public Block blockPrefab;
    public Transform topParent;

    [System.NonSerialized] public Dictionary<int, Block> blockMap = new Dictionary<int, Block>();
    [System.NonSerialized] public TopData topData;

    [Button("GENERATE TOP", ButtonSizes.Large), GUIColor(0.5f, 1f, 0.6f)]
    public void GenerateTop()
    {
        ClearTop();

        if (levelData == null)
        {
            Debug.LogError("[GenerateTop] levelData NULL — abort");
            return;
        }

        topData = levelData.top;
        gridX = topData.gridX;
        gridY = topData.gridY;

        float offsetX = -(gridX - 1) * spacingX * 0.5f;
        float offsetY = (gridY - 1) * spacingY * 0.5f;

        // Init BrickGrid với world bounds
        Vector3 worldOrigin = topParent.TransformPoint(new Vector3(offsetX, 0f, offsetY));
        BrickGrid.Instance.Initialize(gridX, gridY, spacingX, spacingY, worldOrigin);

        foreach (var kv in topData.colors)
        {
            ColorUtility.TryParseHtmlString(kv.Key, out Color color);

            foreach (int idx in kv.Value)
            {
                int row = idx / gridX;
                int col = idx % gridX;
                float lx = offsetX + col * spacingX;
                float ly = offsetY - row * spacingY;

                var block = Instantiate(blockPrefab, topParent);
                block.name = $"Block_{idx}_{kv.Key}";
                block.transform.localPosition = new Vector3(lx, 0f, ly);
                block.SetColor(color);
                block.SetupGrid(col, row, kv.Key);

                BrickGrid.Instance.AddBlock(col, row, block);
                blockMap[idx] = block;
            }
        }

        int totalBlocks = 0;
        foreach (var kv in levelData.top.colors)
            totalBlocks += kv.Value.Count;
        BrickGrid.Instance.totalBlocks = totalBlocks;
    }

    [Button("CLEAR TOP", ButtonSizes.Medium), GUIColor(1f, 0.7f, 0.7f)]
    public void ClearTop()
    {
        blockMap.Clear();
        topData = null;
        if (BrickGrid.Instance != null) BrickGrid.Instance.Clear();

        for (int i = topParent.childCount - 1; i >= 0; i--)
        {
            var go = topParent.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }
    }
}