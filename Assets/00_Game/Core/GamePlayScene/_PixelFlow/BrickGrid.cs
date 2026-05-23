using System.Collections.Generic;
using EventDispatcher;
using UnityEngine;

public enum Side { Top, Bottom, Left, Right }

public class BrickGrid : StaffSingleton<BrickGrid>
{
    private Block[,] grid;
    private int width, height;
    private float spacingX, spacingY;
    private float minX, maxX, minZ, maxZ;
    public int totalBlocks;

    // Caches
    private HashSet<Block> aliveBlocks = new();
    private Dictionary<string, HashSet<Block>> blocksByColor = new();
    private int[] topOuterRow;
    private int[] bottomOuterRow;
    private int[] leftOuterCol;
    private int[] rightOuterCol;

    public override void Init()
    {
        this.RegisterListener(EventID.BLOCK_DESTROYED, OnBlockDestroyed);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        this.RemoveListener(EventID.BLOCK_DESTROYED, OnBlockDestroyed);
    }

    private void OnBlockDestroyed(object param)
    {
        totalBlocks--;
        if (totalBlocks <= 0)
            EventDispatcher.EventDispatcher.Instance.PostEvent(EventID.LEVEL_COMPLETE);
    }

    public void Initialize(int w, int h, float sx, float sy, Vector3 worldOrigin)
    {
        width = w;
        height = h;
        spacingX = sx;
        spacingY = sy;

        minX = worldOrigin.x;
        maxX = worldOrigin.x + (w - 1) * sx;
        minZ = worldOrigin.z - (h - 1) * sy;
        maxZ = worldOrigin.z;

        grid = new Block[w, h];

        aliveBlocks.Clear();
        blocksByColor.Clear();

        topOuterRow = new int[w];
        bottomOuterRow = new int[w];
        leftOuterCol = new int[h];
        rightOuterCol = new int[h];

        for (int i = 0; i < w; i++)
        {
            topOuterRow[i] = -1;
            bottomOuterRow[i] = -1;
        }
        for (int i = 0; i < h; i++)
        {
            leftOuterCol[i] = -1;
            rightOuterCol[i] = -1;
        }
    }

    public void AddBlock(int col, int row, Block block)
    {
        if (col < 0 || col >= width || row < 0 || row >= height) return;
        grid[col, row] = block;

        aliveBlocks.Add(block);

        if (!blocksByColor.TryGetValue(block.colorHex, out var set))
            blocksByColor[block.colorHex] = set = new HashSet<Block>();
        set.Add(block);

        // Update outer pointers
        if (topOuterRow[col] == -1 || row < topOuterRow[col]) topOuterRow[col] = row;
        if (bottomOuterRow[col] == -1 || row > bottomOuterRow[col]) bottomOuterRow[col] = row;
        if (leftOuterCol[row] == -1 || col < leftOuterCol[row]) leftOuterCol[row] = col;
        if (rightOuterCol[row] == -1 || col > rightOuterCol[row]) rightOuterCol[row] = col;
    }

    public void RemoveBlock(int col, int row)
    {
        if (col < 0 || col >= width || row < 0 || row >= height) return;
        var block = grid[col, row];
        if (block == null) return;

        grid[col, row] = null;
        aliveBlocks.Remove(block);

        if (blocksByColor.TryGetValue(block.colorHex, out var set))
            set.Remove(block);

        // Update outer pointers
        if (topOuterRow[col] == row) topOuterRow[col] = ScanTop(col);
        if (bottomOuterRow[col] == row) bottomOuterRow[col] = ScanBottom(col);
        if (leftOuterCol[row] == col) leftOuterCol[row] = ScanLeft(row);
        if (rightOuterCol[row] == col) rightOuterCol[row] = ScanRight(row);
    }

