using UnityEngine;

public partial class Block : MonoBehaviour
{
    public string colorHex;
    public int gridCol;
    public int gridRow;
    public bool IsAlive { get; private set; } = true;
    public bool IsClaimed { get; private set; } = false;

    public void SetupGrid(int col, int row, string hex)
    {
        gridCol = col;
        gridRow = row;
        colorHex = hex;
        IsAlive = true;
        IsClaimed = false;
    }
    public void Claim() => IsClaimed = true;
    public void Break()
    {
        if (!IsAlive) return;
        IsAlive = false;
        IsClaimed = false;
        BrickGrid.Instance.RemoveBlock(gridCol, gridRow);
        EventDispatcher.EventDispatcher.Instance.PostEvent(EventID.BLOCK_DESTROYED, this);
        gameObject.SetActive(false);
    }
}