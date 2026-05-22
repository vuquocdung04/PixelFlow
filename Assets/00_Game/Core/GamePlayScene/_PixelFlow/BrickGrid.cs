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
    }

    public void AddBlock(int col, int row, Block block)
    {
        if (col < 0 || col >= width || row < 0 || row >= height) return;
        grid[col, row] = block;
    }

    public void RemoveBlock(int col, int row)
    {
        if (col < 0 || col >= width || row < 0 || row >= height) return;
        grid[col, row] = null;
    }

    public void Clear()
    {
        if (grid == null) return;
        for (int c = 0; c < width; c++)
            for (int r = 0; r < height; r++)
                grid[c, r] = null;
    }

    public Block GetOuterBlock(Side side, int line)
    {
        switch (side)
        {
            case Side.Top:
                for (int r = 0; r < height; r++)
                    if (grid[line, r] != null && grid[line, r].IsAlive) return grid[line, r];
                break;
            case Side.Bottom:
                for (int r = height - 1; r >= 0; r--)
                    if (grid[line, r] != null && grid[line, r].IsAlive) return grid[line, r];
                break;
            case Side.Left:
                for (int c = 0; c < width; c++)
                    if (grid[c, line] != null && grid[c, line].IsAlive) return grid[c, line];
                break;
            case Side.Right:
                for (int c = width - 1; c >= 0; c--)
                    if (grid[c, line] != null && grid[c, line].IsAlive) return grid[c, line];
                break;
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

        if (maxDist == dzAbove)
            side = Side.Top;
        else if (maxDist == dzBelow)
            side = Side.Bottom;
        else if (maxDist == dxRight)
            side = Side.Right;
        else
            side = Side.Left;

        if (side == Side.Top || side == Side.Bottom)
            line = Mathf.RoundToInt((worldPos.x - minX) / spacingX);
        else
            line = Mathf.RoundToInt((maxZ - worldPos.z) / spacingY);

        int maxLine = (side == Side.Top || side == Side.Bottom) ? width : height;
        return line >= 0 && line < maxLine;
    }

    public void DestroyAllSameColor(string hex)
    {
        if (grid == null) return;
        for (int c = 0; c < width; c++)
            for (int r = 0; r < height; r++)
                if (grid[c, r] != null && grid[c, r].IsAlive && grid[c, r].colorHex == hex)
                    grid[c, r].Break();
    }

    public List<GameObject> GetAllAliveBlocks()
    {
        var result = new List<GameObject>();
        if (grid == null) return result;
        for (int c = 0; c < width; c++)
            for (int r = 0; r < height; r++)
                if (grid[c, r] != null && grid[c, r].IsAlive)
                    result.Add(grid[c, r].gameObject);
        return result;
    }

    public List<Block> GetAliveBlocksByColor(string hex)
    {
        var result = new List<Block>();
        if (grid == null) return result;
        for (int c = 0; c < width; c++)
            for (int r = 0; r < height; r++)
                if (grid[c, r] != null && grid[c, r].IsAlive && grid[c, r].colorHex == hex)
                    result.Add(grid[c, r]);
        return result;
    }
}