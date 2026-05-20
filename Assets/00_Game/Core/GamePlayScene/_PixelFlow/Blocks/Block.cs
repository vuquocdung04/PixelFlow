using DG.Tweening;
using EventDispatcher;
using UnityEngine;

public partial class Block : MonoBehaviour
{
    public string colorHex;
    public int gridCol;
    public int gridRow;
    public bool IsAlive { get; private set; } = true;
    public bool IsClaimed { get; private set; } = false;
    private Vector3 originalScale;
    public void SetupGrid(int col, int row, string hex)
    {
        gridCol = col;
        gridRow = row;
        colorHex = hex;
        IsAlive = true;
        IsClaimed = false;
        originalScale = transform.localScale;

    }
    public void Claim()
    {
        IsClaimed = true;
        this.PostEvent(EventID.BLOCK_DESTROYED, this);
    }
    public void Break()
    {
        if (!IsAlive) return;
        IsAlive = false;
        IsClaimed = false;
        BrickGrid.Instance.RemoveBlock(gridCol, gridRow);
        //EventDispatcher.EventDispatcher.Instance.PostEvent(EventID.BLOCK_DESTROYED, this);

        PlayBreakAnimation();
    }

    private void PlayBreakAnimation()
    {
        Sequence seq = DOTween.Sequence();

        seq.Append(transform.DOScale(originalScale * 1.3f, 0.1f).SetEase(Ease.OutQuad));
        seq.Append(transform.DOScale(Vector3.zero, 0.15f).SetEase(Ease.InBack));

        seq.OnComplete(() =>
        {
            Destroy(gameObject);
        });
    }
}