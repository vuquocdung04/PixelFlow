using UnityEngine;

public partial class Block : MonoBehaviour
{
    public string colorHex;
    public int gridCol;
    public int gridRow;
    public bool IsAlive { get; private set; } = true;

    public void SetupGrid(int col, int row, string hex)
    {
        gridCol = col;
        gridRow = row;
        colorHex = hex;
        IsAlive = true;
    }

    public void Break()
    {
        if (!IsAlive) return;
        IsAlive = false;
        BrickGrid.Instance.RemoveBlock(gridCol, gridRow);
        EventDispatcher.EventDispatcher.Instance.PostEvent(EventID.BLOCK_DESTROYED, this);
        gameObject.SetActive(false);
    }
}