    private int ScanTop(int col)
    {
        for (int r = 0; r < height; r++)
            if (grid[col, r] != null && grid[col, r].IsAlive) return r;
        return -1;
    }
    private int ScanBottom(int col)
    {
        for (int r = height - 1; r >= 0; r--)
            if (grid[col, r] != null && grid[col, r].IsAlive) return r;
        return -1;
    }
    private int ScanLeft(int row)
    {
        for (int c = 0; c < width; c++)
            if (grid[c, row] != null && grid[c, row].IsAlive) return c;
        return -1;
    }
    private int ScanRight(int row)
    {
        for (int c = width - 1; c >= 0; c--)
            if (grid[c, row] != null && grid[c, row].IsAlive) return c;
        return -1;
    }

    public void Clear()
    {
        if (grid == null) return;
        for (int c = 0; c < width; c++)
            for (int r = 0; r < height; r++)
                grid[c, r] = null;

        aliveBlocks.Clear();
        blocksByColor.Clear();

        if (topOuterRow != null)
        {
            for (int i = 0; i < width; i++) { topOuterRow[i] = -1; bottomOuterRow[i] = -1; }
            for (int i = 0; i < height; i++) { leftOuterCol[i] = -1; rightOuterCol[i] = -1; }
        }
    }

    public Block GetOuterBlock(Side side, int line)
    {
        switch (side)
        {
            case Side.Top:
                if (line < 0 || line >= width) return null;
                int tr = topOuterRow[line];
                return tr >= 0 ? grid[line, tr] : null;
            case Side.Bottom:
                if (line < 0 || line >= width) return null;
                int br = bottomOuterRow[line];
                return br >= 0 ? grid[line, br] : null;
            case Side.Left:
                if (line < 0 || line >= height) return null;
                int lc = leftOuterCol[line];
                return lc >= 0 ? grid[lc, line] : null;
            case Side.Right:
                if (line < 0 || line >= height) return null;
                int rc = rightOuterCol[line];
                return rc >= 0 ? grid[rc, line] : null;
        }
        return null;
    }

    public bool GetSideAndLine(Vector3 worldPos, out Side side, out int line)
    {
        side = Side.Top;
        line = -1;

        float dxRight = worldPos.x - maxX;
        float dxLeft = minX - worldPos.x;
        float dzAbove = worldPos.z - maxZ;
        float dzBelow = minZ - worldPos.z;

        float maxDist = Mathf.Max(dxRight, dxLeft, dzAbove, dzBelow);

        if (maxDist == dzAbove) side = Side.Top;
        else if (maxDist == dzBelow) side = Side.Bottom;
        else if (maxDist == dxRight) side = Side.Right;
        else side = Side.Left;

        if (side == Side.Top || side == Side.Bottom)
            line = Mathf.RoundToInt((worldPos.x - minX) / spacingX);
        else
            line = Mathf.RoundToInt((maxZ - worldPos.z) / spacingY);

        int maxLine = (side == Side.Top || side == Side.Bottom) ? width : height;
        return line >= 0 && line < maxLine;
    }

    public List<GameObject> GetAllAliveBlocks()
    {
        var result = new List<GameObject>(aliveBlocks.Count);
        foreach (var b in aliveBlocks) result.Add(b.gameObject);
        return result;
    }

    public List<Block> GetAliveBlocksByColor(string hex)
    {
        if (!blocksByColor.TryGetValue(hex, out var set)) return new List<Block>();
        return new List<Block>(set);
    }

    public HashSet<string> GetOuterRingColors()
    {
        var colors = new HashSet<string>();
        for (int c = 0; c < width; c++)
        {
            int tr = topOuterRow[c];
            if (tr >= 0) colors.Add(grid[c, tr].colorHex);
            int br = bottomOuterRow[c];
            if (br >= 0) colors.Add(grid[c, br].colorHex);
        }
        for (int r = 0; r < height; r++)
        {
            int lc = leftOuterCol[r];
            if (lc >= 0) colors.Add(grid[lc, r].colorHex);
            int rc = rightOuterCol[r];
            if (rc >= 0) colors.Add(grid[rc, r].colorHex);
        }
        return colors;
    }
}